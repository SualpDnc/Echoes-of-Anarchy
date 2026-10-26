using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Echoes.Rendering
{
    /// <summary>
    /// Builds the global "body-worn camera" look in code: wide barrel lens, cheap-sensor grain and fringing,
    /// crushed blacks with lifted shadows, cool white balance, sodium-light bloom, camera motion blur.
    /// The profile is rebuilt on enable, so nothing needs to be saved as an asset.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Volume))]
    public class BodycamVolume : MonoBehaviour
    {
        [Header("Lens")]
        [Range(-1f, 1f)] public float lensDistortion = 0.32f;
        [Range(0.5f, 1.5f)] public float lensScale = 0.93f;
        [Range(0f, 1f)] public float chromaticAberration = 0.07f;
        [Range(0f, 1f)] public float vignette = 0.3f;

        [Header("Sensor")]
        [Range(0f, 1f)] public float grain = 0.08f;
        [Range(0f, 1f)] public float motionBlur = 0.18f;
        public float postExposure = 0.9f;
        [Range(-100f, 100f)] public float contrast = 14f;
        [Range(-100f, 100f)] public float saturation = -18f;
        [Range(-100f, 100f)] public float temperature = -8f;

        [Header("Bloom")]
        public float bloomIntensity = 1.1f;
        public float bloomThreshold = 0.85f;

        Volume volume;
        VolumeProfile profile;
        Vignette vig;
        ChromaticAberration ca;
        ColorAdjustments color;
        LensDistortion lens;

        void OnEnable()
        {
            volume = GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            Build();
        }

        void OnDisable()
        {
            if (volume != null && volume.sharedProfile == profile) volume.sharedProfile = null;
            if (profile != null) DestroyImmediate(profile);
            profile = null;
        }

        void OnValidate()
        {
            if (isActiveAndEnabled && profile != null) Apply();
        }

        void Build()
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Bodycam (runtime)";
            profile.hideFlags = HideFlags.DontSave;

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            profile.Add<Bloom>(true);
            profile.Add<FilmGrain>(true);
            ca = profile.Add<ChromaticAberration>(true);
            vig = profile.Add<Vignette>(true);
            lens = profile.Add<LensDistortion>(true);
            profile.Add<MotionBlur>(true);
            color = profile.Add<ColorAdjustments>(true);
            profile.Add<WhiteBalance>(true);
            profile.Add<ShadowsMidtonesHighlights>(true);
            profile.Add<SplitToning>(true);

            volume.sharedProfile = profile;
            Apply();
        }

        void Apply()
        {
            profile.TryGet(out Bloom bloom);
            bloom.intensity.Override(bloomIntensity);
            bloom.threshold.Override(bloomThreshold);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.93f, 0.85f));
            bloom.highQualityFiltering.Override(true);

            profile.TryGet(out FilmGrain fg);
            fg.type.Override(FilmGrainLookup.Thin1);
            fg.intensity.Override(grain);
            fg.response.Override(0.8f); // grain mostly confined to deep shadows, like a real small sensor at high ISO

            ca.intensity.Override(chromaticAberration);

            vig.intensity.Override(vignette);
            vig.smoothness.Override(0.45f);
            vig.color.Override(Color.black);

            lens.intensity.Override(lensDistortion);
            lens.scale.Override(lensScale);
            lens.xMultiplier.Override(1f);
            lens.yMultiplier.Override(0.85f);

            profile.TryGet(out MotionBlur mb);
            mb.mode.Override(MotionBlurMode.CameraOnly);
            mb.quality.Override(MotionBlurQuality.Medium);
            mb.intensity.Override(motionBlur);
            mb.clamp.Override(0.06f);

            color.postExposure.Override(postExposure);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);
            color.colorFilter.Override(new Color(0.97f, 0.99f, 1f));

            profile.TryGet(out WhiteBalance wb);
            wb.temperature.Override(temperature);
            wb.tint.Override(3f);

            // Lift the blacks slightly toward blue-green, the way compressed bodycam footage looks.
            profile.TryGet(out ShadowsMidtonesHighlights smh);
            smh.shadows.Override(new Vector4(0.95f, 1.0f, 1.08f, 0.02f));
            smh.midtones.Override(new Vector4(1f, 1f, 1f, 0f));
            smh.highlights.Override(new Vector4(1.04f, 1.0f, 0.95f, 0f));

            profile.TryGet(out SplitToning st);
            st.shadows.Override(new Color(0.42f, 0.5f, 0.55f));
            st.highlights.Override(new Color(0.6f, 0.55f, 0.48f));
            st.balance.Override(-15f);
        }

        // --- Runtime hooks for gameplay (damage, adrenaline, signal loss) ---

        /// <summary>Pulse vignette/aberration, e.g. when hurt. 0..1.</summary>
        public void SetStress(float stress01)
        {
            if (profile == null) return;
            vig.intensity.Override(Mathf.Lerp(vignette, 0.6f, stress01));
            vig.color.Override(Color.Lerp(Color.black, new Color(0.25f, 0f, 0f), stress01));
            ca.intensity.Override(Mathf.Lerp(chromaticAberration, 1f, stress01));
            color.saturation.Override(Mathf.Lerp(saturation, -60f, stress01));
        }

        /// <summary>Exposure shift in EV, e.g. auto-exposure adapting when the flashlight is off.</summary>
        public void SetExposureOffset(float ev)
        {
            if (profile == null) return;
            color.postExposure.Override(postExposure + ev);
        }
    }
}
