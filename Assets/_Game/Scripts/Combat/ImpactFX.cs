using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Echoes.Audio;
using Echoes.Rendering;

namespace Echoes.Combat
{
    /// <summary>
    /// Bullet impacts: pooled decals, one-shot particle bursts and surface-matched sounds. Hits on people bleed:
    /// a puff at the wound, an exit spray of droplets, spatter on the wall or ground behind, a few drips, and a
    /// pool that spreads under the dead. A headshot throws a mist and twice the spatter, with its own wet crack.
    /// </summary>
    public class ImpactFX : MonoBehaviour
    {
        static ImpactFX instance;
        const int DecalCount = 80, SplatCount = 60, PoolCount = 10;

        Transform[] decals, splats, pools;
        MeshRenderer[] splatRenderers;
        int nextDecal, nextSplat, nextPool;
        readonly int[] poolGeneration = new int[PoolCount];
        ParticleSystem dust, sparks, blood, glass, bloodDrops, bloodMist;
        Material splatMat, spatterMat, poolMat;
        readonly System.Collections.Generic.Dictionary<ParticleSystem, Vector2> baseSize = new System.Collections.Generic.Dictionary<ParticleSystem, Vector2>();

        // A little self-light keeps blood red in the dark instead of black; the flashlight adds the wet highlight.
        static readonly Color BloodTint = new Color(0.62f, 0.025f, 0.02f);
        static readonly Color BloodGlow = new Color(0.16f, 0.006f, 0.005f);

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
            glass = Burst("Glass", dotMat, new Color(0.7f, 0.8f, 0.8f, 0.8f), 0.015f, 0.03f, 3f, 0.6f, 1f, 18);

            // Blood: bright, saturated and big enough to read across a street at night (owner: it vanished at range).
            var bloodPuffMat = RuntimeMaterials.Particle("BloodPuff", ProcTextures.Puff(), false, 1.6f, 0.1f);
            var dropMat = RuntimeMaterials.Particle("BloodDrop", ProcTextures.Dot(), false, 1.7f, 0.02f);
            blood = Burst("Blood", bloodPuffMat, new Color(0.7f, 0.04f, 0.03f, 0.95f), 0.12f, 0.3f, 1.1f, 0.5f, 0.2f, 16);
            bloodDrops = Burst("BloodDrops", dropMat, new Color(0.75f, 0.03f, 0.02f, 1f), 0.025f, 0.06f, 5f, 0.8f, 1.4f, 60);
            bloodMist = Burst("BloodMist", bloodPuffMat, new Color(0.8f, 0.1f, 0.08f, 0.75f), 0.4f, 0.9f, 0.9f, 1.1f, 0f, 14);
            foreach (var ps in new[] { blood, bloodDrops, bloodMist }) baseSize[ps] = new Vector2(ps.main.startSize.constantMin, ps.main.startSize.constantMax);

            // Blood on surfaces is lit (wet in the flashlight) and lives in its own pools so it never steals bullet holes.
            splatMat = RuntimeMaterials.Decal("BloodSplat", ProcTextures.BloodSplat(), BloodTint, 0.88f, BloodGlow);
            spatterMat = RuntimeMaterials.Decal("BloodSpatter", ProcTextures.BloodSpatter(), BloodTint, 0.88f, BloodGlow);
            poolMat = RuntimeMaterials.Decal("BloodPool", ProcTextures.BloodPool(), new Color(0.5f, 0.02f, 0.02f), 0.94f, BloodGlow * 0.8f);
            splats = new Transform[SplatCount];
            splatRenderers = new MeshRenderer[SplatCount];
            for (int i = 0; i < SplatCount; i++) splats[i] = DecalObject("BloodSplat", quad, splatMat, out splatRenderers[i]);
            pools = new Transform[PoolCount];
            for (int i = 0; i < PoolCount; i++) pools[i] = DecalObject("BloodPool", quad, poolMat, out _);
        }

        Transform DecalObject(string name, Mesh quad, Material mat, out MeshRenderer mr)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = quad;
            mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            go.SetActive(false);
            return go.transform;
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
                    fx.Bleed(hit, shotDir);
                    return; // no decal on characters
                case SurfaceKind.Metal:
                    fx.Emit(fx.sparks, hit.point, Vector3.Lerp(n, reflect, 0.6f), 12);
                    Sound(SoundId.ImpactMetal, hit.point, 0.8f);
                    if (hit.collider.GetComponentInParent<Armored>() != null) return; // armour on a moving man: no bullet hole left hanging in the air
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

        /// <summary>A bullet through a person: puff at the wound, droplets out the back, spatter where they land.</summary>
        void Bleed(RaycastHit hit, Vector3 dir)
        {
            hit.collider.TryGetComponent(out Hitbox hb);
            bool head = hb != null && hb.part == BodyPart.Head;
            Transform body = hb != null && hb.owner != null ? hb.owner.transform : hit.collider.transform;

            // Far away the burst grows (up to 3x) so it still shows up as more than a pixel or two.
            var cam = Camera.main;
            float range = cam != null ? Mathf.Clamp(Vector3.Distance(cam.transform.position, hit.point) / 9f, 1f, 3f) : 1f;
            EmitBlood(blood, range, hit.point, -dir + Vector3.up * 0.2f, head ? 10 : 6);
            EmitBlood(bloodDrops, range, hit.point + dir * 0.08f, dir + Vector3.up * 0.15f, head ? 55 : 22);
            EmitBlood(bloodMist, range, hit.point + dir * 0.12f, dir, head ? 12 : 3);
            Sound(SoundId.ImpactFlesh, hit.point, 0.9f);
            if (head) Sound(SoundId.ImpactHead, hit.point, 1f);

            // Exit spatter: on the wall behind if there is one close enough, otherwise on the ground behind.
            Vector3 o = hit.point + dir * 0.05f;
            if (SurfaceUtil.RaycastIgnoring(o, dir, head ? 3.2f : 2.4f, body, out RaycastHit wall) && CanStain(wall))
                PlaceSplat(spatterMat, wall, dir, head ? Random.Range(1.1f, 1.6f) : Random.Range(0.55f, 0.85f), 1.35f);
            else
            {
                Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
                Vector3 land = hit.point + flat * Random.Range(0.6f, 1.4f) * (head ? 1.5f : 1f);
                if (SurfaceUtil.RaycastIgnoring(land, Vector3.down, 3f, body, out RaycastHit g) && CanStain(g))
                    PlaceSplat(spatterMat, g, flat, head ? Random.Range(0.9f, 1.3f) : Random.Range(0.5f, 0.7f), 1.35f);
            }
            StartCoroutine(Drips(hit.point, body, head ? 3 : Random.Range(1, 3)));
        }

        IEnumerator Drips(Vector3 from, Transform body, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForSeconds(Random.Range(0.15f, 0.5f));
                Vector3 p = from + new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
                if (SurfaceUtil.RaycastIgnoring(p, Vector3.down, 2.5f, body, out RaycastHit g) && CanStain(g))
                    PlaceSplat(splatMat, g, Random.onUnitSphere, Random.Range(0.14f, 0.26f), 1f);
            }
        }

        /// <summary>Blood stays on the static world: not on people, not on things that move.</summary>
        static bool CanStain(RaycastHit h)
        {
            if (h.collider.GetComponent<Hitbox>() != null || h.collider.GetComponentInParent<Health>() != null) return false;
            return h.rigidbody == null || h.rigidbody.isKinematic;
        }

        void PlaceSplat(Material mat, RaycastHit hit, Vector3 along, float size, float stretch)
        {
            var d = splats[nextSplat];
            splatRenderers[nextSplat].sharedMaterial = mat;
            nextSplat = (nextSplat + 1) % SplatCount;
            d.gameObject.SetActive(true);
            d.position = hit.point + hit.normal * 0.004f;
            // The texture's +V points along the travel of the blood, projected onto the surface.
            Vector3 up = Vector3.ProjectOnPlane(along, hit.normal);
            if (up.sqrMagnitude < 1e-4f) up = Vector3.ProjectOnPlane(Random.onUnitSphere, hit.normal);
            d.rotation = Quaternion.LookRotation(-hit.normal, up);
            d.localScale = new Vector3(size, size * stretch, 1f);
        }

        /// <summary>A pool spreads out from under a body once it has come to rest.</summary>
        public static void BloodPool(Transform pelvis, Transform body)
        {
            if (pelvis == null) return;
            Instance.StartCoroutine(Instance.Pool(pelvis, body));
        }

        IEnumerator Pool(Transform pelvis, Transform body)
        {
            yield return new WaitForSeconds(1.8f); // the ragdoll settles
            if (pelvis == null) yield break;
            if (!SurfaceUtil.RaycastIgnoring(pelvis.position + Vector3.up * 0.3f, Vector3.down, 2f, body, out RaycastHit g) || !CanStain(g)) yield break;
            int slot = nextPool, gen = ++poolGeneration[slot]; // a newer pool may take this slot over
            var d = pools[slot];
            nextPool = (nextPool + 1) % PoolCount;
            d.gameObject.SetActive(true);
            d.position = g.point + g.normal * 0.003f;
            d.rotation = Quaternion.LookRotation(-g.normal, Vector3.ProjectOnPlane(Random.onUnitSphere, g.normal));
            float target = Random.Range(1.5f, 2f), aspect = Random.Range(0.75f, 0.95f);
            for (float t = 0f; t < 30f && poolGeneration[slot] == gen; t += Time.deltaTime)
            {
                float s = Mathf.Lerp(0.15f, target, 1f - Mathf.Exp(-t / 7f)); // fast at first, then slower and slower
                d.localScale = new Vector3(s, s * aspect, 1f);
                yield return null;
            }
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

        void EmitBlood(ParticleSystem ps, float scale, Vector3 pos, Vector3 dir, int count)
        {
            var main = ps.main;
            Vector2 s = baseSize[ps];
            main.startSize = new ParticleSystem.MinMaxCurve(s.x * scale, s.y * scale);
            Emit(ps, pos, dir, count);
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
