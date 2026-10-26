using System.Collections.Generic;
using UnityEngine;

namespace Echoes.World
{
    public enum GroundFloor { ShopShutters, ShopBoarded, Residential }

    public struct BuildingSpec
    {
        public float width;        // along the street (local X)
        public float depth;        // away from the street (local Z)
        public int floors;         // including ground floor
        public float floorHeight;  // upper floors
        public GroundFloor ground;
        public bool fireEscape;
        public Material wall;
        public int seed;
    }

    /// <summary>
    /// American walk-up / mixed-use building generator. Local frame: facade on the z = 0 plane facing −Z,
    /// body extends to +Z, X from 0 to width. Everything goes into one mesh with a submesh per material.
    /// </summary>
    public static class BuildingGenerator
    {
        const float GroundH = 4.2f;
        const float Bay = 2.5f;

        struct Opening { public float x0, x1, y0, y1; }

        public static GameObject Build(BuildingSpec s, Palette pal, Transform parent, Vector3 position, float yaw)
        {
            var rng = new System.Random(s.seed);
            var mb = new MeshBuilder();
            float H = GroundH + (s.floors - 1) * s.floorHeight + 0.6f;

            // ---- Body (sides, back, roof). Front is the detailed facade.
            mb.Box(s.wall, new Vector3(s.width / 2f, H / 2f, s.depth / 2f), new Vector3(s.width, H, s.depth), 1 | 2 | 4 | 16, pal.roof);

            // ---- Bay layout
            int bays = Mathf.Max(1, Mathf.FloorToInt((s.width - 0.8f) / Bay));
            float bayW = (s.width - 0.8f) / bays;
            float BayCenter(int i) => 0.4f + bayW * (i + 0.5f);

            // ---- Ground floor
            var gOpen = new List<Opening>();
            int doorBay = rng.Next(bays);
            for (int i = 0; i < bays; i++)
            {
                float cx = BayCenter(i);
                if (i == doorBay)
                {
                    var o = new Opening { x0 = cx - 0.6f, x1 = cx + 0.6f, y0 = 0f, y1 = 2.5f };
                    gOpen.Add(o);
                    Recess(mb, s.wall, o, 0.4f);
                    FacadeRect(mb, pal.doorWood, o.x0, o.y0, o.x1, o.y1, 0.4f);
                    mb.Box(pal.trimStone, new Vector3(cx, 0.08f, 0.2f), new Vector3(1.2f, 0.16f, 0.4f)); // threshold step
                    continue;
                }
                bool shop = s.ground != GroundFloor.Residential;
                var w = shop ? new Opening { x0 = cx - bayW / 2f + 0.25f, x1 = cx + bayW / 2f - 0.25f, y0 = 0.45f, y1 = 3.2f }
                             : new Opening { x0 = cx - 0.6f, x1 = cx + 0.6f, y0 = 1.0f, y1 = 2.8f };
                gOpen.Add(w);
                Recess(mb, s.wall, w, 0.25f);
                Material fill = s.ground == GroundFloor.ShopShutters ? pal.shutter
                              : s.ground == GroundFloor.ShopBoarded || rng.NextDouble() < 0.7 ? pal.boards
                              : pal.glassDark;
                // Boards are nailed close to the face; shutters sit in the recess.
                FacadeRect(mb, fill, w.x0, w.y0, w.x1, w.y1, fill == pal.boards ? 0.04f : 0.22f);
            }
            WallAround(mb, s.wall, gOpen, s.width, 0f, GroundH);
            mb.Box(pal.trimStone, new Vector3(s.width / 2f, 0.2f, -0.03f), new Vector3(s.width, 0.4f, 0.06f), 1 | 2 | 4 | 32);      // plinth
            if (s.ground != GroundFloor.Residential)
                mb.Box(pal.signBand, new Vector3(s.width / 2f, 3.7f, -0.08f), new Vector3(s.width - 0.2f, 0.6f, 0.16f));            // sign band
            mb.Box(pal.trimStone, new Vector3(s.width / 2f, GroundH, -0.06f), new Vector3(s.width, 0.22f, 0.12f));                  // string course

            // ---- Upper floors
            for (int f = 1; f < s.floors; f++)
            {
                float fy = GroundH + (f - 1) * s.floorHeight;
                float wy0 = fy + 0.85f, wy1 = Mathf.Min(wy0 + 1.65f, fy + s.floorHeight - 0.35f);
                var opens = new List<Opening>();
                for (int i = 0; i < bays; i++)
                {
                    float cx = BayCenter(i);
                    var o = new Opening { x0 = cx - 0.55f, x1 = cx + 0.55f, y0 = wy0, y1 = wy1 };
                    opens.Add(o);
                    Window(mb, pal, s.wall, o, rng);
                }
                WallAround(mb, s.wall, opens, s.width, fy, fy + s.floorHeight);
            }
            float topY = GroundH + (s.floors - 1) * s.floorHeight;
            FacadeRect(mb, s.wall, 0f, topY, s.width, H, 0f);

            // ---- Cornice + parapet
            mb.Box(pal.trimStone, new Vector3(s.width / 2f, H - 0.25f, -0.22f), new Vector3(s.width + 0.1f, 0.4f, 0.44f));
            mb.Box(pal.trimStone, new Vector3(s.width / 2f, H - 0.5f, -0.12f), new Vector3(s.width, 0.12f, 0.24f));
            Parapet(mb, s.wall, pal.parapetCap, s.width, s.depth, H);

            // ---- Roof clutter
            RoofProps(mb, pal, s, H, rng);

            // ---- Fire escape
            if (s.fireEscape && s.floors >= 3 && bays >= 2)
            {
                int k = rng.Next(bays - 1);
                FireEscape(mb, pal.metalDark, BayCenter(k) - bayW / 2f + 0.1f, BayCenter(k + 1) + bayW / 2f - 0.1f, s);
            }

            var go = mb.Build("Building", parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(s.width / 2f, H / 2f, s.depth / 2f);
            col.size = new Vector3(s.width, H, s.depth);
            go.isStatic = true;
            return go;
        }

        // ------------------------------------------------------------------ facade pieces

        /// <summary>Quad on a plane of constant z facing −Z.</summary>
        static void FacadeRect(MeshBuilder mb, Material m, float x0, float y0, float x1, float y1, float z)
        {
            if (x1 - x0 < 0.001f || y1 - y0 < 0.001f) return;
            mb.Quad(m, new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x0, y1, z),
                    new Vector2(x0, y0), new Vector2(x1, y1));
        }

        /// <summary>The four inner faces of an opening of the given depth.</summary>
        static void Recess(MeshBuilder mb, Material m, Opening o, float depth)
        {
            float r = depth;
            mb.Quad(m, new Vector3(o.x0, o.y0, 0f), new Vector3(o.x0, o.y0, r), new Vector3(o.x0, o.y1, r), new Vector3(o.x0, o.y1, 0f), new Vector2(0f, o.y0), new Vector2(r, o.y1)); // left, faces +X
            mb.Quad(m, new Vector3(o.x1, o.y0, r), new Vector3(o.x1, o.y0, 0f), new Vector3(o.x1, o.y1, 0f), new Vector3(o.x1, o.y1, r), new Vector2(0f, o.y0), new Vector2(r, o.y1)); // right, faces −X
            mb.Quad(m, new Vector3(o.x0, o.y1, r), new Vector3(o.x1, o.y1, r), new Vector3(o.x1, o.y1, 0f), new Vector3(o.x0, o.y1, 0f), new Vector2(o.x0, 0f), new Vector2(o.x1, r)); // top, faces −Y
            if (o.y0 > 0.001f)
                mb.Quad(m, new Vector3(o.x0, o.y0, 0f), new Vector3(o.x1, o.y0, 0f), new Vector3(o.x1, o.y0, r), new Vector3(o.x0, o.y0, r), new Vector2(o.x0, 0f), new Vector2(o.x1, r)); // bottom, faces +Y
        }

        /// <summary>Fills a horizontal band of facade with wall, leaving holes for the openings.</summary>
        static void WallAround(MeshBuilder mb, Material m, List<Opening> opens, float width, float y0, float y1)
        {
            opens.Sort((a, b) => a.x0.CompareTo(b.x0));
            float x = 0f;
            foreach (var o in opens)
            {
                FacadeRect(mb, m, x, y0, o.x0, y1, 0f);    // pier
                FacadeRect(mb, m, o.x0, y0, o.x1, o.y0, 0f); // below
                FacadeRect(mb, m, o.x0, o.y1, o.x1, y1, 0f); // above
                x = o.x1;
            }
            FacadeRect(mb, m, x, y0, width, y1, 0f);
        }

        static void Window(MeshBuilder mb, Palette pal, Material wall, Opening o, System.Random rng)
        {
            const float depth = 0.18f;
            Recess(mb, wall, o, depth);
            double r = rng.NextDouble();
            float cx = (o.x0 + o.x1) / 2f, w = o.x1 - o.x0;

            if (r < 0.07)
            {
                // Boarded up from the outside for the night.
                FacadeRect(mb, pal.boards, o.x0 - 0.05f, o.y0 - 0.05f, o.x1 + 0.05f, o.y1 + 0.05f, -0.03f);
            }
            else
            {
                Material glass = r < 0.25 ? pal.glassLit[rng.Next(pal.glassLit.Length)] : pal.glassDark;
                FacadeRect(mb, glass, o.x0, o.y0, o.x1, o.y1, depth - 0.02f);
                // Double-hung frame: outline + meeting rail.
                const float t = 0.05f;
                float zf = depth - 0.05f;
                mb.Box(pal.windowFrame, new Vector3(cx, (o.y0 + o.y1) / 2f, zf), new Vector3(w, 0.06f, t));
                mb.Box(pal.windowFrame, new Vector3(cx, o.y0 + t / 2f, zf), new Vector3(w, t, t));
                mb.Box(pal.windowFrame, new Vector3(cx, o.y1 - t / 2f, zf), new Vector3(w, t, t));
                mb.Box(pal.windowFrame, new Vector3(o.x0 + t / 2f, (o.y0 + o.y1) / 2f, zf), new Vector3(t, o.y1 - o.y0, t));
                mb.Box(pal.windowFrame, new Vector3(o.x1 - t / 2f, (o.y0 + o.y1) / 2f, zf), new Vector3(t, o.y1 - o.y0, t));
            }
            // Stone sill and lintel.
            mb.Box(pal.trimStone, new Vector3(cx, o.y0 - 0.04f, -0.04f), new Vector3(w + 0.16f, 0.08f, 0.12f));
            mb.Box(pal.trimStone, new Vector3(cx, o.y1 + 0.09f, -0.02f), new Vector3(w + 0.24f, 0.18f, 0.05f));

            // Window AC unit — the most New-York detail there is.
            if (r > 0.88)
            {
                mb.Box(pal.galvanized, new Vector3(cx, o.y0 + 0.22f, -0.12f), new Vector3(0.66f, 0.42f, 0.6f));
                mb.Box(pal.metalDark, new Vector3(cx, o.y0 + 0.22f, -0.425f), new Vector3(0.56f, 0.32f, 0.01f));
            }
        }

        static void Parapet(MeshBuilder mb, Material wall, Material cap, float w, float d, float H)
        {
            const float t = 0.25f, h = 0.9f;
            mb.Box(wall, new Vector3(w / 2f, H + h / 2f, t / 2f), new Vector3(w, h, t), 0b111111, cap);
            mb.Box(wall, new Vector3(w / 2f, H + h / 2f, d - t / 2f), new Vector3(w, h, t), 0b111111, cap);
            mb.Box(wall, new Vector3(t / 2f, H + h / 2f, d / 2f), new Vector3(t, h, d - 2f * t), 0b111111, cap);
            mb.Box(wall, new Vector3(w - t / 2f, H + h / 2f, d / 2f), new Vector3(t, h, d - 2f * t), 0b111111, cap);
        }

        static void RoofProps(MeshBuilder mb, Palette pal, BuildingSpec s, float H, System.Random rng)
        {
            int ac = rng.Next(0, 3);
            for (int i = 0; i < ac; i++)
            {
                var c = new Vector3(Range(rng, 1.5f, s.width - 1.5f), H + 0.6f, Range(rng, 2f, s.depth - 2f));
                mb.Box(pal.galvanized, c, new Vector3(1.6f, 1.2f, 1.1f));
            }
            for (int i = 0; i < 2; i++)
                mb.Cylinder(pal.metalDark, new Vector3(Range(rng, 1f, s.width - 1f), H, Range(rng, 1.5f, s.depth - 1.5f)), 0.12f, 1.1f, 8);

            if (s.floors >= 5 && rng.NextDouble() < 0.5)
            {
                // Wooden water tank on a steel stand.
                var b = new Vector3(Range(rng, 2.5f, s.width - 2.5f), H, Range(rng, 3f, s.depth - 3f));
                foreach (var o in new[] { new Vector3(-1f, 0, -1f), new Vector3(1f, 0, -1f), new Vector3(1f, 0, 1f), new Vector3(-1f, 0, 1f) })
                    mb.Box(pal.metalDark, b + o + Vector3.up * 0.9f, new Vector3(0.12f, 1.8f, 0.12f));
                mb.Cylinder(pal.boards, b + Vector3.up * 1.8f, 1.45f, 2.6f, 20, true, pal.roof);
                mb.Cylinder(pal.roof, b + Vector3.up * 4.4f, 1.1f, 0.35f, 20);
                mb.Cylinder(pal.roof, b + Vector3.up * 4.75f, 0.6f, 0.25f, 16);
            }
        }

        static void FireEscape(MeshBuilder mb, Material metal, float x0, float x1, BuildingSpec s)
        {
            const float depth = 1.1f, railH = 1.0f, bar = 0.035f;
            float w = x1 - x0, cx = (x0 + x1) / 2f;
            float prevY = -1f;
            for (int f = 1; f < s.floors; f++)
            {
                float y = GroundH + (f - 1) * s.floorHeight;
                // Grated platform + brackets.
                mb.Box(metal, new Vector3(cx, y, -depth / 2f), new Vector3(w, 0.05f, depth));
                mb.BoxRotated(metal, new Vector3(x0 + 0.1f, y - 0.35f, -depth / 2f), new Vector3(0.05f, 0.05f, 1.25f), Quaternion.Euler(-35f, 0f, 0f));
                mb.BoxRotated(metal, new Vector3(x1 - 0.1f, y - 0.35f, -depth / 2f), new Vector3(0.05f, 0.05f, 1.25f), Quaternion.Euler(-35f, 0f, 0f));
                // Railing.
                mb.Box(metal, new Vector3(cx, y + railH, -depth), new Vector3(w, bar, bar));
                mb.Box(metal, new Vector3(x0, y + railH, -depth / 2f), new Vector3(bar, bar, depth));
                mb.Box(metal, new Vector3(x1, y + railH, -depth / 2f), new Vector3(bar, bar, depth));
                for (float bx = x0; bx <= x1 + 0.01f; bx += 0.3f)
                    mb.Box(metal, new Vector3(bx, y + railH / 2f, -depth), new Vector3(0.02f, railH, 0.02f));
                for (float bz = -depth; bz < 0f; bz += 0.3f)
                {
                    mb.Box(metal, new Vector3(x0, y + railH / 2f, bz), new Vector3(0.02f, railH, 0.02f));
                    mb.Box(metal, new Vector3(x1, y + railH / 2f, bz), new Vector3(0.02f, railH, 0.02f));
                }

                // Stair from the platform below, rising left → right.
                if (prevY > 0f)
                {
                    float run = w * 0.6f, rise = y - prevY;
                    float len = Mathf.Sqrt(run * run + rise * rise);
                    float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
                    var mid = new Vector3(x0 + 0.2f + run / 2f, (prevY + y) / 2f, -depth * 0.5f);
                    mb.BoxRotated(metal, mid + new Vector3(0, 0, -0.32f), new Vector3(len, 0.15f, 0.03f), Quaternion.Euler(0f, 0f, ang));
                    mb.BoxRotated(metal, mid + new Vector3(0, 0, 0.32f), new Vector3(len, 0.15f, 0.03f), Quaternion.Euler(0f, 0f, ang));
                    int steps = Mathf.RoundToInt(rise / 0.2f);
                    for (int k = 1; k < steps; k++)
                    {
                        float t = k / (float)steps;
                        mb.Box(metal, new Vector3(x0 + 0.2f + run * t, prevY + rise * t, -depth * 0.5f), new Vector3(0.22f, 0.025f, 0.62f));
                    }
                }
                prevY = y;
            }

            // Counter-balanced drop ladder hanging below the first platform.
            float ly = GroundH;
            float lx = x1 - 0.5f;
            mb.Box(metal, new Vector3(lx - 0.22f, ly - 0.9f, -depth + 0.1f), new Vector3(0.04f, 1.8f, 0.04f));
            mb.Box(metal, new Vector3(lx + 0.22f, ly - 0.9f, -depth + 0.1f), new Vector3(0.04f, 1.8f, 0.04f));
            for (float ry = ly - 1.7f; ry < ly; ry += 0.3f)
                mb.Box(metal, new Vector3(lx, ry, -depth + 0.1f), new Vector3(0.44f, 0.03f, 0.03f));
        }

        static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
    }
}
