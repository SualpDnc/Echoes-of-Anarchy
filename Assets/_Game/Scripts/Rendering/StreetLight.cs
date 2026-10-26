using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Rendering
{
    /// <summary>
    /// A city-powered lamp that story beats can switch: at 19:00 the grid drops (lamps stutter and die in a wave),
    /// then some come back on emergency power, dimmer and failing. Works with or without a LightFlicker.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class StreetLight : MonoBehaviour
    {
        public static readonly List<StreetLight> All = new List<StreetLight>();

        public Renderer lens;
        public Color lensEmission = Color.white;

        Light lamp;
        LightFlicker flicker;
        AudioSource buzz; // the failing-lamp hum, if any
        float baseIntensity;
        MaterialPropertyBlock mpb;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public bool IsOn { get; private set; } = true;

        void Awake()
        {
            lamp = GetComponent<Light>();
            TryGetComponent(out flicker);
            TryGetComponent(out buzz);
            baseIntensity = flicker != null ? flicker.baseIntensity : lamp.intensity;
            mpb = new MaterialPropertyBlock();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void PowerCut(float delay) => StartCoroutine(Cut(delay));

        /// <summary>Back on at a fraction of full brightness, with a failing-lamp stutter.</summary>
        public void EmergencyPower(float delay, float level) => StartCoroutine(Restore(delay, level));

        IEnumerator Cut(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (flicker != null) flicker.enabled = false;
            // A few dying stutters, then out.
            int stutters = Random.Range(2, 5);
            for (int i = 0; i < stutters; i++)
            {
                Set(Random.Range(0f, 0.3f));
                yield return new WaitForSeconds(Random.Range(0.03f, 0.09f));
                Set(Random.Range(0.6f, 1f));
                yield return new WaitForSeconds(Random.Range(0.04f, 0.15f));
            }
            Set(0f);
            lamp.enabled = false;
            if (buzz != null) buzz.mute = true;
            IsOn = false;
        }

        IEnumerator Restore(float delay, float level)
        {
            yield return new WaitForSeconds(delay);
            lamp.enabled = true;
            IsOn = true;
            if (flicker == null)
            {
                flicker = gameObject.AddComponent<LightFlicker>();
                flicker.emissiveRenderer = lens;
                flicker.emissiveColor = lensEmission;
            }
            flicker.mode = LightFlicker.Mode.FailingLamp;
            flicker.baseIntensity = baseIntensity * level;
            flicker.emissiveColor = lensEmission * level;
            flicker.enabled = true;
            if (buzz != null) buzz.mute = false;
        }

        void Set(float level)
        {
            lamp.intensity = baseIntensity * level;
            if (lens == null) return;
            lens.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionId, lensEmission * level);
            lens.SetPropertyBlock(mpb);
        }
    }
}
