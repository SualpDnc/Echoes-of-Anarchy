using UnityEngine;
using UnityEngine.Rendering;
using Echoes.Audio;
using Echoes.Rendering;

namespace Echoes.Combat
{
    /// <summary>Bullet impacts: pooled decals, one-shot particle bursts and surface-matched sounds.</summary>
    public class ImpactFX : MonoBehaviour
    {
        static ImpactFX instance;
        const int DecalCount = 80;

        Transform[] decals;
        int nextDecal;
        ParticleSystem dust, sparks, blood, glass;

        public static ImpactFX Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("ImpactFX").AddComponent<ImpactFX>();
                return instance;
            }
        }

        void Awake()
        {
            instance = this;
            var holeMat = RuntimeMaterials.Particle("BulletHole", ProcTextures.BulletHole(), false, 1f, 0.001f);
            var quad = QuadMesh();
            decals = new Transform[DecalCount];
            for (int i = 0; i < DecalCount; i++)
            {
                var go = new GameObject("Decal", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = quad;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = holeMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                go.SetActive(false);
                decals[i] = go.transform;
            }
            var puffMat = RuntimeMaterials.Particle("ImpactDust", ProcTextures.Puff(), false, 1f, 0.2f);
            var dotMat = RuntimeMaterials.Particle("ImpactSpark", ProcTextures.Dot(), true, 6f, 0.05f);
            dust = Burst("Dust", puffMat, new Color(0.35f, 0.33f, 0.3f, 0.6f), 0.15f, 0.5f, 0.6f, 2.2f, 0f, 10);
            sparks = Burst("Sparks", dotMat, new Color(1f, 0.7f, 0.35f), 0.02f, 0.04f, 4f, 0.35f, 1f, 14);
            blood = Burst("Blood", puffMat, new Color(0.18f, 0.01f, 0.01f, 0.85f), 0.12f, 0.35f, 1.4f, 0.5f, 0.4f, 12);
            glass = Burst("Glass", dotMat, new Color(0.7f, 0.8f, 0.8f, 0.8f), 0.015f, 0.03f, 3f, 0.6f, 1f, 18);
        }

        public static void Spawn(RaycastHit hit, Vector3 shotDir)
        {
            var fx = Instance;
            var kind = SurfaceUtil.Classify(hit);
            Vector3 n = hit.normal;
            Vector3 reflect = Vector3.Reflect(shotDir, n);

            switch (kind)
            {
                case SurfaceKind.Flesh:
                    fx.Emit(fx.blood, hit.point, -shotDir * 0.3f + shotDir, 10);
                    Sound(SoundId.ImpactFlesh, hit.point, 0.9f);
                    return; // no decal on characters
                case SurfaceKind.Metal:
                    fx.Emit(fx.sparks, hit.point, Vector3.Lerp(n, reflect, 0.6f), 12);
                    Sound(SoundId.ImpactMetal, hit.point, 0.8f);
                    break;
                case SurfaceKind.Glass:
                    fx.Emit(fx.glass, hit.point, n, 16);
                    Sound(SoundId.GlassBreak, hit.point, 0.5f);
                    break;
                default:
                    fx.Emit(fx.dust, hit.point, n, 6);
                    if (Random.value < 0.4f) fx.Emit(fx.sparks, hit.point, Vector3.Lerp(n, reflect, 0.5f), 3);
                    Sound(SoundId.ImpactHard, hit.point, 0.8f);
                    break;
            }
            fx.PlaceDecal(hit);
        }

        /// <summary>Glass shard burst (thrown bottles, windows).</summary>
        public static void Glass(Vector3 point, Vector3 normal)
        {
            var fx = Instance;
            fx.Emit(fx.glass, point, normal, 30);
        }

        static void Sound(SoundId id, Vector3 p, float vol)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(id, p, vol, Random.Range(0.9f, 1.1f), 1.5f, 50f);
        }

        void PlaceDecal(RaycastHit hit)
        {
            // Don't stick decals onto moving rigidbodies (they would float in the air).
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic) return;
            var d = decals[nextDecal];
            nextDecal = (nextDecal + 1) % DecalCount;
            // World geometry is static, so decals can live in world space under this pool.
            d.gameObject.SetActive(true);
            d.position = hit.point + hit.normal * 0.006f;
            d.rotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            d.localScale = Vector3.one * Random.Range(0.07f, 0.11f);
        }

        void Emit(ParticleSystem ps, Vector3 pos, Vector3 dir, int count)
        {
            ps.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir.sqrMagnitude > 0.001f ? dir : Vector3.up));
            ps.Emit(count);
        }

        ParticleSystem Burst(string name, Material mat, Color color, float minSize, float maxSize, float speed, float life, float gravity, int max)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = max * 8;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.01f;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.6f)));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            if (gravity > 0.5f) { r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.04f; r.lengthScale = 1f; }
            return ps;
        }

        static Mesh QuadMesh()
        {
            var m = new Mesh { name = "DecalQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
