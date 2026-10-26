using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Echoes.World
{
    /// <summary>One synthesized PBR material: albedo, height (→ normal), mask (R metal, G occlusion, A smoothness), optional emission.</summary>
    public class PBRSet
    {
        public readonly int size;
        public readonly Color[] albedo;
        public readonly float[] height;
        public readonly Color[] mask;
        public Color[] emission;
        public float normalStrength = 4f;

        public PBRSet(int size)
        {
            this.size = size;
            albedo = new Color[size * size];
            height = new float[size * size];
            mask = new Color[size * size];
        }

        public Color[] BuildNormalMap()
        {
            var n = new Color[size * size];
            int s = size;
            Parallel.For(0, s, y =>
            {
                int yu = (y + 1) % s, yd = (y - 1 + s) % s;
                for (int x = 0; x < s; x++)
                {
                    int xr = (x + 1) % s, xl = (x - 1 + s) % s;
                    float dx = (height[y * s + xr] - height[y * s + xl]) * normalStrength;
                    float dy = (height[yu * s + x] - height[yd * s + x]) * normalStrength;
                    var v = new Vector3(-dx, -dy, 1f).normalized;
                    n[y * s + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                }
            });
            return n;
        }
    }

    /// <summary>
    /// CPU texture synthesis recipes. Every recipe is deterministic and tiles seamlessly.
    /// Physical tile sizes are documented per recipe and must match the material tiling in the library.
    /// </summary>
    public static class TextureSynth
    {
        delegate void Pixel(float u, float v, int x, int y, int i);

        static void ForEach(int size, Pixel fn)
        {
            Parallel.For(0, size, y =>
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                    fn((x + 0.5f) / size, v, x, y, y * size + x);
            });
        }

        static Color Gray(float g) => new Color(g, g, g, 1f);
        static Color Mask(float metal, float ao, float smooth) => new Color(metal, ao, 0f, Mathf.Clamp01(smooth));
        static float Smooth01(float a, float b, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, t));

        // ------------------------------------------------------------ ASPHALT (tile = 4 m)
        public static PBRSet Asphalt(int size, int seed)
        {
            var p = new PBRSet(size) { normalStrength = 6f };
            ForEach(size, (u, v, x, y, i) =>
            {
                float agg = TileNoise.Fbm(u, v, 64, 3, seed, 0.6f);
                TileNoise.Worley(u, v, 96, seed + 1, out float s1, out _, out float stoneId);
                float stone = 1f - Smooth01(0.15f, 0.42f, s1);
                float patches = TileNoise.Fbm(u, v, 3, 4, seed + 2);
                float wear = TileNoise.Fbm(u, v, 6, 3, seed + 3);

                // Cracks: thin cellular edges, warped and masked so they appear in clusters.
                float wu = u + (TileNoise.Fbm(u, v, 8, 2, seed + 4) - 0.5f) * 0.03f;
                float wv = v + (TileNoise.Fbm(u, v, 8, 2, seed + 5) - 0.5f) * 0.03f;
                TileNoise.Worley(wu, wv, 7, seed + 6, out float c1, out float c2, out _);
                float crackMask = Smooth01(0.55f, 0.7f, TileNoise.Fbm(u, v, 4, 2, seed + 7));
                float crack = (1f - Smooth01(0.0f, 0.035f, c2 - c1)) * crackMask;

                // Puddles collect in low, smooth areas.
                float puddleN = TileNoise.Fbm(u, v, 2, 4, seed + 8);
                float puddle = Smooth01(0.6f, 0.66f, puddleN);

                float g = 0.065f + agg * 0.05f + stone * (0.04f + stoneId * 0.06f);
                g *= Mathf.Lerp(0.8f, 1.15f, patches);
                g = Mathf.Lerp(g, g * 1.25f + 0.02f, Smooth01(0.55f, 0.8f, wear) * 0.5f); // sun-faded/worn lanes
                g *= 1f - crack * 0.6f;
                g *= Mathf.Lerp(1f, 0.6f, puddle); // wet darkening

                float h = agg * 0.35f + stone * 0.4f - crack * 0.8f;
                h = Mathf.Lerp(h, 0.2f, puddle); // water surface is flat

                p.albedo[i] = new Color(g, g * 0.99f, g * 1.02f, 1f);
                p.height[i] = h;
                float smooth = Mathf.Lerp(0.32f + stone * 0.15f, 0.97f, puddle);
                p.mask[i] = Mask(0f, 1f - crack * 0.5f, smooth);
            });
            return p;
        }

        // ------------------------------------------------------------ BRICK (tile = 2 m × 2 m, 9 bricks × 28 courses)
        public static PBRSet Brick(int size, int seed, Color baseColor, Color mortarColor)
        {
            var p = new PBRSet(size) { normalStrength = 5f };
            const int cols = 9, rows = 28;
            const float mortarU = 0.045f, mortarV = 0.14f; // fraction of a brick cell
            ForEach(size, (u, v, x, y, i) =>
            {
                float rv = v * rows;
                int row = Mathf.FloorToInt(rv);
                float fy = rv - row;
                float ru = u * cols + ((row & 1) == 1 ? 0.5f : 0f);
                int col = Mathf.FloorToInt(ru);
                float fx = ru - col;
                int colW = ((col % cols) + cols) % cols;

                float edge = Mathf.Min(Mathf.Min(fx, 1f - fx) / mortarU, Mathf.Min(fy, 1f - fy) / mortarV);
                float chip = TileNoise.Fbm(u, v, 48, 3, seed + 1);
                float brickMask = Smooth01(0.6f + (chip - 0.5f) * 1.2f, 1.2f, edge);

                float rnd = TileNoise.CellRandom(colW, row, seed);
                float rnd2 = TileNoise.CellRandom(colW, row, seed + 5);
                float fine = TileNoise.Fbm(u, v, 128, 2, seed + 2);
                float grime = TileNoise.Fbm(u * 1f, v, 6, 4, seed + 3);
                float streak = TileNoise.Fbm(u, v * 0.0f + 0.37f, 40, 3, seed + 4); // vertical rain streaks
                float soot = Smooth01(0.45f, 0.85f, grime) * 0.45f + Smooth01(0.55f, 0.8f, streak) * 0.2f;

                Color brick = baseColor * Mathf.Lerp(0.72f, 1.18f, rnd);
                brick = Color.Lerp(brick, brick * new Color(1.1f, 0.9f, 0.8f), rnd2 * 0.5f); // fired-clay hue shift
                brick *= Mathf.Lerp(0.85f, 1.1f, fine);
                Color mortar = mortarColor * Mathf.Lerp(0.8f, 1.1f, fine);
                Color c = Color.Lerp(mortar, brick, brickMask);
                c *= 1f - soot;
                c.a = 1f;

                p.albedo[i] = c;
                p.height[i] = brickMask * (0.75f + fine * 0.25f) - (1f - chip) * 0.08f * brickMask;
                p.mask[i] = Mask(0f, Mathf.Lerp(0.55f, 1f, brickMask), Mathf.Lerp(0.08f, 0.22f, brickMask * rnd2));
            });
            return p;
        }

        // ------------------------------------------------------------ SIDEWALK SLABS (tile = 3 m, 2×2 slabs)
        public static PBRSet Sidewalk(int size, int seed)
        {
            var p = new PBRSet(size) { normalStrength = 4f };
            const int slabs = 2;
            ForEach(size, (u, v, x, y, i) =>
            {
                float su = u * slabs, sv = v * slabs;
                int sx = Mathf.FloorToInt(su), sy = Mathf.FloorToInt(sv);
                float fx = su - sx, fy = sv - sy;
                float joint = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
                float jointMask = Smooth01(0.004f, 0.012f, joint);

                float tint = TileNoise.CellRandom(sx % slabs, sy % slabs, seed);
                float fine = TileNoise.Fbm(u, v, 96, 3, seed + 1);
                float stains = TileNoise.Fbm(u, v, 5, 5, seed + 2);
                TileNoise.Worley(u, v, 40, seed + 3, out float g1, out _, out float gumId);
                float gum = (1f - Smooth01(0.06f, 0.1f, g1)) * (gumId > 0.85f ? 1f : 0f);
                float wet = Smooth01(0.62f, 0.7f, TileNoise.Fbm(u, v, 3, 4, seed + 4));

                float g = Mathf.Lerp(0.42f, 0.52f, tint) * Mathf.Lerp(0.88f, 1.06f, fine);
                g *= Mathf.Lerp(0.7f, 1f, Smooth01(0.3f, 0.6f, stains));
                g *= Mathf.Lerp(1f, 0.35f, gum);
                g *= Mathf.Lerp(1f, 0.6f, wet);
                g *= Mathf.Lerp(0.45f, 1f, jointMask);
                p.albedo[i] = new Color(g, g * 0.98f, g * 0.95f, 1f);
                p.height[i] = jointMask * 0.6f + fine * 0.15f + gum * 0.1f;
                p.mask[i] = Mask(0f, Mathf.Lerp(0.5f, 1f, jointMask), Mathf.Lerp(0.18f + fine * 0.1f, 0.75f, wet));
            });
            return p;
        }

        // ------------------------------------------------------------ STUCCO / PLASTER (tile = 3 m)
        public static PBRSet Stucco(int size, int seed, Color paint)
        {
            var p = new PBRSet(size) { normalStrength = 3f };
            ForEach(size, (u, v, x, y, i) =>
            {
                float bump = TileNoise.Fbm(u, v, 32, 4, seed);
                float big = TileNoise.Fbm(u, v, 4, 4, seed + 1);
                float streak = TileNoise.Fbm(u, 0.5f, 24, 3, seed + 2);
                float peel = Smooth01(0.66f, 0.7f, TileNoise.Fbm(u, v, 6, 5, seed + 3));
                Color c = paint * Mathf.Lerp(0.82f, 1.08f, big) * Mathf.Lerp(0.92f, 1.04f, bump);
                c *= 1f - Smooth01(0.5f, 0.8f, streak) * 0.25f;
                c = Color.Lerp(c, new Color(0.3f, 0.29f, 0.27f), peel); // bare render where paint peeled
                c.a = 1f;
                p.albedo[i] = c;
                p.height[i] = bump * 0.5f - peel * 0.15f;
                p.mask[i] = Mask(0f, 1f, 0.12f + bump * 0.06f);
            });
            return p;
        }

        // ------------------------------------------------------------ RAW CONCRETE (tile = 3 m)
        public static PBRSet Concrete(int size, int seed, float lightness)
        {
            var p = new PBRSet(size) { normalStrength = 3.5f };
            ForEach(size, (u, v, x, y, i) =>
            {
                float n = TileNoise.Fbm(u, v, 16, 5, seed);
                TileNoise.Worley(u, v, 120, seed + 1, out float f1, out _, out float id);
                float pore = (1f - Smooth01(0.05f, 0.2f, f1)) * (id > 0.6f ? 1f : 0f);
                float stain = TileNoise.Fbm(u, v, 3, 4, seed + 2);
                float g = lightness * Mathf.Lerp(0.8f, 1.1f, n) * Mathf.Lerp(0.75f, 1f, Smooth01(0.3f, 0.7f, stain));
                g *= 1f - pore * 0.35f;
                p.albedo[i] = new Color(g, g, g * 1.02f, 1f);
                p.height[i] = n * 0.4f - pore * 0.3f;
                p.mask[i] = Mask(0f, 1f - pore * 0.4f, 0.15f + n * 0.1f);
            });
            return p;
        }

        // ------------------------------------------------------------ WOOD PLANKS / PLYWOOD BOARDING (tile = 2 m, 8 planks)
        public static PBRSet Planks(int size, int seed, Color wood)
        {
            var p = new PBRSet(size) { normalStrength = 4f };
            const int planks = 8;
            ForEach(size, (u, v, x, y, i) =>
            {
                float pv = v * planks;
                int k = Mathf.FloorToInt(pv);
                float fy = pv - k;
                float gap = Smooth01(0.0f, 0.04f, Mathf.Min(fy, 1f - fy));
                float tint = TileNoise.CellRandom(k % planks, 0, seed);
                float grain = TileNoise.Fbm(u * 1f, v, 4, 3, seed + k) * 0.6f
                            + Mathf.Sin((v * planks * 18f + TileNoise.Fbm(u, v, 6, 3, seed + 1) * 12f) * Mathf.PI) * 0.2f;
                TileNoise.Worley(u, v, 10, seed + 2, out float kn, out _, out float knId);
                float knot = (1f - Smooth01(0.05f, 0.14f, kn)) * (knId > 0.7f ? 1f : 0f);
                float weather = TileNoise.Fbm(u, v, 5, 4, seed + 3);
                Color c = wood * Mathf.Lerp(0.7f, 1.15f, tint) * Mathf.Lerp(0.85f, 1.1f, grain);
                c = Color.Lerp(c, new Color(0.25f, 0.24f, 0.22f), Smooth01(0.55f, 0.85f, weather) * 0.5f); // grey weathering
                c *= 1f - knot * 0.45f;
                c *= Mathf.Lerp(0.15f, 1f, gap);
                c.a = 1f;
                p.albedo[i] = c;
                p.height[i] = gap * (0.7f + grain * 0.15f) - knot * 0.1f;
                p.mask[i] = Mask(0f, Mathf.Lerp(0.4f, 1f, gap), 0.12f + grain * 0.08f);
            });
            return p;
        }

        // ------------------------------------------------------------ CORRUGATED ROLLER SHUTTER (tile = 2 m)
        public static PBRSet Shutter(int size, int seed, Color paint)
        {
            var p = new PBRSet(size) { normalStrength = 7f };
            const int ribs = 28;
            ForEach(size, (u, v, x, y, i) =>
            {
                float rib = Mathf.Sin(v * ribs * Mathf.PI * 2f) * 0.5f + 0.5f;
                float rust = Smooth01(0.58f, 0.72f, TileNoise.Fbm(u, v, 6, 5, seed));
                float scratch = TileNoise.Fbm(u, v, 64, 2, seed + 1);
                float dirt = TileNoise.Fbm(u, v, 3, 3, seed + 2);
                Color c = paint * Mathf.Lerp(0.75f, 1.05f, rib) * Mathf.Lerp(0.7f, 1f, dirt);
                c = Color.Lerp(c, new Color(0.28f, 0.13f, 0.06f) * Mathf.Lerp(0.6f, 1.2f, scratch), rust);
                c.a = 1f;
                p.albedo[i] = c;
                p.height[i] = rib * 0.8f - rust * 0.05f;
                p.mask[i] = Mask(Mathf.Lerp(0.6f, 0.1f, rust), Mathf.Lerp(0.6f, 1f, rib), Mathf.Lerp(0.45f, 0.15f, rust));
            });
            return p;
        }

        // ------------------------------------------------------------ PAINTED METAL, RUSTED (tile = 2 m)
        public static PBRSet PaintedMetal(int size, int seed, Color paint, float rustAmount)
        {
            var p = new PBRSet(size) { normalStrength = 3f };
            ForEach(size, (u, v, x, y, i) =>
            {
                float rustN = TileNoise.Fbm(u, v, 5, 6, seed);
                float rust = Smooth01(0.72f - rustAmount * 0.3f, 0.78f - rustAmount * 0.3f, rustN);
                float scratch = TileNoise.Fbm(u * 1f, v, 80, 2, seed + 1);
                float dent = TileNoise.Fbm(u, v, 8, 3, seed + 2);
                Color c = paint * Mathf.Lerp(0.8f, 1.05f, dent);
                c = Color.Lerp(c, new Color(0.32f, 0.15f, 0.06f) * Mathf.Lerp(0.5f, 1.1f, scratch), rust);
                c.a = 1f;
                p.albedo[i] = c;
                p.height[i] = dent * 0.3f + rust * scratch * 0.2f;
                p.mask[i] = Mask(Mathf.Lerp(0.5f, 0f, rust), 1f, Mathf.Lerp(0.5f, 0.12f, rust));
            });
            return p;
        }

        // ------------------------------------------------------------ TAR ROOF (tile = 4 m)
        public static PBRSet Roof(int size, int seed)
        {
            var p = new PBRSet(size) { normalStrength = 3f };
            ForEach(size, (u, v, x, y, i) =>
            {
                TileNoise.Worley(u, v, 160, seed, out float f1, out _, out float id);
                float gravel = 1f - Smooth01(0.1f, 0.5f, f1);
                float patch = TileNoise.Fbm(u, v, 4, 4, seed + 1);
                float g = (0.09f + gravel * id * 0.08f) * Mathf.Lerp(0.8f, 1.2f, patch);
                p.albedo[i] = new Color(g, g, g, 1f);
                p.height[i] = gravel * 0.5f;
                p.mask[i] = Mask(0f, 1f, 0.2f);
            });
            return p;
        }

        // ------------------------------------------------------------ DISTANT WINDOW GRID (emissive, tile = 16 m × 16 m, 4 m × 3.2 m bays)
        public static PBRSet DistantWindows(int size, int seed)
        {
            var p = new PBRSet(size) { normalStrength = 0f, emission = new Color[size * size] };
            const int cols = 6, rows = 5;
            ForEach(size, (u, v, x, y, i) =>
            {
                float cu = u * cols, cv = v * rows;
                int cx = Mathf.FloorToInt(cu), cy = Mathf.FloorToInt(cv);
                float fx = cu - cx, fy = cv - cy;
                bool inWin = fx > 0.18f && fx < 0.82f && fy > 0.25f && fy < 0.8f;
                float r = TileNoise.CellRandom(cx, cy, seed);
                float hue = TileNoise.CellRandom(cx, cy, seed + 3);
                Color lit = hue < 0.6f ? new Color(1f, 0.68f, 0.38f) : hue < 0.85f ? new Color(0.75f, 0.85f, 1f) : new Color(0.4f, 0.5f, 1f);
                float on = inWin && r < 0.2f ? Mathf.Lerp(0.4f, 1f, TileNoise.CellRandom(cx, cy, seed + 7)) : 0f;
                float wall = 0.05f + TileNoise.Fbm(u, v, 8, 2, seed + 1) * 0.03f;
                p.albedo[i] = inWin ? Gray(0.02f) : Gray(wall);
                p.emission[i] = lit * on;
                p.height[i] = 0f;
                p.mask[i] = Mask(0f, 1f, inWin ? 0.9f : 0.1f);
            });
            return p;
        }

        // ------------------------------------------------------------ PARTICLE SPRITES (alpha in A, not tiling)
        public static Color[] SmokePuff(int size, int seed)
        {
            var px = new Color[size * size];
            ForEach(size, (u, v, x, y, i) =>
            {
                float dx = u - 0.5f, dy = v - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float n = TileNoise.Fbm(u, v, 4, 5, seed);
                float a = Mathf.Clamp01((1f - r) * 1.6f - (1f - n) * 0.9f);
                a = a * a;
                float shade = Mathf.Lerp(0.6f, 1f, n);
                px[i] = new Color(shade, shade, shade, a);
            });
            return px;
        }

        public static Color[] Flame(int size, int seed)
        {
            var px = new Color[size * size];
            ForEach(size, (u, v, x, y, i) =>
            {
                float dx = (u - 0.5f) * 2f;
                float n = TileNoise.Fbm(u, v, 4, 4, seed);
                float width = Mathf.Lerp(0.85f, 0.15f, v) + (n - 0.5f) * 0.5f;
                float core = Mathf.Clamp01(1f - Mathf.Abs(dx) / Mathf.Max(0.05f, width));
                float a = core * Mathf.Clamp01(1f - v * 1.1f + (n - 0.5f) * 0.6f) * Smooth01(0f, 0.12f, v);
                px[i] = new Color(1f, 1f, 1f, Mathf.Clamp01(a * 1.6f));
            });
            return px;
        }

        public static Color[] SoftDot(int size)
        {
            var px = new Color[size * size];
            ForEach(size, (u, v, x, y, i) =>
            {
                float dx = u - 0.5f, dy = v - 0.5f;
                float a = Mathf.Exp(-(dx * dx + dy * dy) * 40f);
                px[i] = new Color(1f, 1f, 1f, a);
            });
            return px;
        }
    }
}
