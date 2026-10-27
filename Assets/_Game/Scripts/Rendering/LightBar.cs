using UnityEngine;

namespace Echoes.Rendering
{
    /// <summary>
    /// Emergency light bar: two groups of lenses and lights that flash in turn, each with a double strobe
    /// (on, off, on), like an ambulance's warning lights. Lenses glow through their emission; lights cast no shadows.
    /// </summary>
    public class LightBar : MonoBehaviour
    {
        public Renderer[] lensesA, lensesB;
        public Color emissionA = new Color(1f, 0.06f, 0.04f) * 14f;
        public Color emissionB = new Color(1f, 0.06f, 0.04f) * 14f;
        public Light[] lightsA, lightsB;
        public float lightIntensity = 6f;
        [Tooltip("Seconds for one full A-B cycle.")]
        public float period = 0.55f;
        public bool on;

        MaterialPropertyBlock mpb;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        float offset;

        void Awake()
        {
            mpb = new MaterialPropertyBlock();
            offset = Random.value * period;
            Apply(0f, 0f);
        }

        public void SetOn(bool value)
        {
            on = value;
            if (!on) Apply(0f, 0f);
        }

        void Update()
        {
            if (!on) return;
            float ph = Mathf.Repeat((Time.time + offset) / period, 1f);
            Apply(Strobe(ph), Strobe(Mathf.Repeat(ph + 0.5f, 1f)));
        }

        /// <summary>Double flash in the first half of the cycle.</summary>
        static float Strobe(float ph)
        {
            if (ph < 0.12f) return 1f;
            if (ph < 0.2f) return 0f;
            if (ph < 0.32f) return 1f;
            return 0f;
        }

        void Apply(float a, float b)
        {
            Set(lensesA, emissionA * a);
            Set(lensesB, emissionB * b);
            Set(lightsA, a);
            Set(lightsB, b);
        }

        void Set(Renderer[] lenses, Color c)
        {
            if (lenses == null) return;
            foreach (var r in lenses)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionId, c);
                r.SetPropertyBlock(mpb);
            }
        }

        void Set(Light[] lights, float level)
        {
            if (lights == null) return;
            foreach (var l in lights)
            {
                if (l == null) continue;
                l.enabled = level > 0.01f;
                l.intensity = lightIntensity * level;
            }
        }
    }
}
