using System.Collections.Generic;
using UnityEngine;
using Echoes.World;

namespace Echoes.Rendering
{
    public enum Fabric { Knit, Denim, Wool, Cotton, Leather, Rubber, Skin, Porcelain, Hair }

    /// <summary>
    /// Small tileable detail maps for character materials, synthesized at runtime (characters are built at runtime,
    /// so their textures are too). Albedo is a near-white multiplier for _BaseColor, stored linear so the tint
    /// stays as authored. Normals are packed RGB(A=1), which URP's RG/AG unpack reads correctly on PC.
    /// </summary>
    public static class ClothTextures
    {
        const int Size = 128;

        struct Set { public Texture2D albedo, normal; }
        static readonly Dictionary<Fabric, Set> cache = new Dictionary<Fabric, Set>();

        /// <summary>Physical size of one texture tile in metres (body UVs are in metres).</summary>
        public static float TileSize(Fabric f)
        {
            switch (f)
            {
                case Fabric.Knit: return 0.05f;
                case Fabric.Denim: return 0.04f;
                case Fabric.Wool: return 0.035f;
                case Fabric.Cotton: return 0.03f;
                case Fabric.Leather: return 0.08f;
                case Fabric.Skin: return 0.06f;
                case Fabric.Hair: return 0.06f;
                default: return 0.12f;
            }
        }

        public static float NormalScale(Fabric f) => f == Fabric.Porcelain || f == Fabric.Rubber ? 0.5f : f == Fabric.Skin ? 0.35f : 0.9f;

        public static Texture2D Albedo(Fabric f) => Get(f).albedo;
        public static Texture2D Normal(Fabric f) => Get(f).normal;

        static Set Get(Fabric f)
        {
            if (cache.TryGetValue(f, out var s) && s.albedo != null) return s;
            var height = new float[Size * Size];
            var tone = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size, v = (y + 0.5f) / Size;
                Sample(f, u, v, out float h, out float a);
                height[y * Size + x] = h;
                tone[y * Size + x] = a;
            }

            var albedo = NewTex(f + "_A");
            var normal = NewTex(f + "_N");
            var ap = new Color32[Size * Size];
            var np = new Color32[Size * Size];
            float strength = Strength(f);
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int i = y * Size + x;
                byte g = (byte)(Mathf.Clamp01(tone[i]) * 255f);
                ap[i] = new Color32(g, g, g, 255);
                float dx = height[y * Size + (x + 1) % Size] - height[y * Size + (x + Size - 1) % Size];
                float dy = height[((y + 1) % Size) * Size + x] - height[((y + Size - 1) % Size) * Size + x];
                var n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                np[i] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
            }
            albedo.SetPixels32(ap);
            albedo.Apply(true, true);
            normal.SetPixels32(np);
            normal.Apply(true, true);
            s = new Set { albedo = albedo, normal = normal };
            cache[f] = s;
            return s;
        }

        static float Strength(Fabric f)
        {
            switch (f)
            {
                case Fabric.Knit: return 9f;
                case Fabric.Denim: return 7f;
                case Fabric.Leather: return 5f;
                case Fabric.Hair: return 8f;
                case Fabric.Porcelain: return 3f;
                default: return 4f;
            }
        }

        /// <summary>Height (0..1) and albedo multiplier for one texel of a fabric.</summary>
        static void Sample(Fabric f, float u, float v, out float h, out float a)
        {
            float fuzz = TileNoise.Fbm(u, v, 16, 3, 7);
            switch (f)
            {
                case Fabric.Knit:
                {
                    // Jersey: columns of V-shaped stitches.
                    float cu = u * 14f, cv = v * 20f;
                    float col = cu - Mathf.Floor(cu);
                    float row = cv - Mathf.Floor(cv) + Mathf.Abs(col - 0.5f) * 0.8f;
                    float stitch = Mathf.Sin(col * Mathf.PI) * (0.6f + 0.4f * Mathf.Sin((row - Mathf.Floor(row)) * Mathf.PI));
                    h = stitch * 0.8f + fuzz * 0.2f;
                    a = 0.86f + stitch * 0.1f + (fuzz - 0.5f) * 0.08f;
                    break;
                }
                case Fabric.Denim:
                {
                    // 3/1 twill: diagonal ribs, white weft showing through, slubby warp streaks.
                    float twill = Mathf.Sin((u * 34f + v * 34f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    float slub = Streaks(u, 48, 3);
                    h = twill * 0.7f + fuzz * 0.3f;
                    a = 0.78f + twill * 0.14f + (slub - 0.5f) * 0.18f;
                    break;
                }
                case Fabric.Wool:
                {
                    float herring = Mathf.Abs(Mathf.Sin((u * 24f + Mathf.Abs((v * 12f) % 1f - 0.5f) * 1.5f) * Mathf.PI));
                    h = herring * 0.3f + fuzz * 0.7f;
                    a = 0.9f + (fuzz - 0.5f) * 0.1f + herring * 0.04f;
                    break;
                }
                case Fabric.Cotton:
                {
                    float wx = Mathf.Sin(u * 40f * Mathf.PI * 2f), wy = Mathf.Sin(v * 40f * Mathf.PI * 2f);
                    float weave = (wx * wy) * 0.5f + 0.5f;
                    h = weave * 0.5f + fuzz * 0.5f;
                    a = 0.9f + (weave - 0.5f) * 0.06f + (fuzz - 0.5f) * 0.08f;
                    break;
                }
                case Fabric.Leather:
                {
                    TileNoise.Worley(u, v, 22, 11, out float f1, out float f2, out _);
                    float pebble = Mathf.Clamp01((f2 - f1) * 2.5f);
                    h = pebble * 0.7f + fuzz * 0.3f;
                    a = 0.88f + pebble * 0.1f;
                    break;
                }
                case Fabric.Skin:
                {
                    TileNoise.Worley(u, v, 40, 5, out float f1, out _, out _);
                    float pores = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(f1 * 3f));
                    float mottle = TileNoise.Fbm(u, v, 4, 3, 9);
                    h = pores * 0.6f + fuzz * 0.4f;
                    a = 0.9f + (mottle - 0.5f) * 0.14f;
                    break;
                }
                case Fabric.Hair:
                {
                    // Strands run along V.
                    float strand = Streaks(u, 64, 21) * 0.7f + fuzz * 0.3f;
                    h = strand;
                    a = 0.75f + strand * 0.3f;
                    break;
                }
                case Fabric.Porcelain:
                {
                    TileNoise.Worley(u, v, 6, 13, out float f1, out float f2, out _);
                    float crack = 1f - Mathf.Clamp01((f2 - f1) * 30f);
                    h = 1f - crack * 0.6f;
                    a = 0.96f - crack * 0.12f + (fuzz - 0.5f) * 0.04f;
                    break;
                }
                default: // Rubber
                {
                    float lumps = TileNoise.Fbm(u, v, 3, 4, 17);
                    h = lumps * 0.8f + fuzz * 0.2f;
                    a = 0.9f + (lumps - 0.5f) * 0.12f;
                    break;
                }
            }
            a = Mathf.Clamp(a, 0.55f, 1f);
        }

        /// <summary>Vertical streaks: a random value per column, blended across neighbours. Tiles because columns is an integer.</summary>
        static float Streaks(float u, int columns, int seed)
        {
            float c = u * columns;
            int c0 = Mathf.FloorToInt(c);
            float t = Mathf.SmoothStep(0f, 1f, c - c0);
            return Mathf.Lerp(TileNoise.CellRandom(c0 % columns, 0, seed), TileNoise.CellRandom((c0 + 1) % columns, 0, seed), t);
        }

        static Texture2D NewTex(string name) => new Texture2D(Size, Size, TextureFormat.RGBA32, true, true)
        {
            name = "Cloth_" + name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4,
            hideFlags = HideFlags.DontSave
        };
    }
}
