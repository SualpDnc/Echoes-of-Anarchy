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
