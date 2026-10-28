using System.Collections.Generic;
using UnityEngine;
using Echoes.Rendering;

namespace Echoes.World
{
    /// <summary>
    /// Harlow District layout: a north–south avenue (Harlow Ave) crossed by a side street at z = 30,
    /// with a service alley behind the south-east block. All geometry is generated; nothing is hand-placed.
    /// </summary>
    public static class DistrictGenerator
    {
        public const float RoadHalf = 7f;       // avenue: 2 lanes + parking each side
        public const float Facade = 11f;        // avenue facade line (4 m sidewalk)
        public const float CrossZ = 30f;
        public const float CrossHalf = 5f;
        public const float Extent = 105f;
        public const float Curb = 0.15f;
        const float AlleyX0 = 25f, AlleyX1 = 29f;

        public class Result
        {
            public Vector3 playerSpawn;
            public float playerYaw;
            public readonly List<Vector3> probePoints = new List<Vector3>();
        }

        public static Result Generate(Palette pal, FireFX.Materials fx, Transform root, int seed)
        {
            var rng = new System.Random(seed);
            var res = new Result { playerSpawn = new Vector3(-2f, 0.05f, -88f), playerYaw = 0f };

            BuildGround(pal, root);
            BuildMarkings(pal, root);

            var buildings = new GameObject("Buildings").transform;
            buildings.SetParent(root, false);
            float y = Curb;
            // Avenue rows (depth ≤ 14 so the east row's backs line the alley).
            Row(pal, buildings, new Vector3(-Facade, y, -Extent), Vector3.right, Extent + CrossZ - CrossHalf - 4f, rng, 12f, 14f, 3, 7);
            Row(pal, buildings, new Vector3(-Facade, y, CrossZ + CrossHalf + 4f), Vector3.right, Extent - CrossZ - CrossHalf - 4f, rng, 12f, 14f, 3, 7);
            Row(pal, buildings, new Vector3(Facade, y, CrossZ - CrossHalf - 4f), Vector3.left, Extent + CrossZ - CrossHalf - 4f, rng, 12f, 14f, 3, 7);
            Row(pal, buildings, new Vector3(Facade, y, Extent), Vector3.left, Extent - CrossZ - CrossHalf - 4f, rng, 12f, 14f, 3, 7);
            // Side-street rows.
            float southFace = CrossZ - CrossHalf - 4f, northFace = CrossZ + CrossHalf + 4f;
            Row(pal, buildings, new Vector3(-Facade - 14f, y, southFace), Vector3.forward, Extent - Facade - 14f, rng, 10f, 14f, 2, 5);
            Row(pal, buildings, new Vector3(Extent, y, southFace), Vector3.forward, Extent - AlleyX1, rng, 10f, 14f, 2, 5);
            Row(pal, buildings, new Vector3(-Extent, y, northFace), Vector3.back, Extent - Facade - 14f, rng, 10f, 14f, 2, 5);
            Row(pal, buildings, new Vector3(Facade + 14f, y, northFace), Vector3.back, Extent - Facade - 14f, rng, 10f, 14f, 2, 5);
            // Alley-facing row behind the south-east block: back doors, fire escapes, dumpsters.
            Row(pal, buildings, new Vector3(AlleyX1, y, southFace - 14f), Vector3.left, Extent + southFace - 14f, rng, 10f, 14f, 3, 5, true);
            // End caps so no street runs into the void.
            Row(pal, buildings, new Vector3(Facade, 0f, -Extent), Vector3.forward, Facade * 2f, rng, 10f, 14f, 4, 7);
            Row(pal, buildings, new Vector3(-Facade, 0f, Extent), Vector3.back, Facade * 2f, rng, 10f, 14f, 4, 7);
            Row(pal, buildings, new Vector3(-Extent, 0f, CrossZ - CrossHalf - 4f), Vector3.right, CrossHalf * 2f + 8f, rng, 9f, 12f, 3, 5);
            Row(pal, buildings, new Vector3(Extent, 0f, CrossZ + CrossHalf + 4f), Vector3.left, CrossHalf * 2f + 8f, rng, 9f, 12f, 3, 5);

            var props = new GameObject("Props").transform;
            props.SetParent(root, false);
            StreetProps(pal, fx, props, rng);
            Skyline(pal, root, rng);
            // Gas station (sequence 3). Uses its own rng and clears its lot after the rows are built, so every other
            // building and prop stays exactly as it was.
            GasStationLot(pal, fx, buildings, props, seed);
            // The back alleys and the school (sequence 4), on the empty block behind the west row; own rng as well.
            AlleyBuilder.Build(pal, fx, props, new System.Random(seed + 404));
            // Daniel's house on the lot the alleys leave across from the gate alley (8b); own rng too.
            HouseBuilder.Build(pal, props, new System.Random(seed + 505));
            // The dead subway station under the back alley and the school yard (8c); own rng.
            MetroBuilder.Build(pal, fx, props, new System.Random(seed + 606));
            // The Purge party in the school yard (8d): no randomness.
            PartyBuilder.Build(pal, fx, props);

            for (float z = -85f; z <= 95f; z += 30f) res.probePoints.Add(new Vector3(0f, 2.5f, z));
            res.probePoints.Add(new Vector3(-64f, 2.5f, -60f));
            res.probePoints.Add(new Vector3(-71f, HouseBuilder.F0 + 1.5f, -60.5f));
            res.probePoints.Add(new Vector3(-71f, HouseBuilder.F1 + 1.4f, -66.5f));
            res.probePoints.Add(new Vector3(-76f, HouseBuilder.F0 + 1.5f, -64f));
            res.probePoints.Add(new Vector3(-82f, MetroBuilder.FL + 2f, -44f));
            res.probePoints.Add(new Vector3(-100f, MetroBuilder.FL + 2f, -37f));
            res.probePoints.Add(new Vector3(-80f, MetroBuilder.FL + 2f, -27f));
            res.probePoints.Add(new Vector3(-82f, 2.5f, -36f));
            res.probePoints.Add(new Vector3(-86f, 3f, -14f));
            res.probePoints.Add(new Vector3(-50f, 2.5f, CrossZ));
            res.probePoints.Add(new Vector3(50f, 2.5f, CrossZ));
            res.probePoints.Add(new Vector3((AlleyX0 + AlleyX1) / 2f, 2.5f, -40f));
            return res;
        }

        // ------------------------------------------------------------------ ground

        static void BuildGround(Palette pal, Transform root)
        {
            var mb = new MeshBuilder();
            float E = Extent;
            // Roads at y = 0 (avenue full length, side street split around it).
            mb.Box(pal.asphalt, new Vector3(0f, -0.1f, 0f), new Vector3(RoadHalf * 2f, 0.2f, (E + 15f) * 2f), 0b111111);
            mb.Box(pal.asphalt, new Vector3(-(E + 15f + RoadHalf) / 2f, -0.1f, CrossZ), new Vector3(E + 15f - RoadHalf, 0.2f, CrossHalf * 2f));
            mb.Box(pal.asphalt, new Vector3((E + 15f + RoadHalf) / 2f, -0.1f, CrossZ), new Vector3(E + 15f - RoadHalf, 0.2f, CrossHalf * 2f));

            // Raised blocks: curb-faced sides, sidewalk top.
            void Block(float x0, float x1, float z0, float z1) =>
                mb.Box(pal.curb, new Vector3((x0 + x1) / 2f, Curb / 2f - 0.05f, (z0 + z1) / 2f), new Vector3(x1 - x0, Curb + 0.1f, z1 - z0), 0b111111, pal.sidewalk);
            // The south-west block has the subway's two stairwells cut out of it (8c).
            foreach (var r in Without(-E - 15f, -RoadHalf, -E - 15f, CrossZ - CrossHalf, MetroBuilder.Holes)) Block(r.x, r.y, r.z, r.w);
            Block(-E - 15f, -RoadHalf, CrossZ + CrossHalf, E + 15f);
            Block(RoadHalf, E + 15f, -E - 15f, CrossZ - CrossHalf);
            Block(RoadHalf, E + 15f, CrossZ + CrossHalf, E + 15f);

            // Alley surface: cracked asphalt over the block.
            mb.Box(pal.asphalt, new Vector3((AlleyX0 + AlleyX1) / 2f, Curb + 0.01f, (-E + CrossZ - CrossHalf) / 2f), new Vector3(AlleyX1 - AlleyX0, 0.02f, E + CrossZ - CrossHalf), 0b000100);

            var go = mb.Build("Ground", root);
            go.isStatic = true;
            go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;

            // Dirt fill to the horizon, just under road level.
            var fill = new MeshBuilder();
            foreach (var r in Without(-300f, 300f, -300f, 300f, MetroBuilder.Holes))
                fill.Box(pal.dirt, new Vector3((r.x + r.y) / 2f, -0.52f, (r.z + r.w) / 2f), new Vector3(r.y - r.x, 1f, r.w - r.z), 0b000100);
            var f = fill.Build("GroundFill", root);
            f.isStatic = true;
        }

        /// <summary>
        /// The rectangle [x0, x1] × [z0, z1] minus the holes (each x0, x1, z0, z1), as rectangles: a grid on every
        /// edge, merged along Z within each column. Used to cut the subway's stairwells out of the ground.
        /// </summary>
        static List<Vector4> Without(float x0, float x1, float z0, float z1, Vector4[] holes)
        {
            var xs = new List<float> { x0, x1 };
            var zs = new List<float> { z0, z1 };
            foreach (var h in holes)
            {
                if (h.x > x0 && h.x < x1) xs.Add(h.x);
                if (h.y > x0 && h.y < x1) xs.Add(h.y);
                if (h.z > z0 && h.z < z1) zs.Add(h.z);
                if (h.w > z0 && h.w < z1) zs.Add(h.w);
            }
            xs.Sort();
            zs.Sort();
            bool InHole(float x, float z)
            {
                foreach (var h in holes) if (x > h.x && x < h.y && z > h.z && z < h.w) return true;
                return false;
            }
            var result = new List<Vector4>();
            for (int i = 0; i < xs.Count - 1; i++)
            {
                if (xs[i + 1] - xs[i] < 1e-4f) continue;
                float cx = (xs[i] + xs[i + 1]) / 2f, start = float.NaN;
                for (int j = 0; j < zs.Count - 1; j++)
                {
                    bool solid = !InHole(cx, (zs[j] + zs[j + 1]) / 2f) && zs[j + 1] - zs[j] > 1e-4f;
                    if (solid && float.IsNaN(start)) start = zs[j];
                    if (!solid && !float.IsNaN(start)) { result.Add(new Vector4(xs[i], xs[i + 1], start, zs[j])); start = float.NaN; }
                }
                if (!float.IsNaN(start)) result.Add(new Vector4(xs[i], xs[i + 1], start, zs[zs.Count - 1]));
            }
            return result;
        }

        static void BuildMarkings(Palette pal, Transform root)
        {
            var mb = new MeshBuilder();
            const float y = 0.004f;
            void Stripe(Material m, float x, float z, float sx, float sz) => mb.Box(m, new Vector3(x, y, z), new Vector3(sx, 0.008f, sz), 0b000100);

            float gap0 = CrossZ - CrossHalf - 3.5f, gap1 = CrossZ + CrossHalf + 3.5f;
            for (float z = -Extent; z < Extent; z += 0.5f)
            {
                if (z > gap0 && z < gap1) continue;
                // Double yellow centre line (segment every 0.5 m so worn gaps can be skipped).
                if (Mathf.PerlinNoise(z * 0.3f, 1.7f) > 0.18f)
                {
                    Stripe(pal.roadPaint, -0.1f, z + 0.25f, 0.1f, 0.5f);
                    Stripe(pal.roadPaint, 0.1f, z + 0.25f, 0.1f, 0.5f);
                }
            }
            for (float z = -Extent; z < Extent; z += 9f)
            {
                if (z + 3f > gap0 && z < gap1) continue;
                Stripe(pal.roadPaintWhite, -3.4f, z + 1.5f, 0.12f, 3f);
                Stripe(pal.roadPaintWhite, 3.4f, z + 1.5f, 0.12f, 3f);
            }
            // Crosswalks + stop lines.
            for (float x = -RoadHalf + 0.6f; x < RoadHalf - 0.4f; x += 1.1f)
            {
                Stripe(pal.roadPaintWhite, x, CrossZ - CrossHalf - 1.8f, 0.55f, 3f);
                Stripe(pal.roadPaintWhite, x, CrossZ + CrossHalf + 1.8f, 0.55f, 3f);
            }
            for (float z = CrossZ - CrossHalf + 0.6f; z < CrossZ + CrossHalf - 0.4f; z += 1.1f)
            {
                Stripe(pal.roadPaintWhite, -RoadHalf - 1.8f, z, 3f, 0.55f);
                Stripe(pal.roadPaintWhite, RoadHalf + 1.8f, z, 3f, 0.55f);
            }
            Stripe(pal.roadPaintWhite, -RoadHalf / 2f, CrossZ - CrossHalf - 3.8f, RoadHalf, 0.4f);
            Stripe(pal.roadPaintWhite, RoadHalf / 2f, CrossZ + CrossHalf + 3.8f, RoadHalf, 0.4f);
            mb.Build("RoadMarkings", root, false).isStatic = true;
        }

        // ------------------------------------------------------------------ buildings

        /// <summary>Lines buildings along `along` starting at `start`; facades face `normal` (= along rotated −90°).</summary>
        static void Row(Palette pal, Transform parent, Vector3 start, Vector3 normal, float length, System.Random rng,
                        float minDepth, float maxDepth, int minFloors, int maxFloors, bool alley = false)
        {
            float yaw = Mathf.Atan2(-normal.x, -normal.z) * Mathf.Rad2Deg;
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            float offset = 0f;
            while (offset < length - 0.5f)
            {
                float w = Range(rng, 8f, 15f);
                if (length - offset - w < 6f) w = length - offset;
                var spec = new BuildingSpec
                {
                    width = w,
                    depth = Range(rng, minDepth, maxDepth),
                    floors = rng.Next(minFloors, maxFloors + 1),
                    floorHeight = Range(rng, 3.0f, 3.3f),
                    ground = alley ? GroundFloor.Residential : (GroundFloor)rng.Next(3),
                    fireEscape = alley ? rng.NextDouble() < 0.8 : rng.NextDouble() < 0.4,
                    wall = pal.Pick(pal.facadeWalls, rng),
                    seed = rng.Next()
                };
                BuildingGenerator.Build(spec, pal, parent, start + along * offset, yaw);
                offset += w;
            }
        }

        // ------------------------------------------------------------------ props

        static void StreetProps(Palette pal, FireFX.Materials fx, Transform props, System.Random rng)
        {
            float y = Curb;
            float interA = CrossZ - CrossHalf - 6f, interB = CrossZ + CrossHalf + 6f;
            bool NearIntersection(float z) => z > interA && z < interB;

            // Parked cars along both curbs.
            for (int side = -1; side <= 1; side += 2)
            for (float z = -Extent + 6f; z < Extent - 6f; z += 6.4f)
            {
                if (NearIntersection(z) || rng.NextDouble() < 0.45) continue;
                bool burnt = rng.NextDouble() < 0.12;
                float yaw = (side < 0 ? -90f : 90f) + Range(rng, -3f, 3f);
                PropFactory.Sedan(pal, props, new Vector3(side * (RoadHalf - 1.25f), 0f, z + Range(rng, -0.6f, 0.6f)), yaw, pal.Pick(pal.carPaint, rng), burnt, rng);
            }

            // Wrecks left mid-road from the first hour of the Purge.
            PropFactory.Sedan(pal, props, new Vector3(-1.5f, 0f, -42f), 35f, pal.Pick(pal.carPaint, rng), false, rng);
            var burning = PropFactory.Sedan(pal, props, new Vector3(2.2f, 0f, 12f), -62f, pal.carPaint[0], true, rng);
            FireFX.Fire(fx, props, burning.transform.position + Vector3.up * 0.9f, 1.8f);
            PropFactory.Sedan(pal, props, new Vector3(0.5f, 0f, 63f), 100f, pal.Pick(pal.carPaint, rng), true, rng);

            // Street lamps.
            int lampIndex = 0;
            for (int side = -1; side <= 1; side += 2)
            for (float z = -Extent + (side < 0 ? 8f : 21f); z < Extent; z += 26f)
            {
                if (NearIntersection(z)) continue;
                PropFactory.StreetLamp(pal, props, new Vector3(side * (RoadHalf + 0.55f), y, z), side < 0 ? Vector3.right : Vector3.left,
                                       lampIndex++ % 3 == 0, rng.NextDouble() < 0.15);
            }
            for (int side = -1; side <= 1; side += 2)
            for (float x = -Extent + 12f; x < Extent; x += 26f)
            {
                if (Mathf.Abs(x) < Facade + 3f) continue;
                PropFactory.StreetLamp(pal, props, new Vector3(x, y, CrossZ + side * (CrossHalf + 0.55f)), side < 0 ? Vector3.forward : Vector3.back,
                                       lampIndex++ % 3 == 0, rng.NextDouble() < 0.15);
            }

            // Flashing signals on all four corners.
            float cx = RoadHalf + 0.6f, cz0 = CrossZ - CrossHalf - 0.6f, cz1 = CrossZ + CrossHalf + 0.6f;
            PropFactory.TrafficSignal(pal, props, new Vector3(-cx, y, cz0), Vector3.right, 5f);
            PropFactory.TrafficSignal(pal, props, new Vector3(cx, y, cz1), Vector3.left, 5f);
            PropFactory.TrafficSignal(pal, props, new Vector3(cx, y, cz0), Vector3.forward, 4.5f);
            PropFactory.TrafficSignal(pal, props, new Vector3(-cx, y, cz1), Vector3.back, 4.5f);

            // Sidewalk furniture.
            for (float z = -Extent + 15f; z < Extent; z += 38f)
            {
                if (!NearIntersection(z)) PropFactory.Hydrant(pal, props, new Vector3(-RoadHalf - 0.6f, y, z), 0f);
                if (!NearIntersection(z + 19f)) PropFactory.Hydrant(pal, props, new Vector3(RoadHalf + 0.6f, y, z + 19f), 0f);
            }
            PropFactory.Mailbox(pal, props, new Vector3(-RoadHalf - 1f, y, CrossZ - CrossHalf - 2.5f), 90f);
            PropFactory.Mailbox(pal, props, new Vector3(RoadHalf + 1f, y, CrossZ + CrossHalf + 2.5f), -90f);
            for (int i = 0; i < 16; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float z = Range(rng, -Extent + 5f, Extent - 5f);
                PropFactory.TrashCan(pal, props, new Vector3(side * Range(rng, RoadHalf + 0.8f, RoadHalf + 1.6f), y, z), rng.NextDouble() < 0.35, rng);
            }
            for (int i = 0; i < 14; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                var c = new Vector3(side * (Facade - 0.6f), y, Range(rng, -Extent + 5f, Extent - 5f));
                int n = rng.Next(3, 7);
                for (int k = 0; k < n; k++)
                    PropFactory.GarbageBag(pal, props, c + new Vector3(Range(rng, -0.5f, 0.5f), 0f, Range(rng, -1.2f, 1.2f)), rng);
            }

            // Alley clutter.
            float ax = (AlleyX0 + AlleyX1) / 2f;
            for (float z = 0f; z > -Extent + 5f; z -= Range(rng, 12f, 20f))
            {
                PropFactory.Dumpster(pal, props, new Vector3(AlleyX1 - 1.0f, y, z), 90f + Range(rng, -6f, 6f), rng);
                for (int k = 0; k < rng.Next(2, 6); k++)
                    PropFactory.GarbageBag(pal, props, new Vector3(AlleyX0 + Range(rng, 0.4f, 1.2f), y, z + Range(rng, -3f, 3f)), rng);
            }

            // The gas station's pylon (the station itself comes in phase 6c): the lit landmark Daniel heads for after the crash.
            PropFactory.GasStationSign(pal, props, new Vector3(-8.6f, y, -75f), Vector3.forward);

            // Barricade line: someone tried to close the avenue.
            for (int i = 0; i < 4; i++)
                PropFactory.JerseyBarrier(pal, props, new Vector3(-5f + i * 3.2f, 0f, 80f + (i % 2) * 0.6f), 90f + Range(rng, -12f, 12f));
            for (int i = 0; i < 6; i++)
                PropFactory.Tire(pal, props, new Vector3(Range(rng, -6f, 6f), 0f, Range(rng, -60f, 90f)), Range(rng, 0f, 360f));

            // Fires: barrels, a trash bonfire in the intersection, and the alley.
            BurningBarrel(pal, fx, props, new Vector3(-9.2f, y, -60f));
            BurningBarrel(pal, fx, props, new Vector3(9.5f, y, -18f));
            BurningBarrel(pal, fx, props, new Vector3(-9.0f, y, 52f));
            BurningBarrel(pal, fx, props, new Vector3(ax, y, -47f));
            for (int i = 0; i < 6; i++)
                PropFactory.Tire(pal, props, new Vector3(3f + Range(rng, -0.8f, 0.8f), i * 0.2f, CrossZ + Range(rng, -0.8f, 0.8f)), Range(rng, 0f, 360f));
            FireFX.Fire(fx, props, new Vector3(3f, 0.3f, CrossZ), 2.4f);

            // Manhole steam.
            foreach (var p in new[] { new Vector3(2f, 0f, -70f), new Vector3(-3f, 0f, 18f), new Vector3(-40f, 0f, CrossZ + 1f) })
            {
                var cover = new MeshBuilder();
                cover.Cylinder(pal.metalDark, Vector3.zero, 0.38f, 0.012f, 20);
                cover.Build("Manhole", props).transform.position = p;
                FireFX.Steam(fx, props, p + Vector3.up * 0.05f);
            }
        }

        /// <summary>
        /// Removes the west-row buildings that stand on the gas station lot, builds the station in the gap, and closes
        /// Harlow just south of it (barriers and a burning wreck), so the way on is through the station.
        /// </summary>
        static void GasStationLot(Palette pal, FireFX.Materials fx, Transform buildings, Transform props, int seed)
        {
            float gapMin = GasStationBuilder.LotZMin, gapMax = GasStationBuilder.LotZMax;
            var doomed = new List<GameObject>();
            foreach (Transform b in buildings)
            {
                var rs = b.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var bounds = rs[0].bounds;
                foreach (var r in rs) bounds.Encapsulate(r.bounds);
                bool westRow = bounds.center.x < -Facade && bounds.center.x > -Facade - 16f;
                if (!westRow || bounds.max.z <= GasStationBuilder.LotZMin + 0.1f || bounds.min.z >= GasStationBuilder.LotZMax - 0.1f) continue;
                gapMin = Mathf.Min(gapMin, bounds.min.z);
                gapMax = Mathf.Max(gapMax, bounds.max.z);
                doomed.Add(b.gameObject);
            }
            foreach (var go in doomed) Object.DestroyImmediate(go);

            var rng = new System.Random(seed + 303);
            GasStationBuilder.Build(pal, fx, props, gapMin, gapMax, rng);

            // Harlow closed south of the station: barriers kerb to kerb, a burnt-out car on fire, an invisible wall.
            const float z = -95f;
            for (int i = 0; i < 7; i++)
            {
                float x = -9.6f + i * 3.2f;
                PropFactory.JerseyBarrier(pal, props, new Vector3(x, Mathf.Abs(x) > RoadHalf ? Curb : 0f, z + Range(rng, -0.3f, 0.3f)), 90f + Range(rng, -8f, 8f));
            }
            var wreck = PropFactory.Sedan(pal, props, new Vector3(1.5f, 0f, z - 2.5f), 25f, pal.carPaint[0], true, rng);
            FireFX.Fire(fx, props, wreck.transform.position + Vector3.up * 0.9f, 1.4f, true, false);
            var wall = new GameObject("RoadClosed");
            wall.transform.SetParent(props, false);
            var bc = wall.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 1.5f, z);
            bc.size = new Vector3(Facade * 2f, 3f, 1f);
            wall.isStatic = true;
        }

        internal static void BurningBarrel(Palette pal, FireFX.Materials fx, Transform parent, Vector3 pos)
        {
            var mb = new MeshBuilder();
            mb.Cylinder(pal.rustyMetal, Vector3.zero, 0.29f, 0.88f, 16, false);
            mb.Cylinder(pal.burnt, new Vector3(0f, 0.7f, 0f), 0.28f, 0.01f, 16);
            var go = mb.Build("FireBarrel", parent);
            go.transform.position = pos;
            go.isStatic = true;
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.44f, 0f);
            col.radius = 0.3f;
            col.height = 0.88f;
            FireFX.Fire(fx, parent, pos + Vector3.up * 0.75f, 0.8f, true, false);
        }

        // ------------------------------------------------------------------ skyline

        static void Skyline(Palette pal, Transform root, System.Random rng)
        {
            var mb = new MeshBuilder();
            for (float a = 0f; a < 360f; a += Range(rng, 4f, 9f))
            {
                float r = Range(rng, 140f, 210f);
                float w = Range(rng, 14f, 32f), d = Range(rng, 14f, 32f);
                float h = Range(rng, 22f, 70f) * (rng.NextDouble() < 0.12 ? 2f : 1f);
                var c = Quaternion.Euler(0f, a, 0f) * Vector3.forward * r;
                // Snap so the window texture lines up with storeys.
                h = Mathf.Round(h / 3.2f) * 3.2f;
                mb.Box(pal.distantWindows, new Vector3(c.x, h / 2f, c.z), new Vector3(w, h, d), 0b110111);
            }
            var go = mb.Build("Skyline", root, false);
            go.isStatic = true;
        }

        static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
    }
}
