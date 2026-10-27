using System.Collections.Generic;
using UnityEngine;
using Echoes.Rendering;

namespace Echoes.World
{
    /// <summary>
    /// The gas station on the west side of Harlow Ave (demo sequence 3): a forecourt with a lit canopy and two pump
    /// islands, a convenience store you can walk into (smashed front windows, a counter, shelves, a back door), a
    /// walled lot with a dark corridor along the north wall (the way round the back), and a gate in the back wall
    /// lit by a work lamp: the way on. The canopy and store run on the station's generator, so they stay lit
    /// through the blackout. Built in the gap left in the west building row (lot z −92..−62, x −11..−42).
    /// </summary>
    public static class GasStationBuilder
    {
        public const float LotZMin = -92f, LotZMax = -62f, FrontX = -11f, BackX = -42f;
        public const float StoreX0 = -27f, StoreX1 = -39f, StoreZ0 = -86f, StoreZ1 = -68f, StoreH = 4.2f;
        public const float DoorZ0 = -78f, DoorZ1 = -76f;
        public const float GateZ0 = -76f, GateZ1 = -73f;
        public static readonly Vector3 GateCenter = new Vector3(BackX, DistrictGenerator.Curb, (GateZ0 + GateZ1) / 2f);

        const float Wall = 0.25f;

        /// <param name="gapMin">South edge of the gap actually cleared in the building row (≤ LotZMin).</param>
        /// <param name="gapMax">North edge of the cleared gap (≥ LotZMax).</param>
        public static void Build(Palette pal, FireFX.Materials fx, Transform parent, float gapMin, float gapMax, System.Random rng)
        {
            var root = new GameObject("GasStation").transform;
            root.SetParent(parent, false);
            float y = DistrictGenerator.Curb;

            // Forecourt asphalt over the sidewalk slab.
            var ground = new MeshBuilder();
            ground.Box(pal.asphalt, new Vector3(-18.5f, y + 0.005f, (LotZMin + LotZMax) / 2f), new Vector3(15f, 0.01f, LotZMax - LotZMin), 0b000100); // x −11..−26
            ground.Build("Forecourt", root, false).isStatic = true;

            Walls(pal, root, y, gapMin, gapMax);
            Canopy(pal, root, y);
            Store(pal, root, y, rng);
            Cover(pal, root, y, rng);
            Gate(pal, root, y);
        }

        // ------------------------------------------------------------------ lot walls

        static void Walls(Palette pal, Transform root, float y, float gapMin, float gapMax)
        {
            var s = new Solid();
            const float h = 2.4f, t = 0.3f;
            // North and south walls at the edges of the cleared gap, from the street front to the back wall. They stand
            // 30 cm off the neighbouring buildings' side walls (no z-fighting) and close the gap behind their backs.
            float zn = gapMax - 0.3f, zs = gapMin + 0.3f;
            s.Box(pal.curb, new Vector3((FrontX + BackX) / 2f, y + h / 2f, zn), new Vector3(FrontX - BackX, h, t));
            s.Box(pal.curb, new Vector3((FrontX + BackX) / 2f, y + h / 2f, zs), new Vector3(FrontX - BackX, h, t));
            // Back wall with the gate opening.
            s.Box(pal.curb, new Vector3(BackX - t / 2f, y + h / 2f, (gapMin + GateZ0) / 2f), new Vector3(t, h, GateZ0 - gapMin));
            s.Box(pal.curb, new Vector3(BackX - t / 2f, y + h / 2f, (GateZ1 + gapMax) / 2f), new Vector3(t, h, gapMax - GateZ1));
            s.Build("LotWalls", root);
        }

        // ------------------------------------------------------------------ canopy and pumps

        static void Canopy(Palette pal, Transform root, float y)
        {
            var s = new Solid();
            const float x0 = -13f, x1 = -24f, z0 = -84f, z1 = -68f, top = 5.2f;
            foreach (float x in new[] { -14.2f, -22.8f })
                foreach (float z in new[] { -70.5f, -81.5f })
                    s.Box(pal.galvanized, new Vector3(x, y + 2.3f, z), new Vector3(0.35f, 4.6f, 0.35f));
            s.Box(pal.galvanized, new Vector3((x0 + x1) / 2f, y + top - 0.3f, (z0 + z1) / 2f), new Vector3(x0 - x1, 0.6f, z1 - z0), false);
            // Red fascia band round the edge.
            s.Box(pal.signRed, new Vector3((x0 + x1) / 2f, y + top - 0.3f, (z0 + z1) / 2f), new Vector3(x0 - x1 + 0.06f, 0.4f, z1 - z0 + 0.06f), false);
            // Light panels under the roof.
            for (int i = 0; i < 3; i++)
                for (int k = 0; k < 2; k++)
                    s.Box(pal.signWhite, new Vector3(-16f - k * 5f, y + top - 0.62f, -81f + i * 5.5f), new Vector3(1.6f, 0.04f, 1.6f), false);
            // Pump islands: raised kerb, two pumps each (red body, lit display, hose).
            foreach (float x in new[] { -16.5f, -20.5f })
            {
                s.Box(pal.curb, new Vector3(x, y + 0.08f, -76f), new Vector3(1.2f, 0.16f, 7f));
                foreach (float z in new[] { -77.8f, -74.2f })
                {
                    s.Box(pal.hydrantRed, new Vector3(x, y + 1.05f, z), new Vector3(0.55f, 1.8f, 0.8f));
                    s.Box(pal.signWhite, new Vector3(x, y + 1.6f, z), new Vector3(0.57f, 0.32f, 0.5f), false);
                    s.Box(pal.tire, new Vector3(x + 0.3f, y + 0.9f, z + 0.25f), new Vector3(0.05f, 0.8f, 0.05f), false);
                }
            }
            s.Build("Canopy", root);

            // Generator-powered lights: one shadowed spot (the only one here), the rest cheap.
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("CanopyLight", typeof(Light));
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(new Vector3(-18.5f, y + top - 0.7f, -81f + i * 5.5f), Quaternion.Euler(90f, 0f, 0f));
                var l = go.GetComponent<Light>();
                l.type = LightType.Spot;
                l.color = new Color(0.9f, 0.95f, 1f);
                l.intensity = 9f;
                l.range = 11f;
                l.spotAngle = 140f;
                l.innerSpotAngle = 70f;
                l.shadows = i == 1 ? LightShadows.Soft : LightShadows.None;
            }
        }

        // ------------------------------------------------------------------ the store

        static void Store(Palette pal, Transform root, float y, System.Random rng)
        {
            var s = new Solid();
            Material wall = pal.facadeWalls[0];
            float x0 = StoreX0, x1 = StoreX1, z0 = StoreZ0, z1 = StoreZ1, h = StoreH;
            float midX = (x0 + x1) / 2f, depth = x0 - x1;
            // Side and back walls (the back has the rear door).
            s.Box(wall, new Vector3(midX, y + h / 2f, z1 - Wall / 2f), new Vector3(depth, h, Wall));
            s.Box(wall, new Vector3(midX, y + h / 2f, z0 + Wall / 2f), new Vector3(depth, h, Wall));
            const float backDoor0 = -84f, backDoor1 = -82.8f, doorH = 2.3f;
            s.Box(wall, new Vector3(x1 + Wall / 2f, y + h / 2f, (z0 + backDoor0) / 2f), new Vector3(Wall, h, backDoor0 - z0));
            s.Box(wall, new Vector3(x1 + Wall / 2f, y + h / 2f, (backDoor1 + z1) / 2f), new Vector3(Wall, h, z1 - backDoor1));
            s.Box(wall, new Vector3(x1 + Wall / 2f, y + (h + doorH) / 2f, (backDoor0 + backDoor1) / 2f), new Vector3(Wall, h - doorH, backDoor1 - backDoor0), false);
            // Front wall: door in the middle, a smashed shop window either side (sill below, header above).
            FrontSegment(s, wall, y, z0, -85f, h);
            FrontSegment(s, wall, y, -80f, DoorZ0, h);
            FrontSegment(s, wall, y, DoorZ1, -74f, h);
            FrontSegment(s, wall, y, -69f, z1, h);
            foreach (var (a, b) in new[] { (-85f, -80f), (-74f, -69f) })
            {
                s.Box(wall, new Vector3(x0 - Wall / 2f, y + 0.5f, (a + b) / 2f), new Vector3(Wall, 1f, b - a));
                s.Box(wall, new Vector3(x0 - Wall / 2f, y + (h + 2.4f) / 2f, (a + b) / 2f), new Vector3(Wall, h - 2.4f, b - a), false);
                s.Box(pal.windowFrame, new Vector3(x0 - Wall / 2f, y + 1.02f, (a + b) / 2f), new Vector3(Wall + 0.04f, 0.05f, b - a), false);
            }
            s.Box(wall, new Vector3(x0 - Wall / 2f, y + (h + doorH) / 2f, (DoorZ0 + DoorZ1) / 2f), new Vector3(Wall, h - doorH, DoorZ1 - DoorZ0), false);
            // Roof slab and a lit sign band over the windows.
            s.Box(pal.roof, new Vector3(midX, y + h + 0.15f, (z0 + z1) / 2f), new Vector3(depth + 0.4f, 0.3f, z1 - z0 + 0.4f), false);
            s.Box(pal.signRed, new Vector3(x0 + 0.05f, y + h - 0.45f, (z0 + z1) / 2f), new Vector3(0.12f, 0.6f, z1 - z0 - 1f), false);
            // Floor tiles, the counter with its register, shelves of stock.
            s.Box(pal.sidewalk, new Vector3(midX, y + 0.01f, (z0 + z1) / 2f), new Vector3(depth - 0.1f, 0.02f, z1 - z0 - 0.1f), false);
            s.Box(pal.doorWood, new Vector3(-36f, y + 0.52f, -76.5f), new Vector3(0.8f, 1.04f, 7f));
            s.Box(pal.metalDark, new Vector3(-36f, y + 1.06f, -76.5f), new Vector3(0.9f, 0.04f, 7.1f), false);
            s.Box(pal.metalDark, new Vector3(-35.9f, y + 1.25f, -74.5f), new Vector3(0.4f, 0.32f, 0.35f), false);
            s.Box(pal.doorWood, new Vector3(-38.4f, y + 1.1f, -76.5f), new Vector3(0.5f, 2.2f, 6f)); // cigarette wall behind the counter
            foreach (float x in new[] { -29.6f, -31.8f, -34f })
                foreach (var (a, b) in new[] { (-85f, -80.2f), (-73.6f, -69f) })
                {
                    float zc = (a + b) / 2f, len = b - a;
                    s.Box(pal.metalDark, new Vector3(x, y + 0.9f, zc), new Vector3(0.6f, 1.8f, len));
                    for (int level = 0; level < 3; level++)
                        for (float z = a + 0.2f; z < b - 0.2f; z += 0.32f)
                        {
                            if (rng.NextDouble() < 0.3) continue; // already looted
                            var m = pal.Pick(rng.NextDouble() < 0.5 ? pal.carPaint : pal.facadeWalls, rng);
                            float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                            s.Box(m, new Vector3(x + side * 0.32f, y + 0.35f + level * 0.55f, z), new Vector3(0.08f, 0.26f, 0.22f), false);
                        }
                }
            // Stock knocked onto the floor.
            for (int i = 0; i < 26; i++)
            {
                var p = new Vector3(Range(rng, x1 + 1f, x0 - 1f), y + 0.06f, Range(rng, z0 + 1f, z1 - 1f));
                s.mb.BoxRotated(pal.Pick(pal.carPaint, rng), p, new Vector3(0.22f, 0.1f, 0.14f), Quaternion.Euler(0f, Range(rng, 0f, 360f), 0f));
            }
            // Smashed window glass on the forecourt.
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector3(x0 + Range(rng, 0.2f, 2.5f), y + 0.01f, Range(rng, -85f, -69f));
                s.mb.BoxRotated(pal.carGlass, p, new Vector3(0.05f, 0.005f, 0.04f), Quaternion.Euler(0f, Range(rng, 0f, 360f), 0f));
            }
            s.Build("Store", root);

            // Flickering tubes inside (generator power, about to go).
            var lg = new GameObject("StoreLight", typeof(Light));
            lg.transform.SetParent(root, false);
            lg.transform.position = new Vector3(midX, y + h - 0.4f, -77f);
            var l = lg.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.85f, 0.95f, 0.9f);
            l.range = 11f;
            l.shadows = LightShadows.None;
            var f = lg.AddComponent<LightFlicker>();
            f.mode = LightFlicker.Mode.Neon;
            f.baseIntensity = 2.2f;
        }

        static void FrontSegment(Solid s, Material wall, float y, float a, float b, float h)
        {
            if (b - a < 0.01f) return;
            s.Box(wall, new Vector3(StoreX0 - Wall / 2f, y + h / 2f, (a + b) / 2f), new Vector3(Wall, h, b - a));
        }

        // ------------------------------------------------------------------ cover and the way round the back

        static void Cover(Palette pal, Transform root, float y, System.Random rng)
        {
            // A car left at a pump, a van and pallets along the dark north corridor: cover for a fight or a sneak.
            PropFactory.Sedan(pal, root, new Vector3(-18.5f, y, -79.5f), 90f, pal.Pick(pal.carPaint, rng), false, rng);
            PropFactory.Sedan(pal, root, new Vector3(-20f, y, -64.6f), 0f, pal.Pick(pal.carPaint, rng), false, rng);
            PropFactory.Dumpster(pal, root, new Vector3(-30.5f, y, -64f), 0f, rng);
            var s = new Solid();
            foreach (var (x, z, n) in new[] { (-34.5f, -64.2f, 3), (-37.3f, -65f, 2), (-40.6f, -80f, 2) })
                for (int k = 0; k < n; k++)
                    s.Box(pal.boards, new Vector3(x, y + 0.08f + k * 0.16f, z), new Vector3(1.2f, 0.14f, 1.0f));
            s.Build("Pallets", root);
        }

        static void Gate(Palette pal, Transform root, float y)
        {
            // One leaf of a steel bar gate, swung open; a work lamp over the opening so the way on reads at night.
            var s = new Solid();
            float hinge = GateZ1;
            for (int i = 0; i < 9; i++)
                s.Box(pal.metalDark, new Vector3(BackX - 0.4f - i * 0.17f, y + 1f, hinge - 0.1f), new Vector3(0.03f, 2f, 0.03f), false);
            s.Box(pal.metalDark, new Vector3(BackX - 0.75f, y + 1.95f, hinge - 0.1f), new Vector3(1.5f, 0.05f, 0.05f), false);
            s.Box(pal.metalDark, new Vector3(BackX - 0.75f, y + 0.15f, hinge - 0.1f), new Vector3(1.5f, 0.05f, 0.05f), false);
            s.Box(pal.metalDark, new Vector3(BackX - 0.15f, y + 2.6f, (GateZ0 + GateZ1) / 2f), new Vector3(0.2f, 0.12f, 0.35f), false);
            s.Box(pal.lampGlass, new Vector3(BackX - 0.15f, y + 2.53f, (GateZ0 + GateZ1) / 2f), new Vector3(0.16f, 0.02f, 0.3f), false);
            s.Build("BackGate", root);
            var go = new GameObject("GateLamp", typeof(Light));
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(BackX + 0.3f, y + 2.4f, (GateZ0 + GateZ1) / 2f);
            var l = go.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.75f, 0.45f);
            l.intensity = 2.2f;
            l.range = 7f;
            l.shadows = LightShadows.None;
        }

        static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
    }
}
