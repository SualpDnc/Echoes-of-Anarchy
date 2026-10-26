using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Rendering
{
    /// <summary>
    /// Cached materials created at runtime (characters, viewmodel, FX). Both shaders are guaranteed to be in
    /// builds because generated scene materials already use them.
    /// </summary>
    public static class RuntimeMaterials
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader lit, particle;

        public static Material Lit(Color color, float smoothness, float metallic = 0f, Color? emission = null)
        {
            string key = $"L{color}{smoothness}{metallic}{emission}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(lit) { name = "RT_Lit", hideFlags = HideFlags.DontSave, enableInstancing = true };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            return cache[key] = m;
        }

        /// <summary>Unlit FX material on the Echoes/Particle shader.</summary>
        public static Material Particle(string name, Texture tex, bool additive, float intensity, float softDistance = 0.3f)
        {
            string key = "P" + name;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            if (particle == null) particle = Shader.Find("Echoes/Particle");
            m = new Material(particle) { name = "RT_" + name, hideFlags = HideFlags.DontSave, renderQueue = 3000 };
            m.SetTexture("_MainTex", tex);
            m.SetFloat("_Intensity", intensity);
            m.SetFloat("_SoftDistance", softDistance);
            m.SetFloat("_SrcBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_Additive", additive ? 1f : 0f);
            return cache[key] = m;
        }
    }
}
