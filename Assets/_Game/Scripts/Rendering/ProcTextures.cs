using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Rendering
{
    /// <summary>Runtime-generated textures. Results are cached; textures are never saved to disk.</summary>
    public static class ProcTextures
    {
        static Texture2D flashlightCookie, grain, scanlines;

        /// <summary>LED tactical light: tight hot-spot, faint reflector ring, soft spill, slight angular streaking.</summary>
        public static Texture2D FlashlightCookie(int size)
        {
            if (flashlightCookie != null) return flashlightCookie;
            var tex = NewTex(size, size, TextureFormat.RGBA32, TextureWrapMode.Clamp, "FlashlightCookie", true);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Atan2(v, u);

                float hot = Mathf.Exp(-r * r / 0.018f);
                float ring = Mathf.Exp(-Mathf.Pow((r - 0.3f) / 0.04f, 2f)) * 0.22f;
                float spill = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.15f, 0.97f, r)) * 0.42f;
                float streak = 1f + (Mathf.PerlinNoise(Mathf.Cos(a) * 3f + 10f, Mathf.Sin(a) * 3f + 10f) - 0.5f) * 0.25f * Mathf.Clamp01(r * 2f);
                float val = Mathf.Clamp01((hot + ring + spill) * streak);
                if (r > 0.98f) val = 0f;
                byte b = (byte)(val * 255f);
                px[y * size + x] = new Color32(b, b, b, b);
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return flashlightCookie = tex;
        }

        /// <summary>Tileable monochrome noise used for the sensor-grain overlay.</summary>
        public static Texture2D Grain(int size = 256)
        {
            if (grain != null) return grain;
            var tex = NewTex(size, size, TextureFormat.RGBA32, TextureWrapMode.Repeat, "Grain");
            tex.filterMode = FilterMode.Point;
            var px = new Color32[size * size];
            var rng = new System.Random(1337);
            for (int i = 0; i < px.Length; i++)
            {
                byte b = (byte)rng.Next(256);
                px[i] = new Color32(b, b, b, 255);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return grain = tex;
        }

        /// <summary>1x4 pattern; tile vertically at screen height / 2 for interlace lines.</summary>
        public static Texture2D Scanlines()
        {
            if (scanlines != null) return scanlines;
            var tex = NewTex(1, 4, TextureFormat.RGBA32, TextureWrapMode.Repeat, "Scanlines");
            tex.filterMode = FilterMode.Point;
            tex.SetPixels32(new[]
            {
                new Color32(0, 0, 0, 255), new Color32(0, 0, 0, 90),
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0)
            });
            tex.Apply(false, true);
            return scanlines = tex;
        }

        static Texture2D bulletHole, muzzleStar, puff, dot;


        /// <summary>Dark entry hole with a chipped, cracked rim (alpha-blended decal).</summary>
        public static Texture2D BulletHole(int size = 64)
        {
            if (bulletHole != null) return bulletHole;
            var tex = NewTex(size, size, TextureFormat.RGBA32, TextureWrapMode.Clamp, "BulletHole", true);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                float jag = Mathf.PerlinNoise(Mathf.Cos(a) * 2f + 5f, Mathf.Sin(a) * 2f + 5f);
                float hole = 1f - Mathf.SmoothStep(0.12f, 0.2f, r - jag * 0.06f);
                float chip = (1f - Mathf.SmoothStep(0.25f, 0.7f, r + (jag - 0.5f) * 0.3f)) * 0.55f;
                float crack = Mathf.Pow(Mathf.Abs(Mathf.Sin(a * 5f + jag * 4f)), 40f) * (1f - Mathf.SmoothStep(0.3f, 0.95f, r)) * 0.6f;
                float alpha = Mathf.Clamp01(Mathf.Max(hole, Mathf.Max(chip, crack)));
                byte shade = (byte)(Mathf.Lerp(40f, 4f, hole) );
                px[y * size + x] = new Color32(shade, shade, shade, (byte)(alpha * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return bulletHole = tex;
        }

        /// <summary>Four-point muzzle flash star with hot core.</summary>
        public static Texture2D MuzzleStar(int size = 128)
        {
            if (muzzleStar != null) return muzzleStar;
            var tex = NewTex(size, size, TextureFormat.RGBA32, TextureWrapMode.Clamp, "MuzzleStar", true);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                float rays = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2f)), 12f) * Mathf.Clamp01(1f - r);
                float core = Mathf.Exp(-r * r * 18f);
                float val = Mathf.Clamp01(core + rays * 0.8f);
                byte b = (byte)(val * 255f);
                px[y * size + x] = new Color32(b, b, b, b);
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return muzzleStar = tex;
        }

        public static Texture2D Puff(int size = 64)
        {
            if (puff != null) return puff;
            var tex = NewTex(size, size, TextureFormat.RGBA32, TextureWrapMode.Clamp, "Puff", true);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size - 0.5f, v = (y + 0.5f) / size - 0.5f;
                float r = Mathf.Sqrt(u * u + v * v) * 2f;
                float n = Mathf.PerlinNoise(u * 6f + 3f, v * 6f + 7f);
                float a = Mathf.Clamp01((1f - r) * 1.5f - (1f - n) * 0.7f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return puff = tex;
        }

        public static Texture2D Dot(int size = 32)
        {
            if (dot != null) return dot;
            var tex = NewTex(size, size, TextureFormat.RGBA32, TextureWrapMode.Clamp, "Dot", true);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size - 0.5f, v = (y + 0.5f) / size - 0.5f;
                byte a = (byte)(Mathf.Exp(-(u * u + v * v) * 40f) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return dot = tex;
        }

        // ------------------------------------------------------------------ blood (RGB = thickness shading, tinted by the material)

        static Texture2D bloodSplat, bloodSpatter, bloodPool;

        /// <summary>A round splat with a ragged rim and satellite droplets (drips, hits on the ground).</summary>
        public static Texture2D BloodSplat(int size = 128)
        {
            if (bloodSplat != null) return bloodSplat;
            var rng = new System.Random(71);
            var drops = new List<Vector4>(); // x, y, radius, stretch
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = 0.5f + (float)rng.NextDouble() * 0.42f;
                drops.Add(new Vector4(Mathf.Cos(a) * d, Mathf.Sin(a) * d, Mathf.Lerp(0.07f, 0.02f, (d - 0.5f) / 0.42f), 1f));
            }
            return bloodSplat = BloodTex(size, "BloodSplat", (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                float rim = 0.4f + (Mathf.PerlinNoise(Mathf.Cos(a) * 1.7f + 11f, Mathf.Sin(a) * 1.7f + 3f) - 0.5f) * 0.22f;
                float blob = 1f - Mathf.SmoothStep(rim - 0.03f, rim + 0.01f, r);
                return Mathf.Max(blob, Droplets(drops, u, v, Vector2.zero));
            });
        }

        /// <summary>Directional exit spatter: a heavy blob at the bottom, streaks and droplets thrown toward +V.</summary>
        public static Texture2D BloodSpatter(int size = 128)
        {
            if (bloodSpatter != null) return bloodSpatter;
            var rng = new System.Random(72);
            var drops = new List<Vector4>();
            for (int i = 0; i < 46; i++)
            {
                float spread = ((float)rng.NextDouble() - 0.5f) * 1.2f;   // ±35°
                float d = Mathf.Pow((float)rng.NextDouble(), 0.8f) * 1.35f;
                drops.Add(new Vector4(Mathf.Sin(spread) * d, -0.55f + Mathf.Cos(spread) * d,
                                      Mathf.Lerp(0.085f, 0.014f, d / 1.35f) * (0.6f + (float)rng.NextDouble() * 0.6f), 1f + d * 1.8f));
            }
            return bloodSpatter = BloodTex(size, "BloodSpatter", (u, v) =>
            {
                float du = u, dv = v + 0.55f;
                float r = Mathf.Sqrt(du * du * 1.2f + dv * dv), a = Mathf.Atan2(dv, du);
                float rim = 0.26f + (Mathf.PerlinNoise(Mathf.Cos(a) * 2f + 4f, Mathf.Sin(a) * 2f + 9f) - 0.5f) * 0.16f;
                float blob = 1f - Mathf.SmoothStep(rim - 0.03f, rim + 0.01f, r);
                return Mathf.Max(blob, Droplets(drops, u, v, new Vector2(0f, -0.55f)));
            });
        }

        /// <summary>A pool under a body: one smooth, wide, wet blob with a low-frequency rim.</summary>
        public static Texture2D BloodPool(int size = 128)
        {
            if (bloodPool != null) return bloodPool;
            return bloodPool = BloodTex(size, "BloodPool", (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                float rim = 0.78f + (Mathf.PerlinNoise(Mathf.Cos(a) * 1.1f + 21f, Mathf.Sin(a) * 1.1f + 6f) - 0.5f) * 0.4f;
                return 1f - Mathf.SmoothStep(rim - 0.025f, rim + 0.005f, r);
            });
        }

        /// <summary>Droplets as ellipses stretched away from the source point (x, y = centre, z = radius, w = stretch).</summary>
        static float Droplets(List<Vector4> drops, float u, float v, Vector2 source)
        {
            float best = 0f;
            foreach (var d in drops)
            {
                Vector2 axis = new Vector2(d.x - source.x, d.y - source.y);
                axis = axis.sqrMagnitude > 1e-4f ? axis.normalized : Vector2.up;
                float pu = u - d.x, pv = v - d.y;
                float along = (pu * axis.x + pv * axis.y) / (d.z * d.w), across = (pu * -axis.y + pv * axis.x) / d.z;
                float e = along * along + across * across;
                best = Mathf.Max(best, 1f - Mathf.SmoothStep(0.75f, 1f, e));
            }
            return best;
        }

        /// <summary>Alpha from a coverage function over [-1, 1]²; RGB darkens where the blood is thick (the middle).</summary>
        static Texture2D BloodTex(int size, string name, System.Func<float, float, float> coverage)
        {
            var tex = NewTex(size, size, TextureFormat.RGBA32, TextureWrapMode.Clamp, name, true);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(coverage(u, v));
                float thick = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v)) * 0.35f + Mathf.PerlinNoise(u * 4f + 30f, v * 4f + 2f) * 0.15f;
                byte shade = (byte)(Mathf.Lerp(255f, 150f, thick));
                px[y * size + x] = new Color32(shade, shade, shade, (byte)(a * 245f));
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D marinePhoto;

        /// <summary>
        /// Daniel's boot-camp portrait, the kind that hangs framed in a family home: rendered in 3D by MarinePortrait
        /// (owner: the drawn one didn't look real), then printed: filmic tone, faded warm colour, grain, vignette, a gloss
        /// glare, the white border, yellowing, a crease, worn corners and scratches. About a third of a second, once.
        /// </summary>
        public static Texture2D MarinePhoto()
        {
            if (marinePhoto != null) return marinePhoto;
            const int w = 320, h = 400, border = 14;
            int iw = w - 2 * border, ih = h - 2 * border;
            // The picture itself: a lit 3D portrait (MarinePortrait), then printed and aged on Mia's wall.
            var img = MarinePortrait.Render(iw, ih);
            var soft = new Color[img.Length];
            for (int y = 0; y < ih; y++)
            for (int x = 0; x < iw; x++)
            {
                // A touch of lens softness (also smooths the ray-marched edges).
                Color s = img[y * iw + x] * 4f;
                int n = 4;
                for (int k = 0; k < 4; k++)
                {
                    int xx = Mathf.Clamp(x + (k == 0 ? 1 : k == 1 ? -1 : 0), 0, iw - 1), yy = Mathf.Clamp(y + (k == 2 ? 1 : k == 3 ? -1 : 0), 0, ih - 1);
                    s += img[yy * iw + xx];
                    n++;
                }
                soft[y * iw + x] = s / n;
            }

            var tex = NewTex(w, h, TextureFormat.RGBA32, TextureWrapMode.Clamp, "MarinePhoto", true);
            var px = new Color32[w * h];
            var paper = new Color(0.92f, 0.9f, 0.84f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c;
                bool inside = x >= border && x < w - border && y >= border && y < h - border;
                if (!inside)
                {
                    c = paper * (0.94f + 0.06f * MarinePortrait.Hash(x, y, 3));
                }
                else
                {
                    float u = (x - border) / (float)iw, v = (y - border) / (float)ih;
                    Color l = soft[(y - border) * iw + (x - border)];
                    // Filmic shoulder, then sRGB, then the print's fade: lifted blacks, warm, a little less saturated.
                    l = new Color(l.r / (1f + l.r), l.g / (1f + l.g), l.b / (1f + l.b)) * 1.35f;
                    c = new Color(Mathf.Pow(Mathf.Clamp01(l.r), 1f / 2.2f), Mathf.Pow(Mathf.Clamp01(l.g), 1f / 2.2f), Mathf.Pow(Mathf.Clamp01(l.b), 1f / 2.2f));
                    float grey = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    c = Color.Lerp(new Color(grey, grey, grey), c, 0.82f);
                    c = new Color(0.07f, 0.055f, 0.04f) + c * 0.9f;
                    c = new Color(c.r * 1.04f, c.g * 0.99f, c.b * 0.88f);
                    // Grain, a vignette, and the gloss catching the room light across the top.
                    c *= 0.96f + 0.08f * MarinePortrait.Hash(x, y, 11);
                    float vig = 1f - 0.4f * Mathf.Pow(Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.55f)) * 1.35f), 2.2f);
                    c *= vig;
                    float glare = Mathf.Exp(-Mathf.Pow((u + v * 0.6f - 1.25f) / 0.12f, 2f)) * 0.07f;
                    c += new Color(glare, glare, glare);
                }
                // Age over everything: yellowing, a crease across the lower corner, worn corners, a few scratches.
                c = Color.Lerp(c, new Color(0.78f, 0.66f, 0.45f), 0.12f);
                float crease = (x - y * 0.7f) - 150f;
                if (Mathf.Abs(crease) < 1.4f) c *= 1.16f;
                else if (crease > 1.4f && crease < 3.5f) c *= 0.93f;
                int cx = Mathf.Min(x, w - 1 - x), cy = Mathf.Min(y, h - 1 - y);
                if (cx + cy < 12 && MarinePortrait.Hash(x, y, 21) > 0.35f) c = Color.Lerp(c, paper, 0.7f);
                if (MarinePortrait.Hash(x / 2, (int)(y * 0.08f + x * 0.5f), 31) > 0.9985f) c *= 1.2f;
                c.a = 1f;
                px[y * w + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return marinePhoto = tex;
        }

        static Texture2D NewTex(int w, int h, TextureFormat fmt, TextureWrapMode wrap, string name, bool mips = false)
        {
            return new Texture2D(w, h, fmt, mips, false)
            {
                name = name,
                wrapMode = wrap,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
                hideFlags = HideFlags.DontSave
            };
        }
    }
}
