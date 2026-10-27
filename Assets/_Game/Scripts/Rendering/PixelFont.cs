using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Rendering
{
    /// <summary>
    /// Text painted into the world (spray paint, stencilled signs) without imported fonts: a 5×7 block font drawn into a
    /// runtime texture. `spray` gives soft, uneven strokes with overspray and drips; otherwise the letters are crisp stencil
    /// blocks. Alpha carries the shape (RGB is white), so the colour comes from the material tint, like the blood decals.
    /// </summary>
    public static class PixelFont
    {
        const int W = 5, H = 7;

        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { " ### ", "#   #", "#   #", "#####", "#   #", "#   #", "#   #" },
            ['B'] = new[] { "#### ", "#   #", "#   #", "#### ", "#   #", "#   #", "#### " },
            ['C'] = new[] { " ### ", "#   #", "#    ", "#    ", "#    ", "#   #", " ### " },
            ['D'] = new[] { "#### ", "#   #", "#   #", "#   #", "#   #", "#   #", "#### " },
            ['E'] = new[] { "#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#####" },
            ['F'] = new[] { "#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#    " },
            ['G'] = new[] { " ### ", "#   #", "#    ", "# ###", "#   #", "#   #", " ####" },
            ['H'] = new[] { "#   #", "#   #", "#   #", "#####", "#   #", "#   #", "#   #" },
            ['I'] = new[] { " ### ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### " },
            ['J'] = new[] { "  ###", "   # ", "   # ", "   # ", "   # ", "#  # ", " ##  " },
            ['K'] = new[] { "#   #", "#  # ", "# #  ", "##   ", "# #  ", "#  # ", "#   #" },
            ['L'] = new[] { "#    ", "#    ", "#    ", "#    ", "#    ", "#    ", "#####" },
            ['M'] = new[] { "#   #", "## ##", "# # #", "# # #", "#   #", "#   #", "#   #" },
            ['N'] = new[] { "#   #", "##  #", "# # #", "#  ##", "#   #", "#   #", "#   #" },
            ['O'] = new[] { " ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### " },
            ['P'] = new[] { "#### ", "#   #", "#   #", "#### ", "#    ", "#    ", "#    " },
            ['Q'] = new[] { " ### ", "#   #", "#   #", "#   #", "# # #", "#  # ", " ## #" },
            ['R'] = new[] { "#### ", "#   #", "#   #", "#### ", "# #  ", "#  # ", "#   #" },
            ['S'] = new[] { " ####", "#    ", "#    ", " ### ", "    #", "    #", "#### " },
            ['T'] = new[] { "#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  " },
            ['U'] = new[] { "#   #", "#   #", "#   #", "#   #", "#   #", "#   #", " ### " },
            ['V'] = new[] { "#   #", "#   #", "#   #", "#   #", "#   #", " # # ", "  #  " },
            ['W'] = new[] { "#   #", "#   #", "#   #", "# # #", "# # #", "# # #", " # # " },
            ['X'] = new[] { "#   #", "#   #", " # # ", "  #  ", " # # ", "#   #", "#   #" },
            ['Y'] = new[] { "#   #", "#   #", " # # ", "  #  ", "  #  ", "  #  ", "  #  " },
            ['Z'] = new[] { "#####", "    #", "   # ", "  #  ", " #   ", "#    ", "#####" },
            ['0'] = new[] { " ### ", "#   #", "#  ##", "# # #", "##  #", "#   #", " ### " },
            ['1'] = new[] { "  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### " },
            ['2'] = new[] { " ### ", "#   #", "    #", "   # ", "  #  ", " #   ", "#####" },
            ['3'] = new[] { "#### ", "    #", "    #", " ### ", "    #", "    #", "#### " },
            ['4'] = new[] { "   # ", "  ## ", " # # ", "#  # ", "#####", "   # ", "   # " },
            ['5'] = new[] { "#####", "#    ", "#### ", "    #", "    #", "#   #", " ### " },
            ['6'] = new[] { " ### ", "#    ", "#    ", "#### ", "#   #", "#   #", " ### " },
            ['7'] = new[] { "#####", "    #", "   # ", "  #  ", " #   ", " #   ", " #   " },
            ['8'] = new[] { " ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### " },
            ['9'] = new[] { " ### ", "#   #", "#   #", " ####", "    #", "    #", " ### " },
            ['.'] = new[] { "     ", "     ", "     ", "     ", "     ", " ##  ", " ##  " },
            [','] = new[] { "     ", "     ", "     ", "     ", " ##  ", "  #  ", " #   " },
            ['\''] = new[] { "  #  ", "  #  ", " #   ", "     ", "     ", "     ", "     " },
            ['!'] = new[] { "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "     ", "  #  " },
            ['-'] = new[] { "     ", "     ", "     ", "#####", "     ", "     ", "     " },
            [':'] = new[] { "     ", "  #  ", "  #  ", "     ", "  #  ", "  #  ", "     " },
            [')'] = new[] { " #   ", "  #  ", "   # ", "   # ", "   # ", "  #  ", " #   " },
        };

        /// <summary>Texture size in "cells" for the text (to size the quad it goes on).</summary>
        public static Vector2 Aspect(string text, bool spray)
        {
            var lines = text.Split('\n');
            int cols = 0;
            foreach (var l in lines) cols = Mathf.Max(cols, l.Length);
            return new Vector2(cols * (W + 1) + 2, lines.Length * (H + 2) + (spray ? 3 : 1));
        }

        public static Texture2D Paint(string text, bool spray, int cell, int seed)
        {
            var alpha = Mask(text, spray, cell, seed, out int tw, out int th);
            var px = new Color32[tw * th];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha[i]) * 255f));
            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, true, false)
            {
                name = "Text_" + text.Replace('\n', '_'), wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
                anisoLevel = 4, hideFlags = HideFlags.DontSave
            };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>The text's coverage (0..1, row 0 at the bottom) without a texture, to stamp into another image (a print's caption, a note).</summary>
        public static float[] Mask(string text, bool spray, int cell, int seed, out int tw, out int th)
        {
            var size = Aspect(text, spray);
            tw = Mathf.RoundToInt(size.x * cell); th = Mathf.RoundToInt(size.y * cell);
            var alpha = new float[tw * th];
            var rng = new System.Random(seed);
            var lines = text.ToUpperInvariant().Split('\n');

            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                for (int ci = 0; ci < line.Length; ci++)
                {
                    if (!Glyphs.TryGetValue(line[ci], out var g)) continue;
                    // A sprayed hand isn't steady: each letter sits a little high or low.
                    float wobble = spray ? (float)(rng.NextDouble() - 0.5) * 0.6f : 0f;
                    for (int r = 0; r < H; r++)
                    for (int c = 0; c < W; c++)
                    {
                        if (g[r][c] != '#') continue;
                        // Cell centre in pixels; texture y grows upward, glyph rows grow downward.
                        float cx = (1f + ci * (W + 1) + c + 0.5f) * cell;
                        float cy = th - (1f + li * (H + 2) + r + 0.5f + wobble) * cell;
                        if (spray)
                        {
                            Disc(alpha, tw, th, cx, cy, cell * 0.8f, 0.9f, rng);
                            bool below = r + 1 < H && g[r + 1][c] == '#';
                            if (!below && rng.NextDouble() < 0.22) Drip(alpha, tw, th, cx + (float)(rng.NextDouble() - 0.5) * cell * 0.5f, cy, cell, rng);
                        }
                        else Block(alpha, tw, th, cx, cy, cell * 0.5f);
                    }
                }
            }

            if (spray)
                for (int k = 0; k < tw * th / 90; k++) // overspray mist
                {
                    int x = rng.Next(tw), y = rng.Next(th);
                    if (alpha[y * tw + x] > 0.05f || Near(alpha, tw, th, x, y, cell)) alpha[y * tw + x] = Mathf.Max(alpha[y * tw + x], (float)rng.NextDouble() * 0.35f);
                }

            return alpha;
        }

        static void Disc(float[] a, int tw, int th, float cx, float cy, float rad, float strength, System.Random rng)
        {
            int x0 = Mathf.Max(0, (int)(cx - rad - 1)), x1 = Mathf.Min(tw - 1, (int)(cx + rad + 1));
            int y0 = Mathf.Max(0, (int)(cy - rad - 1)), y1 = Mathf.Min(th - 1, (int)(cy + rad + 1));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / rad;
                if (d > 1f) continue;
                float v = (1f - Mathf.SmoothStep(0.55f, 1f, d)) * strength * (0.82f + (float)rng.NextDouble() * 0.18f);
                a[y * tw + x] = Mathf.Max(a[y * tw + x], v);
            }
        }

        static void Block(float[] a, int tw, int th, float cx, float cy, float half)
        {
            for (int y = Mathf.Max(0, (int)(cy - half)); y < Mathf.Min(th, (int)(cy + half)); y++)
            for (int x = Mathf.Max(0, (int)(cx - half)); x < Mathf.Min(tw, (int)(cx + half)); x++)
                a[y * tw + x] = 1f;
        }

        /// <summary>Paint running down from the bottom of a stroke, thinning, with a bead at the end.</summary>
        static void Drip(float[] a, int tw, int th, float x, float y, float cell, System.Random rng)
        {
            float len = cell * (1f + (float)rng.NextDouble() * 2.6f), w = cell * 0.13f;
            for (float d = 0f; d < len; d += 0.5f)
            {
                float k = d / len;
                int yy = (int)(y - cell * 0.5f - d);
                if (yy < 0) break;
                for (int xx = (int)(x - w); xx <= (int)(x + w); xx++)
                    if (xx >= 0 && xx < tw) a[yy * tw + xx] = Mathf.Max(a[yy * tw + xx], 0.85f * (1f - k * 0.3f));
            }
            Disc(a, tw, th, x, y - cell * 0.5f - len, w * 1.8f, 0.85f, rng);
        }

        static bool Near(float[] a, int tw, int th, int x, int y, float cell)
        {
            int r = Mathf.CeilToInt(cell);
            for (int k = 0; k < 4; k++)
            {
                int xx = Mathf.Clamp(x + (k == 0 ? r : k == 1 ? -r : 0), 0, tw - 1), yy = Mathf.Clamp(y + (k == 2 ? r : k == 3 ? -r : 0), 0, th - 1);
                if (a[yy * tw + xx] > 0.3f) return true;
            }
            return false;
        }
    }
}
