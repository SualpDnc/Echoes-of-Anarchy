using System.Collections;
using UnityEngine;

namespace Echoes.Audio
{
    /// <summary>
    /// The sound of the Purge happening elsewhere: gunfire, screams, alarms, sirens, explosions, fireworks,
    /// scattered around the listener at 50–250 m. Intensity scales how busy the night is.
    /// </summary>
    public class DistantEvents : MonoBehaviour
    {
        [Range(0f, 1f)] public float intensity = 0.6f;
        public bool purgeActive = true;
        public float minRadius = 50f, maxRadius = 240f;

        float nextEvent;

        void Start() => nextEvent = Time.time + Random.Range(2f, 5f);

        void Update()
        {
            if (!purgeActive || AudioManager.Instance == null || Time.time < nextEvent) return;
            float gap = Mathf.Lerp(16f, 3f, intensity);
            nextEvent = Time.time + Random.Range(gap * 0.4f, gap * 1.4f);
            TriggerRandom();
        }

        public void TriggerRandom()
        {
            float r = Random.value;
            if (r < 0.38f) StartCoroutine(Gunfire());
            else if (r < 0.52f) Play(SoundId.Scream, 0.8f, Random.Range(0.92f, 1.1f));
            else if (r < 0.6f) Play(SoundId.DogBark, 0.6f, Random.Range(0.9f, 1.1f));
            else if (r < 0.68f) Play(SoundId.CarAlarm, 0.45f, 1f);
            else if (r < 0.76f) StartCoroutine(PoliceDriveBy());
            else if (r < 0.82f) Play(SoundId.Explosion, 1f, Random.Range(0.85f, 1.05f));
            else if (r < 0.92f) StartCoroutine(Fireworks());
            else Play(SoundId.GlassBreak, 0.6f, Random.Range(0.9f, 1.1f), 0.5f);
        }

        Vector3 RandomPoint(float radiusScale = 1f)
        {
            Vector3 c = Camera.main != null ? Camera.main.transform.position : transform.position;
            Vector2 dir = Random.insideUnitCircle.normalized;
            float d = Random.Range(minRadius, maxRadius) * radiusScale;
            return c + new Vector3(dir.x * d, Random.Range(2f, 12f), dir.y * d);
        }

        void Play(SoundId id, float vol, float pitch, float radiusScale = 1f)
        {
            // Large minDistance keeps far events audible; air absorption is handled by the manager.
            AudioManager.Instance.PlayAt(id, RandomPoint(radiusScale), vol, pitch, 40f, 600f);
        }

        IEnumerator Gunfire()
        {
            Vector3 p = RandomPoint();
            bool rifle = Random.value < 0.35f;
            float pattern = Random.value;
            int shots = pattern < 0.3f ? 1 : pattern < 0.6f ? Random.Range(2, 4) : pattern < 0.85f ? Random.Range(4, 8) : Random.Range(8, 16);
            float spacing = rifle && shots > 6 ? Random.Range(0.08f, 0.12f) : Random.Range(0.15f, 0.5f);
            var id = rifle ? SoundId.GunRifleFar : SoundId.GunPistolFar;
            for (int i = 0; i < shots; i++)
            {
                AudioManager.Instance.PlayAt(id, p + Random.insideUnitSphere * 2f, Random.Range(0.75f, 1f), Random.Range(0.95f, 1.05f), 45f, 700f);
                yield return new WaitForSeconds(spacing * Random.Range(0.8f, 1.3f));
            }
            // Sometimes someone answers back from another direction.
            if (Random.value < 0.35f)
            {
                yield return new WaitForSeconds(Random.Range(0.5f, 2f));
                p = RandomPoint();
                for (int i = 0; i < Random.Range(1, 5); i++)
                {
                    AudioManager.Instance.PlayAt(SoundId.GunPistolFar, p, Random.Range(0.6f, 0.9f), 1f, 45f, 700f);
                    yield return new WaitForSeconds(Random.Range(0.2f, 0.6f));
                }
            }
        }

        IEnumerator Fireworks()
        {
            int n = Random.Range(1, 5);
            Vector3 p = RandomPoint();
            for (int i = 0; i < n; i++)
            {
                AudioManager.Instance.PlayAt(SoundId.Firework, p + Random.insideUnitSphere * 15f, 0.7f, Random.Range(0.9f, 1.15f), 40f, 600f);
                yield return new WaitForSeconds(Random.Range(0.3f, 1.4f));
            }
        }

        /// <summary>A cruiser screaming past a few blocks over — a moving source so Doppler and panning are real.</summary>
        IEnumerator PoliceDriveBy()
        {
            var clip = AudioManager.Instance.Clip(SoundId.PoliceSiren);
            if (clip == null) yield break;
            Vector3 c = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            Vector2 dir = Random.insideUnitCircle.normalized;
            Vector3 perp = new Vector3(-dir.y, 0f, dir.x);
            float offset = Random.Range(60f, 140f);
            Vector3 mid = c + new Vector3(dir.x, 0f, dir.y) * offset + Vector3.up * 1.5f;
            const float reach = 340f; // silent from here on
            Vector3 a = mid - perp * 300f, b = mid + perp * 300f;

            var go = new GameObject("PoliceDriveBy");
            var src = go.AddComponent<AudioSource>();
            var lp = go.AddComponent<AudioLowPassFilter>();
            lp.cutoffFrequency = 3500f;
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 1f;
            // Owner: the siren must fade as it drives away. A custom curve falls all the way to silence at `reach`
            // (the default log rolloff never reaches zero and kept it loud to the end).
            src.rolloffMode = AudioRolloffMode.Custom;
            src.maxDistance = reach;
            src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.04f, 0.9f), new Keyframe(0.15f, 0.42f), new Keyframe(0.35f, 0.16f),
                new Keyframe(0.65f, 0.04f), new Keyframe(1f, 0f)));
            src.dopplerLevel = 1f;
            src.volume = 0f;
            src.time = Random.Range(0f, clip.length);
            go.transform.position = a;
            src.Play();
            float speed = Random.Range(18f, 28f), len = Vector3.Distance(a, b), t = 0f;
            float nextOcclusion = 0f, occlusion = 1f;
            while (t < len)
            {
                t += speed * Time.deltaTime;
                float u = t / len;
                go.transform.position = Vector3.Lerp(a, b, u);
                // Fade in and out at the ends of the run, so it never starts or stops abruptly.
                src.volume = 0.6f * Mathf.Clamp01(u / 0.1f) * Mathf.Clamp01((1f - u) / 0.1f);
                var cam = Camera.main;
                if (cam != null)
                {
                    float d = Vector3.Distance(cam.transform.position, go.transform.position);
                    if (Time.time > nextOcclusion)
                    {
                        nextOcclusion = Time.time + 0.25f;
                        Vector3 to = go.transform.position - cam.transform.position;
                        occlusion = Physics.Raycast(cam.transform.position, to.normalized, d - 2f, ~0, QueryTriggerInteraction.Ignore) ? 0.4f : 1f;
                    }
                    // Air takes the highs first; buildings in between take more.
                    float target = Mathf.Lerp(6000f, 700f, Mathf.Clamp01(d / reach)) * occlusion;
                    lp.cutoffFrequency = Mathf.Lerp(lp.cutoffFrequency, Mathf.Max(400f, target), 1f - Mathf.Exp(-4f * Time.deltaTime));
                }
                yield return null;
            }
            Destroy(go);
        }
    }
}
