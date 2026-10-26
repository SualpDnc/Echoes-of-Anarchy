using System.Collections.Generic;
using UnityEngine;

namespace Echoes.AI
{
    /// <summary>
    /// How lit a point is by the world's lights, accounting for range falloff, spot cones and occlusion of the
    /// strongest contributors. Drives stealth: standing under a sodium lamp is very different from a dark doorway.
    /// </summary>
    public static class LightExposure
    {
        static readonly List<Light> lights = new List<Light>();
        static int cachedFrame = -1;
        static Vector3 cachedPos;
        static float cachedValue;

        public static void Collect(Transform root)
        {
            lights.Clear();
            lights.AddRange(root.GetComponentsInChildren<Light>(true));
            var moon = RenderSettings.sun;
            if (moon != null && !lights.Contains(moon)) lights.Add(moon);
        }

        /// <summary>Raw summed illuminance-ish value. ~0.2 moonlight, ~3 near a fire, ~6+ under a street lamp.</summary>
        public static float Sample(Vector3 p)
        {
            if (Time.frameCount == cachedFrame && (p - cachedPos).sqrMagnitude < 0.01f) return cachedValue;
            float sum = 0f;
            int occlusionChecks = 0;
            foreach (var l in lights)
            {
                if (l == null || !l.isActiveAndEnabled || l.intensity <= 0f) continue;
                if (l.type == LightType.Directional) { sum += l.intensity * 0.6f; continue; }
                Vector3 d = p - l.transform.position;
                float dist = d.magnitude;
                if (dist > l.range || dist < 0.001f) continue;
                float att = 1f - dist / l.range;
                att *= att;
                if (l.type == LightType.Spot)
                {
                    float cos = Vector3.Dot(l.transform.forward, d / dist);
                    float outer = Mathf.Cos(l.spotAngle * 0.5f * Mathf.Deg2Rad), inner = Mathf.Cos(l.innerSpotAngle * 0.5f * Mathf.Deg2Rad);
                    att *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(outer, inner, cos));
                }
                float c = l.intensity * att;
                if (c > 0.4f && occlusionChecks < 6)
                {
                    occlusionChecks++;
                    if (Physics.Linecast(p, l.transform.position - d / dist * 0.3f, ~0, QueryTriggerInteraction.Ignore)) c *= 0.12f;
                }
                sum += c;
            }
            cachedFrame = Time.frameCount;
            cachedPos = p;
            return cachedValue = sum;
        }

        /// <summary>Maps exposure to 0..1 visibility.</summary>
        public static float Visibility(Vector3 p) => 1f - Mathf.Exp(-Sample(p) * 0.35f);
    }
}
