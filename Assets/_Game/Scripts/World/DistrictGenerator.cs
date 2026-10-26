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

            for (float z = -85f; z <= 95f; z += 30f) res.probePoints.Add(new Vector3(0f, 2.5f, z));
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
            Block(-E - 15f, -RoadHalf, -E - 15f, CrossZ - CrossHalf);
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
            fill.Box(pal.dirt, new Vector3(0f, -0.52f, 0f), new Vector3(600f, 1f, 600f), 0b000100);
            var f = fill.Build("GroundFill", root);
            f.isStatic = true;
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

        static void BurningBarrel(Palette pal, FireFX.Materials fx, Transform parent, Vector3 pos)
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
