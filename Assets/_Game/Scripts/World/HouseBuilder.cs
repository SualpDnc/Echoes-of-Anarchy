using UnityEngine;

namespace Echoes.World
{
    /// <summary>
    /// Daniel's house (8b): a two-storey detached house on the north alley across from the gas station's back gate, where
    /// the AlleyBuilder rows leave a lot. Static parts only (walls with door and window openings, floors, the stairs with
    /// a ramp collider, the porch, fences, the big furniture), built with the scene so the NavMesh covers it and the
    /// Smilers can come in. Downstairs: the living room (front, south), the hall and stairs (front, north), the kitchen
    /// (back). Upstairs: Mia's room (front, south, with the closet), the landing, Daniel's room (back, north) and the
    /// bathroom off it. HouseSequence adds what needs runtime textures and motion (fabric, doors, the phone, the photo).
    /// Furniture that should look like fabric is built in objects named "HouseFabric_*": HouseSequence swaps their materials.
    /// </summary>
    public static class HouseBuilder
    {
        public const float X0 = -78.5f, X1 = -68.5f, Z0 = -70.5f, Z1 = -57.5f; // footprint: the front faces +X (the north alley)
        public const float XM = -73.5f, ZM = -63f;                               // interior walls
        public const float LotX0 = -80f, LotX1 = -66f, LotZ0 = -72f, LotZ1 = -54f;
        public const float Rise = 0.35f, Storey = 2.9f, Ceiling = 2.7f, T = 0.2f;
        public const float DoorZ = -60.5f, BackDoorZ = -65f;
        public const float StairX0 = -73.4f, StairX1 = -72.3f, StairBottomZ = -57.9f, StairTopZ = -62.7f, OpeningZ1 = -58.5f;
        public const float ClosetX0 = -73.4f, ClosetX1 = -72.7f, ClosetZ0 = -70.4f, ClosetZ1 = -68.8f;

        public static float F0 => DistrictGenerator.Curb + Rise;   // ground floor level
        public static float F1 => F0 + Storey;                      // upper floor level

        public static Vector3 FrontDoor => new Vector3(X1, F0, DoorZ);
        public static Vector3 BackDoor => new Vector3(X0, F0, BackDoorZ);
        public static Vector3 FrontGate => new Vector3(LotX1, DistrictGenerator.Curb, DoorZ);
        public static Vector3 BackGate => new Vector3(LotX0, DistrictGenerator.Curb, BackDoorZ);
        public static Vector3 MiaBed => new Vector3(-72.4f, F1, -66.1f);
        public static Vector3 Closet => new Vector3((ClosetX0 + ClosetX1) / 2f, F1, (ClosetZ0 + ClosetZ1) / 2f);
        public static Vector3 DanielDoor => new Vector3(XM, F1, -58.2f);
        public static Vector3 Landing => new Vector3(-70.5f, F1, -60.5f);

        struct Hole { public float a, b, lo, hi; }
        static Hole Door(float a, float b) => new Hole { a = a, b = b, lo = 0f, hi = 2.1f };
        static Hole Win(float a, float b, float lo = 0.9f, float hi = 2.1f) => new Hole { a = a, b = b, lo = lo, hi = hi };

        public static void Build(Palette pal, Transform parent, System.Random rng)
        {
            var root = new GameObject("House").transform;
            root.SetParent(parent, false);
            var outside = pal.facadeWalls[1 % pal.facadeWalls.Length];
            var inside = pal.trimStone;

            Shell(pal, root, outside, inside);
            Stairs(pal, root, inside);
            Porch(pal, root);
            Yard(pal, root, rng);
            Furniture(pal, root);
            Roof(pal, root, outside);
        }

        // ------------------------------------------------------------------ walls and floors

        static void Shell(Palette pal, Transform root, Material outside, Material inside)
        {
            var s = new Solid();
            float y0 = DistrictGenerator.Curb;
            // Foundation with the ground floor on top; kitchen tiles.
            s.mb.Box(pal.trimStone, new Vector3((X0 + X1) / 2f, (y0 + F0) / 2f, (Z0 + Z1) / 2f), new Vector3(X1 - X0, F0 - y0, Z1 - Z0), 0b111111, pal.boards);
            s.Collider(new Vector3((X0 + X1) / 2f, (y0 + F0) / 2f, (Z0 + Z1) / 2f), new Vector3(X1 - X0, F0 - y0, Z1 - Z0));
            s.mb.Box(pal.sidewalk, new Vector3((X0 + XM) / 2f, F0 + 0.004f, (Z0 + Z1) / 2f), new Vector3(XM - X0 - T, 0.008f, Z1 - Z0 - 2f * T), 0b000100);

            // Upper floor: a slab with the stairwell cut out of it.
            float sy = F1 - 0.1f;
            void Slab(float xa, float xb, float za, float zb)
            {
                s.mb.Box(inside, new Vector3((xa + xb) / 2f, sy, (za + zb) / 2f), new Vector3(xb - xa, 0.2f, zb - za), 0b111111, pal.boards);
                s.Collider(new Vector3((xa + xb) / 2f, sy, (za + zb) / 2f), new Vector3(xb - xa, 0.2f, zb - za));
            }
            Slab(X0, StairX0, Z0, Z1);
            Slab(StairX1, X1, Z0, Z1);
            Slab(StairX0, StairX1, Z0, StairTopZ);
            Slab(StairX0, StairX1, OpeningZ1, Z1);
            // Ceiling upstairs.
            s.mb.Box(inside, new Vector3((X0 + X1) / 2f, F1 + Ceiling + 0.075f, (Z0 + Z1) / 2f), new Vector3(X1 - X0, 0.15f, Z1 - Z0));

            float hG = Storey, hU = Ceiling;
            // Ground floor, outside walls.
            WallZ(s, X1 - T / 2f, Z0, Z1, F0, hG, outside, inside, Win(-67.3f, -65.5f), Door(-61f, -60f));                 // front
            WallZ(s, X0 + T / 2f, Z0, Z1, F0, hG, inside, outside, Win(-69f, -67.5f), Door(BackDoorZ - 0.5f, BackDoorZ + 0.5f), Win(-61f, -59.5f, 1.05f)); // back
            WallX(s, Z0 + T / 2f, X0, X1, F0, hG, inside, outside, Win(-77f, -76f), Win(-71.5f, -70f));                    // south
            WallX(s, Z1 - T / 2f, X0, X1, F0, hG, outside, inside, Win(-77f, -75.5f));                                    // north
            // Ground floor, inside walls.
            WallZ(s, XM, Z0 + T, Z1 - T, F0, hG, inside, inside, Door(-64.5f, -63.5f));                                     // kitchen | living + hall
            WallX(s, ZM, XM + T / 2f, X1 - T, F0, hG, inside, inside, Door(-70.6f, -69.6f));                               // living | hall

            // Upper floor, outside walls.
            WallZ(s, X1 - T / 2f, Z0, Z1, F1, hU, outside, inside, Win(-67.3f, -65.5f), Win(-60.2f, -59f));
            WallZ(s, X0 + T / 2f, Z0, Z1, F1, hU, inside, outside, Win(-68.5f, -67.6f, 1.4f, 2f), Win(-61f, -59.5f));
            WallX(s, Z0 + T / 2f, X0, X1, F1, hU, inside, outside, Win(-71.5f, -70f));
            WallX(s, Z1 - T / 2f, X0, X1, F1, hU, outside, inside, Win(-77f, -75.5f));
            // Upper floor, inside walls: Daniel's door off the landing, Mia's door, the bathroom off Daniel's room.
            WallZ(s, XM, Z0 + T, Z1 - T, F1, hU, inside, inside, Door(-58.7f, -57.75f));
            WallX(s, ZM, XM + T / 2f, X1 - T, F1, hU, inside, inside, Door(-71.2f, -70.2f));
            WallX(s, ZM, X0 + T, XM - T / 2f, F1, hU, inside, inside, Door(-76f, -75f));
            // Gables: the house is two storeys plus the roof space; close it between the ceiling and the roof.
            s.Build("HouseShell", root);
        }

        /// <summary>A wall along Z at x, two skins (the +X side and the −X side), with door and window holes.</summary>
        static void WallZ(Solid s, float x, float z0, float z1, float y, float h, Material plusX, Material minusX, params Hole[] holes)
        {
            System.Array.Sort(holes, (p, q) => p.a.CompareTo(q.a));
            void Piece(float a, float b, float lo, float hi)
            {
                if (b - a < 0.01f || hi - lo < 0.01f) return;
                s.mb.Box(plusX, new Vector3(x + T / 4f, y + (lo + hi) / 2f, (a + b) / 2f), new Vector3(T / 2f, hi - lo, b - a));
                s.mb.Box(minusX, new Vector3(x - T / 4f, y + (lo + hi) / 2f, (a + b) / 2f), new Vector3(T / 2f, hi - lo, b - a));
                s.Collider(new Vector3(x, y + (lo + hi) / 2f, (a + b) / 2f), new Vector3(T, hi - lo, b - a));
            }
            float c = z0;
            foreach (var o in holes)
            {
                Piece(c, o.a, 0f, h);
                Piece(o.a, o.b, 0f, o.lo);
                Piece(o.a, o.b, o.hi, h);
                if (o.lo > 0f) s.Collider(new Vector3(x, y + (o.lo + o.hi) / 2f, (o.a + o.b) / 2f), new Vector3(T, o.hi - o.lo, o.b - o.a)); // no climbing through windows
                c = o.b;
            }
            Piece(c, z1, 0f, h);
        }

        /// <summary>A wall along X at z, two skins (the +Z side and the −Z side), with door and window holes.</summary>
        static void WallX(Solid s, float z, float x0, float x1, float y, float h, Material plusZ, Material minusZ, params Hole[] holes)
        {
            System.Array.Sort(holes, (p, q) => p.a.CompareTo(q.a));
            void Piece(float a, float b, float lo, float hi)
            {
                if (b - a < 0.01f || hi - lo < 0.01f) return;
                s.mb.Box(plusZ, new Vector3((a + b) / 2f, y + (lo + hi) / 2f, z + T / 4f), new Vector3(b - a, hi - lo, T / 2f));
                s.mb.Box(minusZ, new Vector3((a + b) / 2f, y + (lo + hi) / 2f, z - T / 4f), new Vector3(b - a, hi - lo, T / 2f));
                s.Collider(new Vector3((a + b) / 2f, y + (lo + hi) / 2f, z), new Vector3(b - a, hi - lo, T));
            }
            float c = x0;
            foreach (var o in holes)
            {
                Piece(c, o.a, 0f, h);
                Piece(o.a, o.b, 0f, o.lo);
                Piece(o.a, o.b, o.hi, h);
                if (o.lo > 0f) s.Collider(new Vector3((o.a + o.b) / 2f, y + (o.lo + o.hi) / 2f, z), new Vector3(o.b - o.a, o.hi - o.lo, T));
                c = o.b;
            }
            Piece(c, x1, 0f, h);
        }

        // ------------------------------------------------------------------ stairs

        static void Stairs(Palette pal, Transform root, Material inside)
        {
            var s = new Solid();
            const int steps = 16;
            float run = (StairBottomZ - StairTopZ) / steps, rise = Storey / steps;
            // The bottom of the flight faces the north wall, so the first two steps reach out into the hall with rounded
            // ends (a curtail and a bullnose): you step on from the hall and turn up the flight (owner: the way onto the
            // stairs from the ground floor looked wrong; the first step stopped 20 cm short of the wall).
            float wallZ = Z1 - T;
            float[] reach = { 0.3f, 0.12f };
            for (int i = 0; i < steps; i++)
            {
                float za = i == 0 ? wallZ : StairBottomZ - i * run, zb = StairBottomZ - (i + 1) * run, top = F0 + (i + 1) * rise;
                float x1 = i < reach.Length ? StairX1 + reach[i] : StairX1;
                s.mb.Box(inside, new Vector3((StairX0 + x1) / 2f, (F0 + top) / 2f, (za + zb) / 2f), new Vector3(x1 - StairX0, top - F0, za - zb), 0b111111, pal.boards);
                if (i >= reach.Length) continue;
                float r = (za - zb) / 2f;
                s.mb.Cylinder(inside, new Vector3(x1, F0, (za + zb) / 2f), r, top - F0, 20, true, pal.boards);
                s.Collider(new Vector3((StairX0 + x1 + r) / 2f, (F0 + top) / 2f, (za + zb) / 2f), new Vector3(x1 + r - StairX0, top - F0, za - zb));
            }
            // A stringer along the open side hides the sawtooth of the stacked steps.
            float Nose(float z) => F0 + (StairBottomZ - z) / run * rise;
            Vector3 sa = new Vector3(StairX1 + 0.015f, Nose(StairBottomZ - 2f * run) - 0.16f, StairBottomZ - 2f * run);
            Vector3 sb = new Vector3(StairX1 + 0.015f, Nose(StairTopZ) - 0.16f, StairTopZ);
            s.mb.BoxRotated(pal.doorWood, (sa + sb) / 2f, new Vector3(0.03f, 0.3f, Vector3.Distance(sa, sb)), Quaternion.LookRotation(sb - sa));

            // Banister on the open side: a newel post on the third step, a baluster on every step, a rail following the
            // pitch, and a top newel where the upstairs railing begins (owner: the old rail ran on past the top step).
            float banA = StairBottomZ - 2.5f * run, banB = StairTopZ + 1f;
            float RailY(float z) => Nose(z) + 0.9f;
            float StepTop(float z) => F0 + Mathf.Clamp(Mathf.Ceil((StairBottomZ - z) / run), 1f, steps) * rise;
            const float bx = StairX1 - 0.06f;
            void Newel(float z, float top)
            {
                float foot = StepTop(z);
                s.mb.Box(pal.doorWood, new Vector3(bx, (foot + top) / 2f, z), new Vector3(0.1f, top - foot, 0.1f));
                s.mb.Box(pal.doorWood, new Vector3(bx, top + 0.03f, z), new Vector3(0.14f, 0.06f, 0.14f));
            }
            Newel(banA, RailY(banA) + 0.1f);
            Newel(banB, F1 + 0.98f);
            for (int i = 3; ; i++)
            {
                float z = StairBottomZ - (i + 0.5f) * run;
                if (z < banB + 0.1f) break;
                float foot = StepTop(z), top = RailY(z);
                s.mb.Box(pal.doorWood, new Vector3(bx, (foot + top) / 2f, z), new Vector3(0.035f, top - foot, 0.035f));
            }
            Vector3 ra = new Vector3(bx, RailY(banA), banA), rb = new Vector3(bx, RailY(banB), banB);
            s.mb.BoxRotated(pal.doorWood, (ra + rb) / 2f, new Vector3(0.07f, 0.06f, Vector3.Distance(ra, rb)), Quaternion.LookRotation(rb - ra));
            // It holds Daniel on the flight (it is a thin wall on the stairs' edge; nothing in the hall is behind it).
            s.Collider(new Vector3(bx, (StepTop(banA) + RailY(banB)) / 2f, (banA + banB) / 2f), new Vector3(0.08f, RailY(banB) - StepTop(banA), banA - banB));
            // Railing round the stairwell upstairs. The first metre on the landing side stays open: that is the way off.
            float ry = F1;
            s.Box(pal.doorWood, new Vector3(StairX1 + 0.03f, ry + 0.95f, (StairTopZ + 1f + OpeningZ1) / 2f), new Vector3(0.06f, 0.06f, OpeningZ1 - StairTopZ - 1f), false);
            s.Box(pal.doorWood, new Vector3((StairX0 + StairX1) / 2f, ry + 0.95f, OpeningZ1 + 0.03f), new Vector3(StairX1 - StairX0, 0.06f, 0.06f), false);
            for (float z = StairTopZ + 1f; z <= OpeningZ1 + 0.01f; z += 0.45f)
                s.mb.Box(pal.doorWood, new Vector3(StairX1 + 0.03f, ry + 0.47f, z), new Vector3(0.04f, 0.94f, 0.04f));
            s.Collider(new Vector3(StairX1 + 0.03f, ry + 0.5f, (StairTopZ + 1f + OpeningZ1) / 2f), new Vector3(0.08f, 1f, OpeningZ1 - StairTopZ - 1f));
            s.Collider(new Vector3((StairX0 + StairX1) / 2f, ry + 0.5f, OpeningZ1 + 0.03f), new Vector3(StairX1 - StairX0, 1f, 0.08f));
            s.Build("HouseStairs", root);

            // One smooth ramp under the steps for the character controller and the NavMesh (31°, under both limits).
            Vector3 bottom = new Vector3((StairX0 + StairX1) / 2f, F0, StairBottomZ + 0.15f), top2 = new Vector3((StairX0 + StairX1) / 2f, F1, StairTopZ);
            Vector3 along = top2 - bottom;
            var ramp = new GameObject("StairRamp");
            ramp.transform.SetParent(root, false);
            ramp.isStatic = true;
            var rot = Quaternion.LookRotation(along.normalized, Vector3.up);
            ramp.transform.SetPositionAndRotation((bottom + top2) / 2f - rot * Vector3.up * 0.05f, rot);
            var bc = ramp.AddComponent<BoxCollider>();
            bc.size = new Vector3(StairX1 - StairX0, 0.1f, along.magnitude + 0.1f);
        }

        // ------------------------------------------------------------------ outside

        /// <summary>
        /// The front porch (owner: make the entrance look good): a deck the width of the door bay with two full-width
        /// steps, a railing either side, two posts under a little roof, a lantern by the door (dead: no power), a frame
        /// and a transom round the door, and solar path lights that still glow in a blackout.
        /// </summary>
        static void Porch(Palette pal, Transform root)
        {
            var s = new Solid();
            float y0 = DistrictGenerator.Curb;
            const float px1 = -67f, half = 1.8f;
            float step = (F0 - y0) / 2f;
            s.mb.Box(pal.trimStone, new Vector3((X1 + px1) / 2f, (y0 + F0) / 2f, DoorZ), new Vector3(px1 - X1, F0 - y0, half * 2f), 0b111111, pal.boards);
            s.Collider(new Vector3((X1 + px1) / 2f, (y0 + F0) / 2f, DoorZ), new Vector3(px1 - X1, F0 - y0, half * 2f));
            // Two steps across the whole front.
            s.mb.Box(pal.trimStone, new Vector3(px1 + 0.17f, y0 + step / 2f, DoorZ), new Vector3(0.34f, step, half * 2f - 0.6f), 0b111111, pal.boards);
            s.Collider(new Vector3(px1 + 0.17f, y0 + step / 2f, DoorZ), new Vector3(0.34f, step, half * 2f - 0.6f));
            // Railings on the two open sides of the deck, and in front either side of the steps.
            void Rail(Vector3 a, Vector3 b)
            {
                Vector3 d = b - a;
                float len = d.magnitude;
                var rot = Quaternion.LookRotation(d / len);
                s.mb.BoxRotated(pal.trimStone, (a + b) / 2f + Vector3.up * 0.9f, new Vector3(0.07f, 0.06f, len), rot);
                s.mb.BoxRotated(pal.trimStone, (a + b) / 2f + Vector3.up * 0.12f, new Vector3(0.06f, 0.05f, len), rot);
                for (float u = 0.12f; u < len - 0.05f; u += 0.13f)
                {
                    Vector3 p = a + d / len * u;
                    s.mb.Box(pal.trimStone, p + Vector3.up * 0.5f, new Vector3(0.03f, 0.78f, 0.03f));
                }
                s.Collider((a + b) / 2f + Vector3.up * 0.5f, new Vector3(Mathf.Abs(d.x) + 0.08f, 1f, Mathf.Abs(d.z) + 0.08f));
            }
            Rail(new Vector3(X1 + 0.05f, F0, DoorZ - half + 0.06f), new Vector3(px1 - 0.06f, F0, DoorZ - half + 0.06f));
            Rail(new Vector3(X1 + 0.05f, F0, DoorZ + half - 0.06f), new Vector3(px1 - 0.06f, F0, DoorZ + half - 0.06f));
            Rail(new Vector3(px1 - 0.06f, F0, DoorZ - half + 0.06f), new Vector3(px1 - 0.06f, F0, DoorZ - 0.85f));
            Rail(new Vector3(px1 - 0.06f, F0, DoorZ + 0.85f), new Vector3(px1 - 0.06f, F0, DoorZ + half - 0.06f));
            // Posts and the porch roof, sloping a little toward the alley.
            foreach (float z in new[] { DoorZ - half + 0.06f, DoorZ + half - 0.06f })
                s.Box(pal.trimStone, new Vector3(px1 - 0.06f, F0 + 1.3f, z), new Vector3(0.14f, 2.6f, 0.14f));
            s.mb.BoxRotated(pal.roof, new Vector3((X1 + px1) / 2f + 0.15f, F0 + 2.68f, DoorZ), new Vector3(px1 - X1 + 0.5f, 0.1f, half * 2f + 0.4f), Quaternion.Euler(0f, 0f, -8f));
            s.mb.Box(pal.trimStone, new Vector3(px1 - 0.06f, F0 + 2.56f, DoorZ), new Vector3(0.16f, 0.18f, half * 2f));        // beam
            // The door bay: a painted frame, a transom light over the door, a lantern on the wall (dead: no power).
            float fx = X1 + 0.02f;
            s.mb.Box(pal.trimStone, new Vector3(fx, F0 + 1.1f, DoorZ - 0.56f), new Vector3(0.05f, 2.2f, 0.1f));
            s.mb.Box(pal.trimStone, new Vector3(fx, F0 + 1.1f, DoorZ + 0.56f), new Vector3(0.05f, 2.2f, 0.1f));
            s.mb.Box(pal.trimStone, new Vector3(fx, F0 + 2.55f, DoorZ), new Vector3(0.05f, 0.1f, 1.22f));
            s.mb.Box(pal.glassDark, new Vector3(X1 - 0.02f, F0 + 2.3f, DoorZ), new Vector3(0.02f, 0.38f, 1f));
            s.mb.Box(pal.metalDark, new Vector3(X1 + 0.1f, F0 + 1.95f, DoorZ - 0.85f), new Vector3(0.16f, 0.32f, 0.16f));
            s.mb.Box(pal.lampGlass, new Vector3(X1 + 0.1f, F0 + 1.93f, DoorZ - 0.85f), new Vector3(0.12f, 0.22f, 0.12f));
            s.Build("HousePorch", root);
        }

        static void Yard(Palette pal, Transform root, System.Random rng)
        {
            var s = new Solid();
            float y = DistrictGenerator.Curb;
            s.mb.Box(pal.dirt, new Vector3((LotX0 + LotX1) / 2f, y + 0.003f, (LotZ0 + LotZ1) / 2f), new Vector3(LotX1 - LotX0, 0.006f, LotZ1 - LotZ0), 0b000100);
            s.mb.Box(pal.sidewalk, new Vector3((-67.1f + LotX1) / 2f, y + 0.008f, DoorZ), new Vector3(LotX1 - -67.1f, 0.01f, 1.1f), 0b000100);

            // Front: a white picket fence on the alley, the gate standing open.
            const float fx = LotX1 - 0.2f, gate0 = DoorZ - 0.55f, gate1 = DoorZ + 0.55f;
            void Pickets(float za, float zb)
            {
                for (float z = za + 0.06f; z < zb - 0.03f; z += 0.14f)
                    s.mb.Box(pal.trimStone, new Vector3(fx, y + 0.5f, z), new Vector3(0.025f, 1f, 0.08f));
                foreach (float h in new[] { 0.25f, 0.8f })
                    s.mb.Box(pal.trimStone, new Vector3(fx - 0.03f, y + h, (za + zb) / 2f), new Vector3(0.03f, 0.07f, zb - za));
                s.Collider(new Vector3(fx, y + 0.6f, (za + zb) / 2f), new Vector3(0.1f, 1.2f, zb - za));
            }
            Pickets(LotZ0, gate0);
            Pickets(gate1, LotZ1);
            // The gate leaf swung right back against the inside of the fence, clear of the way in.
            s.mb.Box(pal.trimStone, new Vector3(fx - 0.06f, y + 0.5f, gate1 + 0.55f), new Vector3(0.03f, 0.95f, 1.05f));

            // Back: a tall board fence on the back alley with a gate.
            const float bx = LotX0 + 0.05f, b0 = BackDoorZ - 0.6f, b1 = BackDoorZ + 0.6f;
            void Boards(float za, float zb)
            {
                s.mb.Box(pal.boards, new Vector3(bx, y + 0.9f, (za + zb) / 2f), new Vector3(0.05f, 1.8f, zb - za));
                s.Collider(new Vector3(bx, y + 0.9f, (za + zb) / 2f), new Vector3(0.1f, 1.8f, zb - za));
            }
            Boards(LotZ0, b0);
            Boards(b1, LotZ1);
            // The back gate leaf is HouseSequence's: bolted shut until the raid, so the back alley (and the subway) wait for the house.

            // A child's bike on its side by the porch, a trash can by the back door.
            s.mb.Cylinder(pal.metalDark, new Vector3(-67.6f, y + 0.02f, -64.2f), 0.3f, 0.04f, 18, true);
            s.mb.Cylinder(pal.metalDark, new Vector3(-67.6f, y + 0.02f, -65.1f), 0.3f, 0.04f, 18, true);
            s.mb.Box(pal.hydrantRed, new Vector3(-67.6f, y + 0.06f, -64.65f), new Vector3(0.05f, 0.05f, 0.85f));
            s.Build("HouseYard", root);
            PropFactory.TrashCan(pal, root, new Vector3(LotX0 + 0.6f, y, BackDoorZ - 1.4f), false, rng);
        }

        // ------------------------------------------------------------------ furniture (the pieces with colliders)

        static void Furniture(Palette pal, Transform root)
        {
            var wood = pal.doorWood;
            var dark = pal.metalDark;
            // Fabric pieces, each its own object so HouseSequence can give it a fabric material.
            void FabricPiece(string name, params (Vector3 c, Vector3 s)[] boxes)
            {
                var f = new Solid();
                foreach (var (c, sz) in boxes) f.Box(pal.signBand, c, sz);
                f.Build("HouseFabric_" + name, root);
            }
            float g = F0, u = F1;
            // Living room: the sofa against the kitchen wall, facing the TV in the corner by the front window.
            FabricPiece("Sofa",
                (new Vector3(-72.85f, g + 0.22f, -68.4f), new Vector3(0.9f, 0.44f, 2.3f)),
                (new Vector3(-73.25f, g + 0.6f, -68.4f), new Vector3(0.22f, 0.75f, 2.3f)),
                (new Vector3(-72.85f, g + 0.45f, -69.62f), new Vector3(0.9f, 0.5f, 0.16f)),
                (new Vector3(-72.85f, g + 0.45f, -67.18f), new Vector3(0.9f, 0.5f, 0.16f)));
            FabricPiece("BedMia",
                (new Vector3(-72.4f, u + 0.25f, -66.1f), new Vector3(2f, 0.5f, 1f)),
                (new Vector3(-73.32f, u + 0.55f, -66.1f), new Vector3(0.1f, 0.9f, 1f)));
            FabricPiece("BedDaniel",
                (new Vector3(-77.3f, u + 0.25f, -60.4f), new Vector3(2f, 0.5f, 1.5f)),
                (new Vector3(-78.25f, u + 0.6f, -60.4f), new Vector3(0.1f, 1f, 1.5f)));

            var s = new Solid();
            // Living room.
            s.Box(dark, new Vector3(-69.05f, g + 0.25f, -69.15f), new Vector3(0.55f, 0.5f, 1.4f));                // TV stand
            s.mb.Box(pal.glassDark, new Vector3(-69.1f, g + 0.85f, -69.15f), new Vector3(0.06f, 0.65f, 1.15f));  // TV
            s.Box(wood, new Vector3(-71f, g + 0.21f, -68.3f), new Vector3(0.8f, 0.42f, 1f));                      // coffee table
            // Photo frames on the living-room side of the hall wall; one bare rectangle where the Marine portrait was.
            for (int i = 0; i < 4; i++)
                s.mb.Box(i == 2 ? pal.trimStone : dark, new Vector3(-72.6f + i * 0.55f, g + 1.6f + (i % 2) * 0.12f, ZM - T / 2f - 0.012f), new Vector3(0.36f, 0.45f, 0.02f));
            // Kitchen: counters along the back wall, the fridge, a table with four chairs.
            s.Box(wood, new Vector3(-78.05f, g + 0.45f, -60.4f), new Vector3(0.6f, 0.9f, 4.4f));
            s.mb.Box(pal.trimStone, new Vector3(-78.05f, g + 0.915f, -60.4f), new Vector3(0.64f, 0.03f, 4.44f));
            s.Box(pal.galvanized, new Vector3(-78.0f, g + 0.9f, -63.5f), new Vector3(0.7f, 1.8f, 0.75f));
            s.Box(wood, new Vector3(-75.75f, g + 0.73f, -68.3f), new Vector3(1.5f, 0.05f, 1.4f), false);
            s.Collider(new Vector3(-75.75f, g + 0.37f, -68.3f), new Vector3(1.5f, 0.74f, 1.4f));
            foreach (var (cx, cz) in new[] { (-76.9f, -68.3f), (-74.6f, -68.3f), (-75.75f, -69.4f), (-75.75f, -67.2f) })
            {
                s.mb.Box(wood, new Vector3(cx, g + 0.45f, cz), new Vector3(0.45f, 0.04f, 0.45f));
                s.mb.Box(wood, new Vector3(cx, g + 0.22f, cz), new Vector3(0.05f, 0.44f, 0.05f));
            }
            // Hall: a shoe bench.
            s.Box(wood, new Vector3(-69.05f, g + 0.22f, -58.4f), new Vector3(0.45f, 0.44f, 1f));
            // Mia's room: the closet (doors are HouseSequence's), a desk under the side wall, a shelf.
            float cxm = (ClosetX0 + ClosetX1) / 2f;
            s.Box(wood, new Vector3(cxm, u + 2.2f, (ClosetZ0 + ClosetZ1) / 2f), new Vector3(ClosetX1 - ClosetX0, 0.1f, ClosetZ1 - ClosetZ0));   // top
            s.Box(wood, new Vector3(cxm, u + 1.1f, ClosetZ0 - 0.0f + 0.02f), new Vector3(ClosetX1 - ClosetX0, 2.2f, 0.04f));                   // sides
            s.Box(wood, new Vector3(cxm, u + 1.1f, ClosetZ1 - 0.02f), new Vector3(ClosetX1 - ClosetX0, 2.2f, 0.04f));
            s.mb.Box(dark, new Vector3(cxm, u + 1.8f, (ClosetZ0 + ClosetZ1) / 2f), new Vector3(0.02f, 0.02f, ClosetZ1 - ClosetZ0 - 0.1f));    // rail
            s.Box(wood, new Vector3(-69.05f, u + 0.74f, -69.3f), new Vector3(0.6f, 0.04f, 1.2f), false);                                      // desk
            s.Collider(new Vector3(-69.05f, u + 0.37f, -69.3f), new Vector3(0.6f, 0.74f, 1.2f));
            s.mb.Box(wood, new Vector3(-69.05f, u + 0.37f, -69.85f), new Vector3(0.55f, 0.74f, 0.04f));
            s.mb.Box(wood, new Vector3(-69.05f, u + 0.37f, -68.75f), new Vector3(0.55f, 0.74f, 0.04f));
            s.mb.Box(wood, new Vector3(X1 - T - 0.12f, u + 1.6f, -69.3f), new Vector3(0.22f, 0.03f, 1f));                                     // shelf over the desk
            // Daniel's room: a wardrobe. Bathroom: the tub, a sink.
            s.Box(wood, new Vector3(-75.2f, u + 1f, Z1 - T - 0.3f), new Vector3(1.4f, 2f, 0.6f));
            s.Box(pal.trimStone, new Vector3(-77.95f, u + 0.28f, -69.4f), new Vector3(0.75f, 0.56f, 1.7f));
            s.Box(pal.trimStone, new Vector3(-74.1f, u + 0.42f, -69.9f), new Vector3(0.5f, 0.84f, 0.45f));
            s.Build("HouseFurniture", root);
        }

        // ------------------------------------------------------------------ roof

        static void Roof(Palette pal, Transform root, Material outside)
        {
            var mb = new MeshBuilder();
            float eave = F1 + Ceiling + 0.15f, ridge = eave + 1.9f, xr = (X0 + X1) / 2f, over = 0.4f;
            float half = (X1 - X0) / 2f + over, slope = Mathf.Atan2(ridge - eave, half) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(half * half + (ridge - eave) * (ridge - eave));
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 c = new Vector3(xr + side * half / 2f, (eave + ridge) / 2f, (Z0 + Z1) / 2f);
                mb.BoxRotated(pal.roof, c, new Vector3(len, 0.12f, Z1 - Z0 + 2f * over), Quaternion.Euler(0f, 0f, -side * slope));
            }
            // Gable ends.
            foreach (float z in new[] { Z0, Z1 })
            {
                Vector3 n = new Vector3(0f, 0f, z < (Z0 + Z1) / 2f ? -1f : 1f);
                mb.Polygon(outside, n, true, new Vector3(X0, eave - 0.15f, z), new Vector3(X1, eave - 0.15f, z), new Vector3(xr, ridge - 0.1f, z));
            }
            var go = mb.Build("HouseRoof", root);
            go.isStatic = true;
        }
    }
}
