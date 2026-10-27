using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Echoes.Audio;

namespace Echoes.Rendering
{
    /// <summary>
    /// A single mortar firework set on the ground: `Ignite()` fizzes the fuse, fires the shell up on a spark trail and
    /// bursts it into the Smilers' sign, a cyan grin (face ring, two eyes, a smile) with white glitter, lighting the
    /// streets around it for a moment. Used in sequence 4 (a Smiler's "hello" to the stream); reusable for later scenes.
    /// </summary>
    public class Firework : MonoBehaviour
    {
        public Color color = new Color(0.35f, 1f, 1f);
        public float height = 32f;

        ParticleSystem fuse, trail, stars, glitter;
        Light fuseLight;
        bool lit;

        public static Firework Place(Vector3 groundPos, float yaw)
        {
            var go = new GameObject("Firework");
            go.transform.SetPositionAndRotation(groundPos, Quaternion.Euler(0f, yaw, 0f));
            // The tube: red card with a black band, on a square base.
            PrimitiveMeshes.Part(go.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.015f, 0f), new Vector3(0.22f, 0.03f, 0.22f),
                                 RuntimeMaterials.Lit(new Color(0.25f, 0.2f, 0.14f), 0.2f));
            PrimitiveMeshes.Part(go.transform, "Tube", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.12f, 0.17f, 0.12f),
                                 RuntimeMaterials.Lit(new Color(0.55f, 0.06f, 0.05f), 0.25f));
            PrimitiveMeshes.Part(go.transform, "Band", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.125f, 0.025f, 0.125f),
                                 RuntimeMaterials.Lit(new Color(0.03f, 0.03f, 0.03f), 0.3f));
            return go.AddComponent<Firework>();
        }

        void Awake()
        {
            var spark = RuntimeMaterials.Particle("FireworkSpark", ProcTextures.Dot(), true, 7f, 0.05f);
            fuse = Sparks("Fuse", spark, new Color(1f, 0.75f, 0.4f), 0.03f, 1.2f, 0.4f, 0.6f);
            fuse.transform.localPosition = new Vector3(0.07f, 0.06f, 0f);
            trail = Sparks("Trail", spark, new Color(1f, 0.8f, 0.5f), 0.08f, 0.8f, 0.7f, 0.9f);
            stars = Sparks("Stars", spark, color, 0.5f, 0f, 2.4f, 0.08f);
            glitter = Sparks("Glitter", spark, Color.white, 0.22f, 12f, 1.8f, 0.25f);
            foreach (var ps in new[] { stars, glitter })
            {
                // Air drag holds the shape for a moment before the stars sag and fall.
                var lv = ps.limitVelocityOverLifetime;
                lv.enabled = true;
                lv.limit = 1000f;
                lv.drag = 1.4f;
                var tr = ps.trails;
                tr.enabled = true;
                tr.lifetime = 0.18f;
                tr.widthOverTrail = 0.6f;
                tr.dieWithParticles = true;
                ps.GetComponent<ParticleSystemRenderer>().trailMaterial = ps.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            }
        }

        public void Ignite()
        {
            if (lit) return;
            lit = true;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            var am = AudioManager.Instance;
            // Fuse.
            fuseLight = PointLight("FuseLight", transform.position + Vector3.up * 0.2f, new Color(1f, 0.6f, 0.3f), 9f);
            var em = fuse.emission;
            em.enabled = true;
            em.rateOverTime = 70f;
            fuse.Play();
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                fuseLight.intensity = 1.2f + Random.value * 0.9f;
                yield return null;
            }
            em.enabled = false;
            Destroy(fuseLight.gameObject);

            // Launch: the shell climbs on a spark trail.
            if (am != null) am.PlayAt(SoundId.FireworkLaunch, transform.position + Vector3.up * 0.3f, 1f, Random.Range(0.95f, 1.05f), 4f, 260f);
            Vector3 from = transform.position + Vector3.up * 0.35f;
            Vector3 to = from + Vector3.up * height + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            var shellLight = PointLight("ShellLight", from, new Color(1f, 0.7f, 0.4f), 14f);
            var tem = trail.emission;
            tem.enabled = true;
            tem.rateOverTime = 140f;
            trail.Play();
            const float climb = 1.35f;
            for (float t = 0f; t < climb; t += Time.deltaTime)
            {
                float k = t / climb;
                Vector3 p = Vector3.Lerp(from, to, 1f - (1f - k) * (1f - k)); // slowing as it climbs
                trail.transform.position = p;
                shellLight.transform.position = p;
                shellLight.intensity = 3f;
                yield return null;
            }
            tem.enabled = false;
            Destroy(shellLight.gameObject);

            // Burst.
            if (am != null) am.PlayAt(SoundId.FireworkBurst, to, 1f, Random.Range(0.95f, 1.05f), 25f, 600f);
            Grin(to);
            var flash = PointLight("BurstLight", to, color, 90f);
            flash.shadows = LightShadows.None;
            for (float t = 0f; t < 2.6f; t += Time.deltaTime)
            {
                flash.intensity = 60f * Mathf.Exp(-t * 2.2f) * (0.85f + Random.value * 0.15f);
                yield return null;
            }
            Destroy(flash.gameObject);
            yield return new WaitForSeconds(3f);
            // The spent tube stays; the effects go.
            foreach (var ps in new[] { fuse, trail, stars, glitter }) Destroy(ps.gameObject);
            Destroy(this);
        }

        /// <summary>The grin, drawn in stars on a plane facing the camera: face ring, two eyes, a smile, plus loose glitter.</summary>
        void Grin(Vector3 c)
        {
            var cam = Camera.main;
            Vector3 fwd = cam != null ? (c - cam.transform.position).normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 up = Vector3.Cross(fwd, right);
            const float speed = 14f; // with drag 1.4 a star travels speed/drag ≈ 10 m
            var ep = new ParticleSystem.EmitParams();

            void Star(float x, float y, float jitter)
            {
                ep.position = c;
                ep.velocity = (right * x + up * y) * speed + Random.insideUnitSphere * jitter;
                ep.startColor = color;
                ep.startSize = Random.Range(0.4f, 0.6f);
                ep.startLifetime = Random.Range(2f, 2.6f);
                stars.Emit(ep, 1);
            }
            for (int i = 0; i < 56; i++) { float a = i / 56f * Mathf.PI * 2f; Star(Mathf.Cos(a), Mathf.Sin(a), 0.3f); }
            for (int i = 0; i < 7; i++) { Star(-0.35f + Random.Range(-0.04f, 0.04f), 0.32f + Random.Range(-0.04f, 0.04f), 0.1f); Star(0.35f + Random.Range(-0.04f, 0.04f), 0.32f + Random.Range(-0.04f, 0.04f), 0.1f); }
            for (int i = 0; i < 22; i++) { float a = Mathf.Lerp(200f, 340f, i / 21f) * Mathf.Deg2Rad; Star(Mathf.Cos(a) * 0.6f, Mathf.Sin(a) * 0.6f - 0.02f, 0.15f); }
            glitter.transform.position = c;
            glitter.Emit(120);
        }

        ParticleSystem Sparks(string name, Material mat, Color col, float size, float speed, float life, float gravity)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = col;
            main.gravityModifier = gravity;
            main.maxParticles = 1200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.02f;
            var col2 = ps.colorOverLifetime;
            col2.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col2.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return ps;
        }

        Light PointLight(string name, Vector3 p, Color c, float range)
        {
            var go = new GameObject(name, typeof(UnityEngine.Light));
            go.transform.SetParent(transform, true);
            go.transform.position = p;
            var l = go.GetComponent<UnityEngine.Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = 0f;
            l.shadows = LightShadows.None;
            return l;
        }
    }
}
