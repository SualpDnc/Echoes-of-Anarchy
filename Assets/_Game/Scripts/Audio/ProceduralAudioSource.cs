using UnityEngine;

namespace Echoes.Audio
{
    /// <summary>
    /// Looping 3D emitter for a synthesized clip (fires, steam, lamp buzz...). Clips only exist at runtime,
    /// so the emitter stores the SoundId and fetches the clip from the AudioManager on start.
    /// Muffles itself when a wall stands between it and the listener.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralAudioSource : MonoBehaviour
    {
        public SoundId sound;
        [Range(0f, 1f)] public float volume = 0.6f;
        public float minDistance = 1.5f;
        public float maxDistance = 30f;
        public float pitchJitter = 0.06f;
        [Tooltip("Lock loop phase to the clock (e.g. relay clicks in sync with a blinking light).")]
        public bool syncToTime;

        AudioSource src;
        AudioLowPassFilter lp;
        float nextOcclusionCheck;
        float targetCutoff = 22000f;

        void Awake()
        {
            src = GetComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.dopplerLevel = 0f;
            if (!TryGetComponent(out lp)) lp = gameObject.AddComponent<AudioLowPassFilter>();
        }

        void Start()
        {
            var am = AudioManager.Instance;
            if (am == null) { enabled = false; return; }
            src.clip = am.Clip(sound);
            src.volume = volume;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            if (src.clip == null) return;
            src.time = syncToTime ? Mathf.Repeat(Time.time, src.clip.length) : Random.Range(0f, src.clip.length);
            src.Play();
            nextOcclusionCheck = Time.time + Random.value * 0.3f;
        }

        void Update()
        {
            if (Time.time >= nextOcclusionCheck)
            {
                nextOcclusionCheck = Time.time + 0.25f;
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 a = cam.transform.position, b = transform.position;
                    float d = Vector3.Distance(a, b);
                    if (d > maxDistance * 1.2f) { targetCutoff = 22000f; }
                    else
                    {
                        // Ignore the last 2 m: emitters often sit inside their own prop's collider.
                        bool blocked = d > 3f && Physics.Raycast(a, (b - a) / d, d - 2f, ~0, QueryTriggerInteraction.Ignore);
                        targetCutoff = blocked ? 900f : Mathf.Lerp(22000f, 6000f, d / maxDistance);
                    }
                }
            }
            lp.cutoffFrequency = Mathf.Lerp(lp.cutoffFrequency, targetCutoff, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }

        public static ProceduralAudioSource Attach(GameObject go, SoundId id, float volume, float minDist, float maxDist, bool sync = false)
        {
            go.AddComponent<AudioSource>();
            var p = go.AddComponent<ProceduralAudioSource>();
            p.sound = id;
            p.volume = volume;
            p.minDistance = minDist;
            p.maxDistance = maxDist;
            p.syncToTime = sync;
            if (sync) p.pitchJitter = 0f;
            return p;
        }
    }
}
