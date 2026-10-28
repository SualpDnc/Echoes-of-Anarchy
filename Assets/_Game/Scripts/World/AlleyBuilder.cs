using UnityEngine;
using Echoes.Rendering;

namespace Echoes.World
{
    /// <summary>
    /// The back alleys west of the gas station (demo sequence 4) and the school they lead to. Out of the station's back
    /// gate: the gate alley runs west to a junction; the north alley runs up to a barred fence, with a dead-end pocket to
    /// the south; halfway up, a roll-up shutter stuck 1.5 m open on the left leads into a garage you can only cross crouched,
    /// whose back door lets out into a parallel alley. That one runs north to the school yard's gate. The school (lit:
    /// its floodlights are on in the blackout) faces the yard. Everything else is built up: buildings face the alleys,
    /// their backs close the area, so the only way through is the route. Built on the empty block behind the west row.
    /// </summary>
    public static class AlleyBuilder
    {
        // Walkable corridors (x0..x1, z0..z1).
        public const float GateAlleyZ0 = -76.5f, GateAlleyZ1 = -72.5f;        // x −62..−42
        public const float NorthAlleyX0 = -66f, NorthAlleyX1 = -62f;          // z −90..−30 (fence), −22 beyond it
        public const float NorthFenceZ = -30f, NorthAlleySouthZ = -90f;
        public const float BackAlleyX0 = -84f, BackAlleyX1 = -80f;            // z −72..−22 (8b: down to the back gate of Daniel's house)
        public const float BackAlleySouthZ = -72f;
        public const float GarageZ0 = -44f, GarageZ1 = -36f;                  // x −80..−66
        public const float ShutterZ0 = -42f, ShutterZ1 = -38f, ShutterGap = 1.5f; // crouched Daniel is 1.15 m, the NavMesh agent 2 m
        public const float GarageDoorZ = -40f;
        public const float YardX0 = -100f, YardX1 = -72f, YardZ0 = -22f, YardZ1 = -6f;
        public const float SchoolZ1 = 5f;

        public static readonly Vector3 ShutterPoint = new Vector3(NorthAlleyX0, DistrictGenerator.Curb, (ShutterZ0 + ShutterZ1) / 2f);
        public static readonly Vector3 YardGate = new Vector3((BackAlleyX0 + BackAlleyX1) / 2f, DistrictGenerator.Curb, YardZ0);
        public static readonly Vector3 SchoolDoor = new Vector3((YardX0 + YardX1) / 2f, DistrictGenerator.Curb, YardZ1);

        const float Depth = 14f;

        public static void Build(Palette pal, FireFX.Materials fx, Transform parent, System.Random rng)
        {
            var root = new GameObject("Alleys").transform;
            root.SetParent(parent, false);
            float y = DistrictGenerator.Curb;

            Buildings(pal, root, y, rng);
            Garage(pal, root, y, rng);
            Walls(pal, root, y);
            Yard(pal, root, y, rng);
            Clutter(pal, fx, root, y, rng);
            Lights(pal, root, y);
        }

        // ------------------------------------------------------------------ buildings

        static void Buildings(Palette pal, Transform root, float y, System.Random rng)
        {
            var b = new GameObject("AlleyBuildings").transform;
            b.SetParent(root, false);
            // Gate alley: north side (backs onto the north alley's east row), south side.
            Row(pal, b, new Vector3(-62f, y, GateAlleyZ1), Vector3.back, 19.6f, 12.5f, rng, 2, 4);
            Row(pal, b, new Vector3(-42.4f, y, GateAlleyZ0), Vector3.forward, 19.6f, Depth, rng, 2, 4);
            // North alley, east side, from the gate alley up to the building closing its north end.
            Row(pal, b, new Vector3(NorthAlleyX1, y, -22f), Vector3.left, 38f, Depth, rng, 3, 5);
            // North alley, west side, south and north of the garage (their backs line the back alley). Daniel's house
            // (HouseBuilder) takes the lot between the two southern rows, across from the gate alley.
            Row(pal, b, new Vector3(NorthAlleyX0, y, NorthAlleySouthZ), Vector3.right, HouseBuilder.LotZ0 - NorthAlleySouthZ, Depth, rng, 2, 4);
            Row(pal, b, new Vector3(NorthAlleyX0, y, HouseBuilder.LotZ1), Vector3.right, GarageZ0 - HouseBuilder.LotZ1, Depth, rng, 2, 3);
            Row(pal, b, new Vector3(NorthAlleyX0, y, GarageZ1), Vector3.right, -22f - GarageZ1, Depth, rng, 3, 4);
            // Back alley, west side.
            Row(pal, b, new Vector3(BackAlleyX0, y, BackAlleySouthZ), Vector3.right, -60f - BackAlleySouthZ, Depth, rng, 2, 3);
            Row(pal, b, new Vector3(BackAlleyX0, y, -60f), Vector3.right, 38f, Depth, rng, 3, 5);
            // Closing the north alley and the yard's east side.
            Row(pal, b, new Vector3(-48f, y, YardZ0), Vector3.back, 24f, Depth, rng, 2, 3, mirror: true);

            // The school: two long storeys of brick facing the yard. Its door and the scene at it are 6e's.
            BuildingGenerator.Build(new BuildingSpec
            {
                width = YardX1 - YardX0, depth = SchoolZ1 - YardZ1, floors = 2, floorHeight = 3.6f,
                ground = GroundFloor.Residential, fireEscape = false, wall = pal.facadeWalls[0], seed = rng.Next()
            }, pal, b, new Vector3(YardX0, y, YardZ1), 0f);
        }

        /// <summary>
        /// Buildings in a row along `normal`'s left, facades facing `normal`, with a fixed depth so their backs line up
        /// straight (they are the walls of the next alley). `mirror` runs the row from the far end (start = right end).
        /// </summary>
        static void Row(Palette pal, Transform parent, Vector3 start, Vector3 normal, float length, float depth, System.Random rng,
                        int minFloors, int maxFloors, bool mirror = false)
        {
            float yaw = Mathf.Atan2(-normal.x, -normal.z) * Mathf.Rad2Deg;
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            if (mirror) start -= along * length;
            float offset = 0f;
            while (offset < length - 0.5f)
            {
                float w = Range(rng, 8f, 14f);
                if (length - offset - w < 6f) w = length - offset;
                BuildingGenerator.Build(new BuildingSpec
                {
                    width = w, depth = depth, floors = rng.Next(minFloors, maxFloors + 1), floorHeight = Range(rng, 3.0f, 3.3f),
                    ground = GroundFloor.Residential, fireEscape = rng.NextDouble() < 0.7,
                    wall = pal.Pick(pal.facadeWalls, rng), seed = rng.Next()
                }, pal, parent, start + along * offset, yaw);
                offset += w;
            }
        }

        // ------------------------------------------------------------------ the garage you crawl through

        static void Garage(Palette pal, Transform root, float y, System.Random rng)
        {
            var s = new Solid();
            const float h = 4.5f, t = 0.25f;
            float x0 = BackAlleyX1, x1 = NorthAlleyX0, z0 = GarageZ0, z1 = GarageZ1;
            float cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f;
            var wall = pal.facadeWalls[5];

            s.mb.Box(pal.curb, new Vector3(cx, y + 0.005f, cz), new Vector3(x1 - x0, 0.01f, z1 - z0), 0b000100); // oily slab
            s.Box(pal.roof, new Vector3(cx, y + h + 0.1f, cz), new Vector3(x1 - x0, 0.2f, z1 - z0));
            s.Box(wall, new Vector3(cx, y + h / 2f, z0 + t / 2f), new Vector3(x1 - x0, h, t));
            s.Box(wall, new Vector3(cx, y + h / 2f, z1 - t / 2f), new Vector3(x1 - x0, h, t));

            // East wall on the north alley: the shutter opening, the roll-up shutter stuck at 1.5 m.
            float ex = x1 - t / 2f;
            s.Box(wall, new Vector3(ex, y + h / 2f, (z0 + ShutterZ0) / 2f), new Vector3(t, h, ShutterZ0 - z0));
            s.Box(wall, new Vector3(ex, y + h / 2f, (ShutterZ1 + z1) / 2f), new Vector3(t, h, z1 - ShutterZ1));
            float top = 3.4f;
            s.Box(wall, new Vector3(ex, y + (top + h) / 2f, cz), new Vector3(t, h - top, ShutterZ1 - ShutterZ0));
            float sb = 0.02f; // 8b: shut. (The 6d chase crawled under it at ShutterGap; the house replaced the chase, and an open
                              // shutter would be a way round the house to the school.)
            s.Box(pal.shutter, new Vector3(x1 + 0.06f, y + (sb + top) / 2f, (ShutterZ0 + ShutterZ1) / 2f), new Vector3(0.06f, top - sb, ShutterZ1 - ShutterZ0));
            for (float r = sb + 0.12f; r < top; r += 0.12f)
                s.mb.Box(pal.shutter, new Vector3(x1 + 0.1f, y + r, (ShutterZ0 + ShutterZ1) / 2f), new Vector3(0.02f, 0.03f, ShutterZ1 - ShutterZ0));
            s.mb.Box(pal.metalDark, new Vector3(x1 + 0.1f, y + sb + 0.03f, (ShutterZ0 + ShutterZ1) / 2f), new Vector3(0.08f, 0.06f, ShutterZ1 - ShutterZ0)); // bottom bar
            s.mb.Box(pal.metalDark, new Vector3(x1 + 0.15f, y + top + 0.2f, (ShutterZ0 + ShutterZ1) / 2f), new Vector3(0.3f, 0.4f, ShutterZ1 - ShutterZ0 + 0.3f)); // roll box

            // West wall on the back alley: a plain back door, kicked open.
            float wx = x0 + t / 2f, d0 = GarageDoorZ - 0.6f, d1 = GarageDoorZ + 0.6f;
            s.Box(wall, new Vector3(wx, y + h / 2f, (z0 + d0) / 2f), new Vector3(t, h, d0 - z0));
            s.Box(wall, new Vector3(wx, y + h / 2f, (d1 + z1) / 2f), new Vector3(t, h, z1 - d1));
            s.Box(wall, new Vector3(wx, y + (2.3f + h) / 2f, GarageDoorZ), new Vector3(t, h - 2.3f, 1.2f));
            s.mb.BoxRotated(pal.doorWood, new Vector3(x0 - 0.45f, y + 1.12f, d1 + 0.28f), new Vector3(1.15f, 2.2f, 0.05f), Quaternion.Euler(0f, -60f, 0f));

            // Inside: a car up against the north wall, a workbench, tyres, drums. The way from shutter to door stays clear.
            s.Box(pal.boards, new Vector3(cx - 1f, y + 0.45f, z0 + 0.7f), new Vector3(3.2f, 0.9f, 0.8f));
            s.Box(pal.metalDark, new Vector3(x0 + 0.8f, y + 0.9f, z0 + 1.2f), new Vector3(0.6f, 1.8f, 1.6f)); // tool cabinet
            s.Build("Garage", root);
            PropFactory.Sedan(pal, root, new Vector3(cx + 0.5f, y, z1 - 1.3f), 0f, pal.Pick(pal.carPaint, rng), false, rng); // yaw 0 = along x, by the north wall
            for (int i = 0; i < 4; i++) PropFactory.Tire(pal, root, new Vector3(x1 - 1.2f, y + 0.12f + i * 0.24f, z0 + 0.9f), Range(rng, 0f, 90f));
        }

        // ------------------------------------------------------------------ walls and fences

        static void Walls(Palette pal, Transform root, float y)
        {
            var s = new Solid();
            // Dead ends: the south end of the north alley, the south end of the back alley.
            s.Box(pal.facadeWalls[1], new Vector3((NorthAlleyX0 + NorthAlleyX1) / 2f, y + 1.6f, NorthAlleySouthZ + 0.15f), new Vector3(NorthAlleyX1 - NorthAlleyX0, 3.2f, 0.3f));
            s.Box(pal.facadeWalls[1], new Vector3((BackAlleyX0 + BackAlleyX1) / 2f, y + 1.6f, BackAlleySouthZ - 0.15f), new Vector3(BackAlleyX1 - BackAlleyX0, 3.2f, 0.3f));
            // Yard: west wall, the stub between the back-alley row and it, the stub between the east building and the school.
            s.Box(pal.curb, new Vector3(YardX0 - 0.15f, y + 1.3f, (YardZ0 + YardZ1) / 2f), new Vector3(0.3f, 2.6f, YardZ1 - YardZ0));
            s.Box(pal.curb, new Vector3((YardX0 + BackAlleyX0 - Depth) / 2f, y + 1.3f, YardZ0 - 0.15f), new Vector3(Mathf.Abs(BackAlleyX0 - Depth - YardX0) + 0.3f, 2.6f, 0.3f));
            s.Box(pal.curb, new Vector3(YardX1 + 0.15f, y + 1.3f, (YardZ0 + Depth + YardZ1) / 2f), new Vector3(0.3f, 2.6f, YardZ1 - YardZ0 - Depth + 0.2f));
            s.Build("AlleyWalls", root);

            // Steel palisade across the north alley: you see the school yard's light through it, but it's 3 m of spikes.
            Palisade(pal, root, y, new Vector3(NorthAlleyX0, 0f, NorthFenceZ), new Vector3(NorthAlleyX1, 0f, NorthFenceZ), 3f, true);
        }

        /// <summary>Vertical steel bars with spiked tops between two points; one box collider closes the whole span.</summary>
        static void Palisade(Palette pal, Transform root, float y, Vector3 a, Vector3 b, float h, bool collide)
        {
            var s = new Solid();
            Vector3 d = b - a;
            float len = d.magnitude;
            Vector3 dir = d / len;
            var rot = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up));
            for (float u = 0.06f; u < len; u += 0.14f)
            {
                Vector3 p = a + dir * u;
                s.mb.Box(pal.metalDark, new Vector3(p.x, y + h / 2f, p.z), new Vector3(0.025f, h, 0.025f));
                s.mb.BoxRotated(pal.metalDark, new Vector3(p.x, y + h + 0.04f, p.z), new Vector3(0.04f, 0.04f, 0.04f), rot * Quaternion.Euler(0f, 0f, 45f));
            }
            foreach (float r in new[] { 0.25f, h - 0.3f })
                s.mb.BoxRotated(pal.metalDark, new Vector3((a.x + b.x) / 2f, y + r, (a.z + b.z) / 2f), new Vector3(0.04f, 0.06f, len), Quaternion.LookRotation(dir));
            if (collide) s.Collider(new Vector3((a.x + b.x) / 2f, y + h / 2f, (a.z + b.z) / 2f), new Vector3(Mathf.Abs(d.x) + 0.1f, h, Mathf.Abs(d.z) + 0.1f));
            s.Build("Palisade", root);
        }

        // ------------------------------------------------------------------ the school yard

        static void Yard(Palette pal, Transform root, float y, System.Random rng)
        {
            var s = new Solid();
            float cx = (YardX0 + YardX1) / 2f, cz = (YardZ0 + YardZ1) / 2f;
            // The asphalt, in three pieces round the subway exit's stairwell (8c, MetroBuilder).
            float ex0 = MetroBuilder.EX0, ex1 = MetroBuilder.EX1, ez = MetroBuilder.BZTop;
            s.mb.Box(pal.asphalt, new Vector3((YardX0 + ex0) / 2f, y + 0.006f, cz), new Vector3(ex0 - YardX0, 0.012f, YardZ1 - YardZ0), 0b000100);
            s.mb.Box(pal.asphalt, new Vector3((ex1 + YardX1) / 2f, y + 0.006f, cz), new Vector3(YardX1 - ex1, 0.012f, YardZ1 - YardZ0), 0b000100);
            s.mb.Box(pal.asphalt, new Vector3((ex0 + ex1) / 2f, y + 0.006f, (ez + YardZ1) / 2f), new Vector3(ex1 - ex0, 0.012f, YardZ1 - ez), 0b000100);
            // Faded court lines and a hopscotch.
            const float ly = 0.014f;
            void Line(float x, float z, float sx, float sz) => s.mb.Box(pal.roadPaintWhite, new Vector3(x, y + ly, z), new Vector3(sx, 0.004f, sz), 0b000100);
            Line(-77f, cz, 0.08f, 10f); Line(-93f, cz, 0.08f, 10f);
            Line(-85f, cz - 5f, 16f, 0.08f); Line(-85f, cz + 5f, 16f, 0.08f); Line(-85f, cz, 0.08f, 10f);
            for (int i = 0; i < 6; i++) Line(-91f, -19f + i * 0.6f, 0.55f, 0.04f);

            // A flagpole with no flag. (8d: the hoop and the bench made way for the party's stage and VIP strip, PartyBuilder.)
            s.Box(pal.galvanized, new Vector3(-99.5f, y + 4f, -8f), new Vector3(0.1f, 8f, 0.1f)); // clear of the subway exit
            // A board over the door bays where a school sign was (the name is 6e's problem).
            s.mb.Box(pal.signBand, new Vector3(cx, y + 4.9f, YardZ1 - 0.12f), new Vector3(9f, 0.8f, 0.12f));
            s.Build("SchoolYard", root);

            // The yard gate at the end of the back alley is PartySequence's (8d): chained shut, until the ram truck.
        }

        // ------------------------------------------------------------------ clutter

        static void Clutter(Palette pal, FireFX.Materials fx, Transform root, float y, System.Random rng)
        {
            // Gate alley: bags and a dumpster against the south wall.
            PropFactory.Dumpster(pal, root, new Vector3(-50f, y, GateAlleyZ0 + 0.9f), 0f, rng);
            for (int i = 0; i < 4; i++) PropFactory.GarbageBag(pal, root, new Vector3(Range(rng, -57f, -53f), y, GateAlleyZ0 + Range(rng, 0.4f, 1.1f)), rng);
            PropFactory.TrashCan(pal, root, new Vector3(-45f, y, GateAlleyZ1 - 0.5f), false, rng);

            // North alley: things to run round, all against the walls: at least 2.2 m stays clear everywhere.
            // (A car is 4.8 m long: across this alley it closed it off. Owner: no car here.)
            PropFactory.Dumpster(pal, root, new Vector3(NorthAlleyX1 - 0.65f, y, -66f), 90f, rng);
            var s = new Solid();
            // A junk pile against the west wall: crates, a steel cabinet on its back, a mattress.
            float jx = NorthAlleyX0 + 0.8f;
            s.Box(pal.boards, new Vector3(jx, y + 0.35f, -55f), new Vector3(1.1f, 0.7f, 1.1f));
            s.Box(pal.boards, new Vector3(jx - 0.1f, y + 0.95f, -55.2f), new Vector3(0.8f, 0.5f, 0.8f));
            s.Box(pal.boards, new Vector3(jx, y + 0.3f, -53.8f), new Vector3(0.9f, 0.6f, 0.9f));
            s.Box(pal.galvanized, new Vector3(jx + 0.05f, y + 0.3f, -52.2f), new Vector3(0.75f, 0.6f, 1.8f));
            s.mb.BoxRotated(pal.signBand, new Vector3(NorthAlleyX0 + 0.35f, y + 0.95f, -50.6f), new Vector3(0.18f, 1.9f, 1.3f), Quaternion.Euler(0f, 0f, -14f));
            s.Collider(new Vector3(NorthAlleyX0 + 0.35f, y + 0.95f, -50.6f), new Vector3(0.5f, 1.9f, 1.3f));
            for (int k = 0; k < 3; k++) s.Box(pal.boards, new Vector3(NorthAlleyX1 - 0.6f, y + 0.08f + k * 0.16f, -47f), new Vector3(1.0f, 0.14f, 1.2f));
            s.Build("Pallets", root);
            PropFactory.TrashCan(pal, root, new Vector3(NorthAlleyX1 - 0.5f, y, -36f), true, rng);
            for (int i = 0; i < 5; i++) PropFactory.GarbageBag(pal, root, new Vector3(Range(rng, NorthAlleyX0 + 0.4f, NorthAlleyX1 - 0.4f), y, Range(rng, -34f, -31f)), rng);
            // The dead-end pocket south of the junction: a fire in a drum, someone's camp.
            DistrictGenerator.BurningBarrel(pal, fx, root, new Vector3(NorthAlleyX0 + 1.2f, y, NorthAlleySouthZ + 2.5f));
            PropFactory.Dumpster(pal, root, new Vector3(NorthAlleyX1 - 0.9f, y, NorthAlleySouthZ + 2.2f), 90f, rng);

            // Back alley.
            PropFactory.Dumpster(pal, root, new Vector3(BackAlleyX0 + 0.9f, y, -46f), 90f, rng);
            for (int i = 0; i < 4; i++) PropFactory.GarbageBag(pal, root, new Vector3(BackAlleyX0 + Range(rng, 0.4f, 1.2f), y, Range(rng, -32f, -27f)), rng);
            PropFactory.TrashCan(pal, root, new Vector3(BackAlleyX1 - 0.45f, y, -29f), false, rng);
        }

        // ------------------------------------------------------------------ light

        static void Lights(Palette pal, Transform root, float y)
        {
            var s = new Solid();
            // Wall lamps: a failing one in the gate alley, one up the north alley, one over the back alley.
            WallLamp(pal, s, root, new Vector3(-53f, y + 3.3f, GateAlleyZ1 + 0.05f), Vector3.back, 2f, LightFlicker.Mode.FailingLamp);
            WallLamp(pal, s, root, new Vector3(NorthAlleyX1 + 0.05f, y + 3.4f, -60f), Vector3.left, 1.8f, null);
            WallLamp(pal, s, root, new Vector3(BackAlleyX1 + 0.05f, y + 3.4f, -30f), Vector3.left, 1.8f, null);

            // (8b: the shutter is shut; its work lamp and the light spilling under it are gone.)

            // A tube inside the garage, dying.
            var tube = Point(root, new Vector3((BackAlleyX1 + NorthAlleyX0) / 2f, y + 4.1f, GarageDoorZ), new Color(0.8f, 0.95f, 0.88f), 1.6f, 9f);
            var f = tube.gameObject.AddComponent<LightFlicker>();
            f.mode = LightFlicker.Mode.Neon;
            f.baseIntensity = 1.6f;
            s.mb.Box(pal.lampGlass, new Vector3((BackAlleyX1 + NorthAlleyX0) / 2f, y + 4.35f, GarageDoorZ), new Vector3(1.2f, 0.05f, 0.1f));

            // The school's floodlights: on in a blacked-out city. Someone wants the yard lit.
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -92f : -78f;
                var p = new Vector3(x, y + 6.4f, YardZ1 - 0.35f);
                s.mb.Box(pal.metalDark, p + new Vector3(0f, 0.05f, 0.15f), new Vector3(0.5f, 0.35f, 0.3f));
                s.mb.Box(pal.lampGlass, p + new Vector3(0f, 0f, -0.02f), new Vector3(0.44f, 0.28f, 0.02f));
                Spot(root, p + new Vector3(0f, 0f, -0.1f), new Vector3((-86f - x) * 0.15f, -0.9f, -1f), new Color(0.92f, 0.95f, 1f), 9f, 30f, 95f, i == 0);
            }
            s.Build("AlleyLampFixtures", root);
        }

        static void WallLamp(Palette pal, Solid s, Transform root, Vector3 p, Vector3 facing, float intensity, LightFlicker.Mode? flicker)
        {
            s.mb.Box(pal.metalDark, p + facing * 0.15f + Vector3.up * 0.1f, new Vector3(Mathf.Abs(facing.z) * 0.3f + 0.08f, 0.06f, Mathf.Abs(facing.x) * 0.3f + 0.08f));
            s.mb.Box(pal.lampGlass, p + facing * 0.28f, new Vector3(0.18f, 0.14f, 0.18f));
            var l = Point(root, p + facing * 0.5f - Vector3.up * 0.1f, new Color(1f, 0.78f, 0.5f), intensity, 10f);
            if (flicker.HasValue)
            {
                var f = l.gameObject.AddComponent<LightFlicker>();
                f.mode = flicker.Value;
                f.baseIntensity = intensity;
            }
        }

        static Light Point(Transform root, Vector3 p, Color c, float intensity, float range)
        {
            var go = new GameObject("AlleyLight", typeof(Light));
            go.transform.SetParent(root, false);
            go.transform.position = p;
            var l = go.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        static Light Spot(Transform root, Vector3 p, Vector3 dir, Color c, float intensity, float range, float angle, bool shadows)
        {
            var go = new GameObject("AlleySpot", typeof(Light));
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(dir.normalized));
            var l = go.GetComponent<Light>();
            l.type = LightType.Spot;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = angle;
            l.innerSpotAngle = angle * 0.6f;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
    }
}
