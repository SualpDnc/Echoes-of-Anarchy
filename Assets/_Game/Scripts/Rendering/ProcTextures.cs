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
