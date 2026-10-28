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
            // The picture itself: a lit 3D portrait (MarinePortrait), then printed and aged on Mia's wall.
            const int iw = 292, ih = 372;
            var img = Soften(MarinePortrait.Render(iw, ih), iw, ih);
            return marinePhoto = Print("MarinePhoto", img, iw, ih, 14, 14, 150f, null);
        }

        static Texture2D unitPhoto;

        /// <summary>
        /// The old unit photo Vance left on Mia's bed (8b): four Marines in dress blues (MarinePortrait.RenderGroup),
        /// printed and aged like the portrait, a typed caption on the bottom border, and Vance's face scratched out of
        /// the emulsion down to the paper.
        /// </summary>
        public static Texture2D UnitPhoto()
        {
            if (unitPhoto != null) return unitPhoto;
            const int iw = 540, ih = 200, side = 14, bottom = 34;
            var img = Soften(MarinePortrait.RenderGroup(iw, ih), iw, ih);
            Vector2 face = MarinePortrait.HeadPixel(2, iw, ih) + new Vector2(0f, -4f);
            unitPhoto = Print("UnitPhoto", img, iw, ih, side, bottom, 240f, (px, w, h) =>
            {
                // Scratched out: the emulsion gouged off in strokes, the paper showing through.
                var rng = new System.Random(2019);
                var paper = new Color(0.86f, 0.83f, 0.76f);
                Vector2 c = face + new Vector2(side, bottom);
                for (int k = 0; k < 70; k++)
                {
                    Vector2 a = c + new Vector2((float)(rng.NextDouble() - 0.5) * 44f, (float)(rng.NextDouble() - 0.5) * 60f);
                    float ang = (float)rng.NextDouble() * Mathf.PI, len = 10f + (float)rng.NextDouble() * 26f;
                    Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    for (float t = -len / 2f; t < len / 2f; t += 0.5f)
                    {
                        Vector2 p = a + dir * t;
                        Vector2 e = new Vector2((p.x - c.x) / 24f, (p.y - c.y) / 32f);
                        if (e.sqrMagnitude > 1f) continue;
                        int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y);
                        for (int dy = 0; dy <= 1; dy++)
                        for (int dx = 0; dx <= 1; dx++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                            px[yy * w + xx] = Color.Lerp(px[yy * w + xx], paper * (0.85f + 0.15f * (float)rng.NextDouble()), 0.85f);
                        }
                    }
                }
                // The caption, typed on the border under the picture.
                var mask = PixelFont.Mask("1ST PLT  CHARLIE CO  2019", false, 2, 7, out int tw, out int th);
                int ox = side + 4, oy = (bottom - th) / 2;
                for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    int xx = ox + x, yy = oy + y;
                    if (xx >= w || yy < 0 || yy >= h) continue;
                    px[yy * w + xx] = Color.Lerp(px[yy * w + xx], new Color(0.12f, 0.1f, 0.09f), mask[y * tw + x] * 0.85f);
                }
            });
            return unitPhoto;
        }

        static Texture2D miaNote;

        /// <summary>The note on Mia's bed (8b): a page torn from her school notebook, ruled lines and a red margin, and in red marker "SCHOOL. COME ALONE. :)".</summary>
        public static Texture2D MiaNote()
        {
            if (miaNote != null) return miaNote;
            const int w = 256, h = 200;
            var mask = PixelFont.Mask("SCHOOL.\nCOME ALONE.\n :)", true, 3, 77, out int tw, out int th);
            int ox = 40, oy = (h - th) / 2;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = new Color(0.9f, 0.88f, 0.82f) * (0.95f + 0.05f * MarinePortrait.Hash(x, y, 41));
                if ((h - y) % 18 == 0 && y < h - 30) c = Color.Lerp(c, new Color(0.45f, 0.6f, 0.8f), 0.6f);  // ruled lines
                if (x == 28 || x == 29) c = Color.Lerp(c, new Color(0.85f, 0.3f, 0.3f), 0.6f);              // margin
                if (y > h - 6 && MarinePortrait.Hash(x / 3, 0, 43) > 0.5f) c *= 0.8f;                         // torn top edge
                int mx = x - ox, my = y - oy;
                if (mx >= 0 && my >= 0 && mx < tw && my < th) c = Color.Lerp(c, new Color(0.62f, 0.04f, 0.04f), mask[my * tw + mx]);
                c.a = 1f;
                px[y * w + x] = c;
            }
            var tex = NewTex(w, h, TextureFormat.RGBA32, TextureWrapMode.Clamp, "MiaNote", true);
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return miaNote = tex;
        }

        static Texture2D phoneScreen;

        /// <summary>
        /// The screen of the phone in Mia's closet (8b): a voice memo playing on repeat. Title, file name, a waveform,
        /// the elapsed time and a red REPEAT badge. Used as the screen's emission and held up to the lens.
        /// </summary>
        public static Texture2D PhoneScreen()
        {
            if (phoneScreen != null) return phoneScreen;
            const int w = 180, h = 340;
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0.02f, 0.025f, 0.035f);
            void Fill(int x0, int y0, int x1, int y1, Color c)
            {
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++) px[y * w + x] = c;
            }
            void Text(string s, int cx, int top, int cell, Color c)
            {
                var m = PixelFont.Mask(s, false, cell, 3, out int tw, out int th);
                int ox = cx - tw / 2, oy = top - th;
                for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    int xx = ox + x, yy = oy + y;
                    if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                    px[yy * w + xx] = Color.Lerp(px[yy * w + xx], c, m[y * tw + x]);
                }
            }
            var white = new Color(0.92f, 0.94f, 0.98f);
            var dim = new Color(0.5f, 0.55f, 0.62f);
            Fill(0, h - 22, w, h, new Color(0.06f, 0.07f, 0.09f));
            Text("21:14", w / 2, h - 4, 2, dim);
            Text("VOICE MEMO", w / 2, h - 40, 2, white);
            Text("MIA - DOOR", w / 2, h - 70, 2, new Color(1f, 0.68f, 0.82f));
            // The waveform: bars of a recorded voice, the played part brighter.
            var rng = new System.Random(14);
            int mid = h / 2 + 10;
            for (int x = 14; x < w - 14; x += 4)
            {
                float a = (float)(0.25 + 0.75 * rng.NextDouble()) * (0.5f + 0.5f * Mathf.Sin(x * 0.11f));
                int hh = Mathf.Max(2, Mathf.RoundToInt(a * 46f));
                Fill(x, mid - hh, x + 3, mid + hh, x < w * 0.62f ? new Color(0.45f, 0.75f, 1f) : new Color(0.2f, 0.28f, 0.38f));
            }
            Fill((int)(w * 0.62f), mid - 56, (int)(w * 0.62f) + 2, mid + 56, white);        // the play head
            Text("00:09 / 00:14", w / 2, mid - 66, 2, dim);
            Fill(w / 2 - 50, 52, w / 2 + 50, 84, new Color(0.7f, 0.08f, 0.08f));         // REPEAT badge
            Text("REPEAT ON", w / 2, 78, 2, white);
            var tex = NewTex(w, h, TextureFormat.RGBA32, TextureWrapMode.Clamp, "PhoneScreen", true);
            var c32 = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++) c32[i] = px[i];
            tex.SetPixels32(c32);
            tex.Apply(true, true);
            return phoneScreen = tex;
        }

        /// <summary>A touch of lens softness (also smooths the ray-marched edges).</summary>
        static Color[] Soften(Color[] img, int iw, int ih)
        {
            var soft = new Color[img.Length];
            for (int y = 0; y < ih; y++)
            for (int x = 0; x < iw; x++)
            {
                Color s = img[y * iw + x] * 4f;
                s += img[y * iw + Mathf.Min(x + 1, iw - 1)] + img[y * iw + Mathf.Max(x - 1, 0)];
                s += img[Mathf.Min(y + 1, ih - 1) * iw + x] + img[Mathf.Max(y - 1, 0) * iw + x];
                soft[y * iw + x] = s / 8f;
            }
            return soft;
        }

        /// <summary>
        /// A linear image as an old gloss print: filmic tone and sRGB, faded warm colour with lifted blacks, grain, a
        /// vignette and a glare, a white border (`side` px, `bottom` px below), yellowing, a crease, worn corners and
        /// scratches. `mark` can draw on the finished print before it is aged (sRGB pixels, row 0 at the bottom).
        /// </summary>
        static Texture2D Print(string name, Color[] img, int iw, int ih, int side, int bottom, float crease, System.Action<Color[], int, int> mark)
        {
            int w = iw + 2 * side, h = ih + side + bottom;
            var px = new Color[w * h];
            var paper = new Color(0.92f, 0.9f, 0.84f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c;
                int ix = x - side, iy = y - bottom;
                if (ix < 0 || iy < 0 || ix >= iw || iy >= ih) c = paper * (0.94f + 0.06f * MarinePortrait.Hash(x, y, 3));
                else
                {
                    float u = ix / (float)iw, v = iy / (float)ih;
                    Color l = img[iy * iw + ix];
                    // Filmic shoulder, then sRGB, then the print's fade: lifted blacks, warm, a little less saturated.
                    l = new Color(l.r / (1f + l.r), l.g / (1f + l.g), l.b / (1f + l.b)) * 1.35f;
                    c = new Color(Mathf.Pow(Mathf.Clamp01(l.r), 1f / 2.2f), Mathf.Pow(Mathf.Clamp01(l.g), 1f / 2.2f), Mathf.Pow(Mathf.Clamp01(l.b), 1f / 2.2f));
                    float grey = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    c = Color.Lerp(new Color(grey, grey, grey), c, 0.82f);
                    c = new Color(0.07f, 0.055f, 0.04f) + c * 0.9f;
                    c = new Color(c.r * 1.04f, c.g * 0.99f, c.b * 0.88f);
                    // Grain, a vignette, and the gloss catching the room light across the top.
                    c *= 0.96f + 0.08f * MarinePortrait.Hash(x, y, 11);
                    c *= 1f - 0.4f * Mathf.Pow(Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.55f)) * 1.35f), 2.2f);
                    float glare = Mathf.Exp(-Mathf.Pow((u + v * 0.6f - 1.25f) / 0.12f, 2f)) * 0.07f;
                    c += new Color(glare, glare, glare);
                }
                px[y * w + x] = c;
            }
            if (mark != null) mark(px, w, h);

            var outPx = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Age over everything: yellowing, a crease across one corner, worn corners, a few scratches.
                Color c = Color.Lerp(px[y * w + x], new Color(0.78f, 0.66f, 0.45f), 0.12f);
                float cr = (x - y * 0.7f) - crease;
                if (Mathf.Abs(cr) < 1.4f) c *= 1.16f;
                else if (cr > 1.4f && cr < 3.5f) c *= 0.93f;
                int cx = Mathf.Min(x, w - 1 - x), cy = Mathf.Min(y, h - 1 - y);
                if (cx + cy < 12 && MarinePortrait.Hash(x, y, 21) > 0.35f) c = Color.Lerp(c, paper, 0.7f);
                if (MarinePortrait.Hash(x / 2, (int)(y * 0.08f + x * 0.5f), 31) > 0.9985f) c *= 1.2f;
                c.a = 1f;
                outPx[y * w + x] = c;
            }
            var tex = NewTex(w, h, TextureFormat.RGBA32, TextureWrapMode.Clamp, name, true);
            tex.SetPixels32(outPx);
            tex.Apply(true, true);
            return tex;
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
