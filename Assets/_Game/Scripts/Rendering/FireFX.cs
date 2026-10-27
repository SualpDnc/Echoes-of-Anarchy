using UnityEngine;
using UnityEngine.Rendering;
using Echoes.Audio;

namespace Echoes.Rendering
{
    /// <summary>Builds particle-based fire, ember, smoke and steam effects entirely from code.</summary>
    public static class FireFX
    {
        [System.Serializable]
        public struct Materials
        {
            public Material flame, ember, smoke, steam;
        }

        /// <summary>A complete fire: flames, embers, rising smoke column and a flickering light. Scale ~1 = burning barrel.</summary>
        public static GameObject Fire(Materials m, Transform parent, Vector3 position, float scale, bool withLight = true, bool shadows = true)
        {
            var root = new GameObject("Fire");
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            Flames(m.flame, root.transform, scale);
            Embers(m.ember, root.transform, scale);
            Smoke(m.smoke, root.transform, scale, new Color(0.11f, 0.1f, 0.095f, 0.55f), 6f * scale);
            ProceduralAudioSource.Attach(root, SoundId.Fire, Mathf.Clamp01(0.35f + 0.25f * scale), 1.5f * scale, 14f + 10f * scale);

            if (withLight)
            {
                var lightGo = new GameObject("FireLight", typeof(Light));
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.localPosition = Vector3.up * (0.6f * scale);
                var l = lightGo.GetComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.47f, 0.15f);
                l.range = 9f + 5f * scale;
                l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
                l.shadowNearPlane = 0.3f;
                var f = lightGo.AddComponent<LightFlicker>();
                f.mode = LightFlicker.Mode.Fire;
                f.baseIntensity = 3.5f + 2.5f * scale;
                f.amount = 0.45f;
            }
            return root;
        }

        /// <summary>Manhole / grate steam — the city breathing.</summary>
        public static GameObject Steam(Materials m, Transform parent, Vector3 position)
        {
            var go = Smoke(m.steam, parent, 0.8f, new Color(0.55f, 0.56f, 0.6f, 0.22f), 3.5f);
            go.name = "Steam";
            go.transform.position = position;
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            var shape = ps.shape;
            shape.radius = 0.35f;
            ProceduralAudioSource.Attach(go, SoundId.Steam, 0.25f, 1f, 12f);
            return go;
        }

        static GameObject NewSystem(string name, Transform parent, Material mat, out ParticleSystem ps)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // emit upward
            ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = 0f;
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            return go;
        }

        static void Flames(Material mat, Transform parent, float scale)
        {
            NewSystem("Flames", parent, mat, out var ps);
            var main = ps.main;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * scale, 1.3f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f * scale, 0.9f * scale);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            main.maxParticles = 120;
            main.startColor = new Color(1f, 0.85f, 0.6f);

            var em = ps.emission;
            em.rateOverTime = 38f * Mathf.Max(0.5f, scale);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f * scale;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.12f), 0.4f), new GradientColorKey(new Color(0.7f, 0.15f, 0.02f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.25f)));

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 1.4f;
            noise.scrollSpeed = 1.2f;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            r.maxParticleSize = 2f;
            ps.Play();
        }

        static void Embers(Material mat, Transform parent, float scale)
        {
            NewSystem("Embers", parent, mat, out var ps);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = new Color(1f, 0.55f, 0.15f);
            main.gravityModifier = -0.05f;
            main.maxParticles = 80;
            var em = ps.emission;
            em.rateOverTime = 9f * scale;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f * scale;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1.4f;
            noise.frequency = 0.8f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.7f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
        }

        static GameObject Smoke(Material mat, Transform parent, float scale, Color tint, float height)
        {
            var go = NewSystem("Smoke", parent, mat, out var ps);
            go.transform.localPosition = Vector3.up * 0.5f * scale;
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(height / 1.5f, height / 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f * scale, 1.3f * scale);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = tint;
            main.maxParticles = 90;
            var em = ps.emission;
            em.rateOverTime = 7f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f * scale;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 4f)));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            // Drift with the wind.
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.2f;

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sortingFudge = 10f; // draw smoke behind flames
            r.maxParticleSize = 4f;
            ps.Play();
            return go;
        }
    }
}
