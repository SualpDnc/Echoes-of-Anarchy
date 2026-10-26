using UnityEngine;

namespace Echoes.Rendering
{
    /// <summary>Procedural light behaviour for fires, failing sodium lamps and steady sources.</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        public enum Mode { Fire, FailingLamp, Neon, Blink }

        public Mode mode = Mode.Fire;
        public float baseIntensity = 3f;
        public float amount = 0.35f;
        public float speed = 1f;
        [Tooltip("Optional emissive renderer that should follow the light.")]
        public Renderer emissiveRenderer;
        public Color emissiveColor = Color.white;

        Light lamp;
        float seed;
        float dropTimer;
        float dropLevel = 1f;
        Vector3 basePos;
        MaterialPropertyBlock mpb;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            lamp = GetComponent<Light>();
            seed = Random.value * 1000f;
            basePos = transform.localPosition;
            mpb = new MaterialPropertyBlock();
        }

        void Update()
        {
            float t = Time.time * speed;
            float level = 1f;
            switch (mode)
            {
                case Mode.Fire:
                    // Layered noise: slow breathing + fast licks; the light also wanders like a flame centre.
                    float slow = Mathf.PerlinNoise(seed, t * 1.3f);
                    float fast = Mathf.PerlinNoise(seed + 7.3f, t * 9f);
                    level = 1f + ((slow - 0.5f) * 0.9f + (fast - 0.5f) * 0.6f) * amount * 2f;
                    transform.localPosition = basePos + new Vector3(
                        (Mathf.PerlinNoise(seed + 2f, t * 2.1f) - 0.5f) * 0.12f,
                        (Mathf.PerlinNoise(seed + 3f, t * 2.7f) - 0.5f) * 0.1f,
                        (Mathf.PerlinNoise(seed + 4f, t * 2.4f) - 0.5f) * 0.12f);
                    break;

                case Mode.FailingLamp:
                    dropTimer -= Time.deltaTime;
                    if (dropTimer <= 0f)
                    {
                        // Mostly on; occasionally stutters out.
                        bool stutter = Random.value < 0.18f;
                        dropLevel = stutter ? Random.Range(0f, 0.25f) : 1f;
                        dropTimer = stutter ? Random.Range(0.03f, 0.12f) : Random.Range(0.2f, 3.5f);
                    }
                    level = dropLevel * (1f + (Mathf.PerlinNoise(seed, t * 30f) - 0.5f) * amount * 0.3f);
                    break;

                case Mode.Blink:
                    // Traffic signal on flash mode: 1 Hz with an incandescent-style ramp.
                    float ph = Mathf.Repeat(t, 1f);
                    level = Mathf.Clamp01(ph < 0.5f ? ph * 12f : (1f - ph) * 12f - 5f);
                    break;

                case Mode.Neon:
                    level = 1f + Mathf.Sin(t * 377f) * 0.02f + (Random.value < 0.004f ? -0.8f : 0f);
                    break;
            }

            lamp.intensity = baseIntensity * Mathf.Max(0f, level);
            if (emissiveRenderer != null)
            {
                emissiveRenderer.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionId, emissiveColor * Mathf.Max(0f, level));
                emissiveRenderer.SetPropertyBlock(mpb);
            }
        }
    }
}
