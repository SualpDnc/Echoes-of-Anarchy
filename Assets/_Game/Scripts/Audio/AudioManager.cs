using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Echoes.Audio
{
    /// <summary>
    /// Renders every SynthLibrary recipe at load (in parallel), owns the clip bank, the stereo ambience beds,
    /// a pool of 3D one-shot voices with speed-of-sound delay and air absorption, and the bodycam mic colouring.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float ambienceVolume = 0.55f;
        [Range(0f, 1f)] public float windVolume = 0.35f;
        public int voiceCount = 24;

        const float SpeedOfSound = 343f;

        readonly Dictionary<SoundId, AudioClip[]> bank = new Dictionary<SoundId, AudioClip[]>();
        readonly List<Voice> voices = new List<Voice>();
        AudioSource ambienceSrc, windSrc;
        AudioListener listener;

        class Voice
        {
            public AudioSource src;
            public AudioLowPassFilter lp;
            public float busyUntil;
        }

        public bool Ready { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            float t0 = Time.realtimeSinceStartup;
            RenderBank();
            Debug.Log($"[Echoes] Synthesized {bank.Count} sound types in {(Time.realtimeSinceStartup - t0) * 1000f:0} ms");

            for (int i = 0; i < voiceCount; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(transform, false);
                var v = new Voice { src = go.AddComponent<AudioSource>(), lp = go.AddComponent<AudioLowPassFilter>() };
                v.src.playOnAwake = false;
                v.src.spatialBlend = 1f;
                v.src.rolloffMode = AudioRolloffMode.Logarithmic;
                v.src.dopplerLevel = 0f;
                voices.Add(v);
            }
            ambienceSrc = Bed(SoundId.AmbienceCity, ambienceVolume);
            windSrc = Bed(SoundId.Wind, windVolume);
            Ready = true;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            AttachMicColouring();
        }

        void Update()
        {
            AudioListener.volume = masterVolume;
            ambienceSrc.volume = ambienceVolume;
            windSrc.volume = windVolume;
        }

        void RenderBank()
        {
            var recipes = SynthLibrary.All();
            var jobs = new List<(SynthLibrary.Recipe recipe, int variation)>();
            foreach (var rc in recipes)
                for (int v = 0; v < rc.variations; v++) jobs.Add((rc, v));

            var data = new float[jobs.Count][];
            Parallel.For(0, jobs.Count, i =>
            {
                var (rc, v) = jobs[i];
                var rng = new System.Random(((int)rc.id + 1) * 7919 + v * 104729);
                data[i] = rc.render(rng);
            });

            for (int i = 0; i < jobs.Count; i++)
            {
                var (rc, v) = jobs[i];
                int ch = rc.stereo ? 2 : 1;
                var clip = AudioClip.Create($"{rc.id}_{v}", data[i].Length / ch, ch, DSP.Rate, false);
                clip.SetData(data[i], 0);
                if (!bank.TryGetValue(rc.id, out var arr)) bank[rc.id] = arr = new AudioClip[rc.variations];
                arr[v] = clip;
            }
        }

        AudioSource Bed(SoundId id, float vol)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = Clip(id);
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = vol;
            src.priority = 0;
            src.Play();
            return src;
        }

        /// <summary>
        /// A body-worn camera mic: rolls off sub-bass handling noise and the top octave.
        /// Filters on the listener object process the whole mix.
        /// </summary>
        void AttachMicColouring()
        {
            listener = Camera.main != null ? Camera.main.GetComponent<AudioListener>() : null;
            if (listener == null) return;
            var go = listener.gameObject;
            if (!go.TryGetComponent(out AudioHighPassFilter hp)) hp = go.AddComponent<AudioHighPassFilter>();
            hp.cutoffFrequency = 70f;
            if (!go.TryGetComponent(out AudioLowPassFilter lp)) lp = go.AddComponent<AudioLowPassFilter>();
            lp.cutoffFrequency = 12500f;
        }

        // ------------------------------------------------------------------ API

        public AudioClip Clip(SoundId id, int variation = -1)
        {
            if (!bank.TryGetValue(id, out var arr) || arr.Length == 0) return null;
            return variation < 0 ? arr[Random.Range(0, arr.Length)] : arr[variation % arr.Length];
        }

        public int Variations(SoundId id) => bank.TryGetValue(id, out var arr) ? arr.Length : 0;

        /// <summary>
        /// Plays a positional one-shot. Far sounds arrive late (speed of sound) and lose highs (air absorption);
        /// sounds behind buildings are muffled further.
        /// </summary>
        public AudioSource PlayAt(SoundId id, Vector3 pos, float volume = 1f, float pitch = 1f,
                                  float minDistance = 2f, float maxDistance = 120f, bool physicalDelay = true, int variation = -1)
        {
            var clip = Clip(id, variation);
            if (clip == null) return null;
            var v = GetVoice();
            v.src.transform.position = pos;
            v.src.clip = clip;
            v.src.volume = volume;
            v.src.pitch = pitch;
            v.src.minDistance = minDistance;
            v.src.maxDistance = maxDistance;
            v.src.loop = false;
            v.src.ignoreListenerPause = false;

            float dist = listener != null ? Vector3.Distance(listener.transform.position, pos) : 0f;
            float cutoff = Mathf.Lerp(22000f, 1800f, Mathf.Clamp01(dist / 250f));
            if (listener != null && dist > 3f && Occluded(listener.transform.position, pos)) cutoff *= 0.35f;
            v.lp.cutoffFrequency = Mathf.Max(400f, cutoff);

            float delay = physicalDelay ? dist / SpeedOfSound : 0f;
            v.src.PlayDelayed(delay);
            v.busyUntil = Time.time + delay + clip.length / Mathf.Max(0.1f, pitch) + 0.1f;
            return v.src;
        }

        /// <summary>Non-positional one-shot (player foley, UI).</summary>
        public void Play2D(SoundId id, float volume = 1f, float pitch = 1f, int variation = -1)
        {
            var clip = Clip(id, variation);
            if (clip == null) return;
            var v = GetVoice();
            v.src.transform.localPosition = Vector3.zero;
            v.src.ignoreListenerPause = AudioListener.pause; // a 2D sound started while the world is paused (end cards) still plays
            v.src.spatialBlend = 0f;
            v.src.clip = clip;
            v.src.volume = volume;
            v.src.pitch = pitch;
            v.lp.cutoffFrequency = 22000f;
            v.src.Play();
            v.busyUntil = Time.time + clip.length / Mathf.Max(0.1f, pitch) + 0.05f;
            StartCoroutine(Restore3D(v, v.busyUntil));
        }

        System.Collections.IEnumerator Restore3D(Voice v, float until)
        {
            while (Time.time < until) yield return null;
            v.src.spatialBlend = 1f;
        }

        Voice GetVoice()
        {
            Voice oldest = voices[0];
            foreach (var v in voices)
            {
                if (Time.time >= v.busyUntil) { v.src.spatialBlend = 1f; return v; }
                if (v.busyUntil < oldest.busyUntil) oldest = v;
            }
            oldest.src.Stop();
            oldest.src.spatialBlend = 1f;
            return oldest;
        }

        static bool Occluded(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            return Physics.Raycast(from, d.normalized, d.magnitude - 1f, ~0, QueryTriggerInteraction.Ignore);
        }
    }
}
