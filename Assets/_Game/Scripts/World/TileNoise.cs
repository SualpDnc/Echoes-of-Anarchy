using UnityEngine;

namespace Echoes.World
{
    /// <summary>
    /// Periodic (seamlessly tiling) noise functions for texture synthesis.
    /// All inputs are in [0,1) texture space; `period` is the lattice cell count across the texture.
    /// </summary>
    public static class TileNoise
    {
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        static int Wrap(int v, int p) => ((v % p) + p) % p;

        static Vector2 Grad(int x, int y, int seed)
        {
            float a = Hash(x, y, seed) * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        /// <summary>Tileable gradient noise, roughly [-1,1].</summary>
        public static float Perlin(float u, float v, int period, int seed)
        {
            float x = u * period, y = v * period;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            int xa = Wrap(x0, period), xb = Wrap(x0 + 1, period);
            int ya = Wrap(y0, period), yb = Wrap(y0 + 1, period);
            float n00 = Vector2.Dot(Grad(xa, ya, seed), new Vector2(fx, fy));
            float n10 = Vector2.Dot(Grad(xb, ya, seed), new Vector2(fx - 1f, fy));
            float n01 = Vector2.Dot(Grad(xa, yb, seed), new Vector2(fx, fy - 1f));
            float n11 = Vector2.Dot(Grad(xb, yb, seed), new Vector2(fx - 1f, fy - 1f));
            float sx = Fade(fx), sy = Fade(fy);
            return Mathf.Lerp(Mathf.Lerp(n00, n10, sx), Mathf.Lerp(n01, n11, sx), sy) * 1.414f;
        }

        /// <summary>Fractal sum of Perlin octaves, normalized to roughly [0,1].</summary>
        public static float Fbm(float u, float v, int period, int octaves, int seed, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            int p = period;
            for (int i = 0; i < octaves; i++)
            {
                sum += Perlin(u, v, p, seed + i * 31) * amp;
                norm += amp;
                amp *= gain;
                p *= 2;
            }
            return Mathf.Clamp01(sum / norm * 0.5f + 0.5f);
        }

        /// <summary>Tileable cellular noise. Returns F1, F2 distances (in cell units) and the nearest cell id hash.</summary>
        public static void Worley(float u, float v, int period, int seed, out float f1, out float f2, out float cellId)
        {
            float x = u * period, y = v * period;
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            f1 = f2 = 10f;
            cellId = 0f;
            for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                int gx = cx + i, gy = cy + j;
                int wx = Wrap(gx, period), wy = Wrap(gy, period);
                float px = gx + Hash(wx, wy, seed);
                float py = gy + Hash(wx, wy, seed + 17);
                float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                if (d < f1) { f2 = f1; f1 = d; cellId = Hash(wx, wy, seed + 91); }
                else if (d < f2) f2 = d;
            }
        }

        /// <summary>Stable per-integer-cell random value in [0,1].</summary>
        public static float CellRandom(int x, int y, int seed) => Hash(x, y, seed);
    }
}
