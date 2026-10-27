using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Echoes.Rendering
{
    /// <summary>
    /// Renders Daniel's boot-camp portrait (Parris Island, 2016) for the photo on the school door: a small ray marcher
    /// over a signed-distance bust instead of a drawing (owner: the photo should look real). A sculpted head (jaw, chin,
    /// cheekbones, brow, nose, sockets with eyes looking at the lens, lips, ears, a high-and-tight cut) on a neck, the
    /// dress-blue coat with its high collar and red piping, brass buttons and a ribbon bar, and the white cover with
    /// its black patent visor and gold emblem; lit like a studio portrait (warm key with soft shadows, cool fill, a
    /// rim light, occlusion) in front of an out-of-focus flag. Returns linear colour; ProcTextures ages the print.
    /// Pure math, so rows render in parallel.
    /// </summary>
    public static class MarinePortrait
    {
        const int Skin = 0, Eye = 1, Lips = 2, Blues = 3, White = 4, Visor = 5, Brass = 6;

        static readonly Vector3 HeadC = new Vector3(0f, 1.675f, 0f);
        static readonly Vector3 CamPos = new Vector3(0f, 1.6f, 2.1f);
        static readonly Vector3 Target = new Vector3(0f, 1.57f, 0f);
        const float TanHalf = 0.145f;
        const float Yaw = 10f * Mathf.Deg2Rad;   // turned a little off the lens, as these portraits are

        static readonly Vector3 Key = new Vector3(0.55f, 0.62f, 0.56f).normalized;
        static readonly Vector3 Fill = new Vector3(-0.75f, 0.1f, 0.65f).normalized;
        static readonly Vector3 Rim = new Vector3(-0.45f, 0.55f, -0.7f).normalized;
        static readonly float[] ButtonY = { 1.415f, 1.345f, 1.275f, 1.205f };
        static float[] buttonZ;

        public static Color[] Render(int w, int h)
        {
            var px = new Color[w * h];
            buttonZ = new float[ButtonY.Length];
            for (int i = 0; i < ButtonY.Length; i++) buttonZ[i] = FrontOfCoat(ButtonY[i]);
            Vector3 fwd = (Target - CamPos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 up = Vector3.Cross(fwd, right);
            float aspect = w / (float)h;
            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    Vector3 dir = (fwd + right * ((u - 0.5f) * 2f * TanHalf * aspect) + up * ((v - 0.5f) * 2f * TanHalf)).normalized;
                    px[y * w + x] = Trace(dir, u, v);
                }
            });
            return px;
        }

        // ------------------------------------------------------------------ shading

        static Color Trace(Vector3 dir, float u, float v)
        {
            float t = 1.5f;
            for (int i = 0; i < 110 && t < 2.9f; i++)
            {
                Vector3 p = CamPos + dir * t;
                float d = Map(p, out int mat);
                if (d < 0.00025f) return Shade(p, dir, mat);
                t += d * 0.9f;
            }
            return Backdrop(u, v);
        }

        static Color Shade(Vector3 p, Vector3 rd, int mat)
        {
            Vector3 n = Normal(p);
            Vector3 view = -rd;
            Vector3 s = Turn(p);
            Color albedo = Albedo(s, mat, out float spec, out float gloss, out bool metal);

            float ndl = Vector3.Dot(n, Key);
            float key = mat == Skin || mat == Lips ? Mathf.Clamp01((ndl + 0.3f) / 1.3f) : Mathf.Max(0f, ndl); // skin wraps the light
            if (key > 0f) key *= SoftShadow(p + n * 0.0015f, Key);
            float fill = Mathf.Max(0f, Vector3.Dot(n, Fill)) * 0.22f;
            float rimL = Mathf.Max(0f, Vector3.Dot(n, Rim)) * Mathf.Pow(1f - Mathf.Clamp01(Vector3.Dot(n, view)), 2f) * 0.9f;
            float ao = Occlusion(p, n);

            var keyCol = new Color(1f, 0.9f, 0.78f) * 1.35f;
            var fillCol = new Color(0.62f, 0.72f, 0.95f);
            Color diffuse = albedo * (keyCol * key + fillCol * fill + new Color(0.55f, 0.6f, 0.7f) * (0.13f * ao)) + new Color(0.9f, 0.92f, 1f) * (rimL * 0.35f);
            if (mat == Skin || mat == Lips) diffuse += new Color(0.06f, 0.012f, 0.004f) * key * ao; // light through skin is red

            Vector3 hv = (Key + view).normalized;
            float sp = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, hv)), gloss) * spec * key;
            Color specCol = metal ? albedo * 2f : new Color(1f, 0.95f, 0.88f);
            return diffuse + specCol * sp;
        }

        static Color Albedo(Vector3 s, int mat, out float spec, out float gloss, out bool metal)
        {
            spec = 0.04f; gloss = 18f; metal = false;
            Vector3 h = s - HeadC;
            switch (mat)
            {
                case Eye:
                {
                    spec = 0.9f; gloss = 120f;
                    float side = Mathf.Sign(h.x);
                    Vector3 eyeC = new Vector3(side * 0.031f, 0.002f, 0.077f);
                    Vector3 toCam = Turn(CamPos) - (HeadC + eyeC);
                    float a = Vector3.Dot((h - eyeC).normalized, toCam.normalized);
                    if (a > 0.985f) return new Color(0.004f, 0.004f, 0.004f);             // pupil
                    if (a > 0.93f) return Color.Lerp(new Color(0.03f, 0.016f, 0.008f), new Color(0.1f, 0.055f, 0.025f), (a - 0.93f) / 0.055f); // iris
                    return new Color(0.5f, 0.45f, 0.42f);
                }
                case Lips: spec = 0.08f; gloss = 30f; return new Color(0.33f, 0.12f, 0.09f);
                case Blues:
                {
                    spec = 0.025f; gloss = 10f;
                    if (s.y > 1.532f && s.y < 1.545f) return new Color(0.35f, 0.015f, 0.012f);              // red piping on the collar
                    if (s.x < -0.055f && s.x > -0.135f && s.y > 1.338f && s.y < 1.362f && s.z > 0.03f)       // ribbon bar
                    {
                        int band = (int)((s.x + 0.135f) / 0.0135f);
                        Color[] ribbons = { new Color(0.32f, 0.02f, 0.02f), new Color(0.5f, 0.38f, 0.05f), new Color(0.02f, 0.05f, 0.2f),
                                            new Color(0.45f, 0.45f, 0.42f), new Color(0.32f, 0.02f, 0.02f), new Color(0.5f, 0.38f, 0.05f) };
                        spec = 0.15f;
                        return ribbons[Mathf.Clamp(band, 0, ribbons.Length - 1)];
                    }
                    return new Color(0.011f, 0.013f, 0.038f);
                }
                case White: spec = 0.05f; gloss = 14f; return new Color(0.72f, 0.72f, 0.69f);
                case Visor: spec = 0.9f; gloss = 220f; return new Color(0.008f, 0.008f, 0.009f);
                case Brass: spec = 0.7f; gloss = 70f; metal = true; return new Color(0.5f, 0.33f, 0.09f);
            }
            // Skin: a young recruit, clean-shaven; the high-and-tight shows as shadow on the sides; brows; flushed cheeks.
            spec = 0.05f; gloss = 26f;
            var c = new Color(0.44f, 0.25f, 0.16f);
            float ax = Mathf.Abs(h.x);
            if (h.y > 0.015f && ax > 0.055f && h.z < 0.07f) c = Color.Lerp(c, new Color(0.08f, 0.06f, 0.045f), Mathf.Clamp01((h.y - 0.015f) / 0.025f) * 0.75f);
            if (h.y > 0.027f && h.y < 0.041f && ax > 0.012f && ax < 0.056f && h.z > 0.055f)
                c = Color.Lerp(c, new Color(0.05f, 0.032f, 0.022f), 0.8f * Mathf.Clamp01(1f - Mathf.Abs(h.y - 0.034f - (ax - 0.03f) * 0.1f) / 0.008f));
            c += new Color(0.05f, -0.01f, -0.01f) * Gauss(ax - 0.045f, h.y + 0.03f, 0.02f, 0.02f) * Mathf.Clamp01(h.z * 20f);
            c = Color.Lerp(c, new Color(0.3f, 0.19f, 0.13f), 0.18f * Mathf.Clamp01((-h.y - 0.06f) / 0.04f) * Mathf.Clamp01(h.z * 15f)); // a hint of shadow on the jaw
            return c;
        }

        static Color Backdrop(float u, float v)
        {
            // Mottled studio blue, darker toward the bottom.
            var c = Color.Lerp(new Color(0.035f, 0.045f, 0.07f), new Color(0.09f, 0.11f, 0.15f), Mathf.SmoothStep(0f, 1f, v));
            c *= 0.85f + 0.3f * ValueNoise(u * 5f, v * 5f);
            // The flag, out of focus, hanging behind his right shoulder: soft stripes in gentle folds, the blue canton.
            float flag = 1f - Mathf.SmoothStep(0.3f, 0.52f, u);
            if (flag > 0f)
            {
                float fold = 0.8f + 0.25f * Mathf.Sin(u * 22f + v * 3f);
                float stripe = Mathf.SmoothStep(0.3f, 0.7f, 0.5f + 0.5f * Mathf.Sin((v * 6.5f + 0.06f * Mathf.Sin(u * 9f)) * Mathf.PI * 2f));
                var f = Color.Lerp(new Color(0.24f, 0.02f, 0.025f), new Color(0.42f, 0.4f, 0.36f), stripe) * fold;
                float canton = Mathf.SmoothStep(0.62f, 0.68f, v) * (1f - Mathf.SmoothStep(0.16f, 0.24f, u));
                f = Color.Lerp(f, new Color(0.02f, 0.03f, 0.1f) * fold + new Color(0.08f, 0.08f, 0.08f) * Mathf.Max(0f, ValueNoise(u * 40f, v * 40f) - 0.7f), canton);
                c = Color.Lerp(c, f, flag * 0.9f);
            }
            return c;
        }

        // ------------------------------------------------------------------ the bust

        static Vector3 Turn(Vector3 q)
        {
            float cs = Mathf.Cos(Yaw), sn = Mathf.Sin(Yaw);
            return new Vector3(cs * q.x - sn * q.z, q.y, sn * q.x + cs * q.z);
        }

        static float Map(Vector3 q, out int mat)
        {
            Vector3 p = Turn(q);
            float d = Torso(p, out mat, true);
            float dh = Head(p, out int hm);
            if (dh < d) { d = dh; mat = hm; }
            float dc = Cover(p, out int cm);
            if (dc < d) { d = dc; mat = cm; }
            return d;
        }

        static float Head(Vector3 p, out int mat)
        {
            mat = Skin;
            Vector3 h = p - HeadC;
            if (h.sqrMagnitude > 0.04f) return h.magnitude - 0.16f;
            float d = Ell(h, new Vector3(0.079f, 0.1f, 0.093f));
            d = SMin(d, Ell(h - new Vector3(0f, -0.066f, 0.01f), new Vector3(0.068f, 0.056f, 0.072f)), 0.035f);   // jaw
            d = SMin(d, Ell(h - new Vector3(0f, -0.118f, 0.056f), new Vector3(0.027f, 0.019f, 0.022f)), 0.02f);  // chin
            for (int s = -1; s <= 1; s += 2)
            {
                d = SMin(d, Ell(h - new Vector3(s * 0.046f, -0.018f, 0.058f), new Vector3(0.024f, 0.016f, 0.024f)), 0.02f); // cheekbones
                d = SMin(d, Ell(h - new Vector3(s * 0.073f, -0.01f, -0.008f), new Vector3(0.009f, 0.025f, 0.015f)), 0.006f); // ears
            }
            d = SMin(d, Ell(h - new Vector3(0f, 0.033f, 0.075f), new Vector3(0.056f, 0.011f, 0.018f)), 0.015f);   // brow ridge
            d = SMin(d, Capsule(h, new Vector3(0f, 0.022f, 0.084f), new Vector3(0f, -0.025f, 0.097f), 0.0085f), 0.012f); // bridge
            d = SMin(d, Ell(h - new Vector3(0f, -0.029f, 0.094f), new Vector3(0.0125f, 0.0105f, 0.0115f)), 0.008f);  // tip
            for (int s = -1; s <= 1; s += 2)
            {
                d = SMin(d, Ell(h - new Vector3(s * 0.013f, -0.034f, 0.086f), new Vector3(0.008f, 0.0075f, 0.008f)), 0.006f); // nostril wings
                d = SSub(d, Ell(h - new Vector3(s * 0.031f, 0.003f, 0.09f), new Vector3(0.019f, 0.011f, 0.016f)), 0.009f); // sockets
            }
            float lips = Mathf.Min(Ell(h - new Vector3(0f, -0.074f, 0.077f), new Vector3(0.02f, 0.005f, 0.0075f)),
                                   Ell(h - new Vector3(0f, -0.083f, 0.075f), new Vector3(0.018f, 0.006f, 0.008f)));
            float withLips = SMin(d, lips, 0.005f);
            if (lips < d + 0.0006f) mat = Lips;
            d = withLips;
            float eyes = Mathf.Min((h - new Vector3(-0.031f, 0.002f, 0.077f)).magnitude - 0.0115f, (h - new Vector3(0.031f, 0.002f, 0.077f)).magnitude - 0.0115f);
            // Lids: shells over the eyeballs, cut to leave an almond of eye between them.
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 e = h - new Vector3(s * 0.031f, 0.002f, 0.077f);
                float shell = e.magnitude - 0.0128f;
                float upper = Mathf.Max(shell, 0.0035f - e.y + 0.08f * e.x * e.x / 0.0128f);
                float lower = Mathf.Max(shell, e.y + 0.0055f);
                d = SMin(d, Mathf.Min(upper, lower), 0.002f);
            }
            if (eyes < d) { d = eyes; mat = Eye; }
            return d;
        }

        static float Cover(Vector3 p, out int mat)
        {
            mat = White;
            Vector3 h = p - HeadC;
            if ((h - new Vector3(0f, 0.085f, 0.01f)).sqrMagnitude > 0.03f) return (h - new Vector3(0f, 0.085f, 0.01f)).magnitude - 0.15f;
            float crown = Ell(h - new Vector3(0f, 0.104f, 0.004f), new Vector3(0.112f, 0.024f, 0.122f));
            float band = Ell(h - new Vector3(0f, 0.072f, 0.002f), new Vector3(0.084f, 0.03f, 0.1f));
            float d = SMin(crown, band, 0.012f);
            // The visor: a thin shell sloping down over the eyes.
            Vector3 vq = h - new Vector3(0f, 0.05f, 0.09f);
            float a = -14f * Mathf.Deg2Rad;
            vq = new Vector3(vq.x, Mathf.Cos(a) * vq.y - Mathf.Sin(a) * vq.z, Mathf.Sin(a) * vq.y + Mathf.Cos(a) * vq.z);
            float visor = Ell(vq, new Vector3(0.07f, 0.0045f, 0.043f));
            if (visor < d) { d = visor; mat = Visor; }
            float emblem = Ell(h - new Vector3(0f, 0.08f, 0.1f), new Vector3(0.012f, 0.012f, 0.004f));
            if (emblem < d) { d = emblem; mat = Brass; }
            return d;
        }

        static float Torso(Vector3 p, out int mat, bool buttons)
        {
            mat = Blues;
            Vector3 c = p - new Vector3(0f, 1.3f, 0f);
            if (c.sqrMagnitude > 0.3f) return c.magnitude - 0.5f;
            float d = Ell(p - new Vector3(0f, 1.26f, -0.02f), new Vector3(0.19f, 0.2f, 0.11f));
            d = SMin(d, Ell(p - new Vector3(0f, 1.33f, 0f), new Vector3(0.17f, 0.09f, 0.1f)), 0.04f);              // chest
            d = SMin(d, Capsule(p, new Vector3(-0.16f, 1.405f, -0.02f), new Vector3(0.16f, 1.405f, -0.02f), 0.068f), 0.06f); // shoulders
            for (int s = -1; s <= 1; s += 2)
                d = SMin(d, Capsule(p, new Vector3(s * 0.205f, 1.38f, -0.025f), new Vector3(s * 0.225f, 1.08f, -0.02f), 0.052f), 0.03f);
            d = SMin(d, Ell(p - new Vector3(0f, 1.5f, -0.012f), new Vector3(0.07f, 0.045f, 0.068f)), 0.035f);   // high collar
            float neck = Capsule(p, new Vector3(0f, 1.44f, -0.012f), new Vector3(0f, 1.64f, -0.02f), 0.058f);
            if (neck < d) { d = neck; mat = Skin; }
            if (buttons && buttonZ != null)
                for (int i = 0; i < ButtonY.Length; i++)
                {
                    float b = (p - new Vector3(0f, ButtonY[i], buttonZ[i])).magnitude - 0.0075f;
                    if (b < d) { d = b; mat = Brass; }
                }
            return d;
        }

        /// <summary>Where the coat's front is at height y (so the buttons sit on it).</summary>
        static float FrontOfCoat(float y)
        {
            float z = 0.25f;
            for (int i = 0; i < 80; i++)
            {
                float d = Torso(new Vector3(0f, y, z), out _, false);
                if (d < 0.0002f) break;
                z -= d;
            }
            return z;
        }

        // ------------------------------------------------------------------ helpers

        static Vector3 Normal(Vector3 p)
        {
            const float e = 0.0004f;
            Vector3 a = new Vector3(1f, -1f, -1f), b = new Vector3(-1f, -1f, 1f), c = new Vector3(-1f, 1f, -1f), d = new Vector3(1f, 1f, 1f);
            return (a * Map(p + a * e, out _) + b * Map(p + b * e, out _) + c * Map(p + c * e, out _) + d * Map(p + d * e, out _)).normalized;
        }

        static float SoftShadow(Vector3 p, Vector3 l)
        {
            float res = 1f, t = 0.004f;
            for (int i = 0; i < 28 && t < 0.4f; i++)
            {
                float d = Map(p + l * t, out _);
                if (d < 0.0002f) return 0f;
                res = Mathf.Min(res, 10f * d / t);
                t += Mathf.Clamp(d, 0.003f, 0.04f);
            }
            return Mathf.Clamp01(res);
        }

        static float Occlusion(Vector3 p, Vector3 n)
        {
            float occ = 0f, w = 1f;
            for (int i = 1; i <= 5; i++)
            {
                float h = 0.006f * i;
                occ += (h - Map(p + n * h, out _)) * w;
                w *= 0.7f;
            }
            return Mathf.Clamp01(1f - occ * 18f);
        }

        static float Ell(Vector3 p, Vector3 r)
        {
            float k0 = new Vector3(p.x / r.x, p.y / r.y, p.z / r.z).magnitude;
            float k1 = new Vector3(p.x / (r.x * r.x), p.y / (r.y * r.y), p.z / (r.z * r.z)).magnitude;
            return k1 < 1e-6f ? -Mathf.Min(r.x, Mathf.Min(r.y, r.z)) : k0 * (k0 - 1f) / k1;
        }

        static float Capsule(Vector3 p, Vector3 a, Vector3 b, float r)
        {
            Vector3 pa = p - a, ba = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba));
            return (pa - ba * t).magnitude - r;
        }

        static float SMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        /// <summary>Smoothly carves b out of a.</summary>
        static float SSub(float a, float b, float k) => -SMin(-a, b, k);

        static float Gauss(float x, float y, float sx, float sy) => Mathf.Exp(-(x * x / (sx * sx) + y * y / (sy * sy)));

        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144269504);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        static float ValueNoise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(ix, iy, 5), b = Hash(ix + 1, iy, 5), c = Hash(ix, iy + 1, 5), d = Hash(ix + 1, iy + 1, 5);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
    }
}
