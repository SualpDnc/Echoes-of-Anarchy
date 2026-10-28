using UnityEngine;
using Echoes.Rendering;

namespace Echoes.World
{
    /// <summary>
    /// 8d: the Smilers' Purge party in the school yard, the static part (the NavMesh covers it). The subway's exit comes
    /// up inside a plywood hoarding in the yard's west corner (its door is PartySequence's). The crowd stands in the
    /// yard; a line of steel crowd barriers keeps a VIP strip clear along the school's facade, with one gap guarded at
    /// the stage end; the stage fills the east end (speaker stacks, the DJ table, a lighting truss, the backstage door
    /// in the wall behind it); string lights over the yard; a drinks table and a fire drum by the south wall.
    /// The pickup, the screen, the people, the music and the yard gate are PartySequence's (runtime).
    /// </summary>
    public static class PartyBuilder
    {
        // The hoarding round the top of the exit stairs (west side = the yard wall).
        public const float BoxX0 = -100f, BoxX1 = -95.6f, BoxZ0 = -13f, BoxZ1 = -8.6f;
        public const float BoxDoorZ0 = -11.6f, BoxDoorZ1 = -10.2f;
        // The VIP strip along the school, behind the barrier line; the gap in it at the stage end.
        public const float BarrierZ = -9.6f, GapX0 = -81.4f, GapX1 = -79.6f;
        // The stage at the east end.
        public const float StageX0 = -79f, StageX1 = -72.4f, StageZ0 = -18f, StageZ1 = -6.2f, StageH = 1.0f;

        public static Vector3 BoxDoor => new Vector3(BoxX1, DistrictGenerator.Curb, (BoxDoorZ0 + BoxDoorZ1) / 2f);
        public static Vector3 BackstageDoor => new Vector3(StageX1 + 0.3f, DistrictGenerator.Curb + StageH, -13f);
        public static Vector3 DjSpot => new Vector3(-74.6f, DistrictGenerator.Curb + StageH, -12f);
        public static Vector3 ScreenCentre => new Vector3(AlleyBuilder.SchoolDoor.x, DistrictGenerator.Curb + 5.2f, AlleyBuilder.YardZ1 - 0.35f);

        public static void Build(Palette pal, FireFX.Materials fx, Transform parent)
        {
            var root = new GameObject("Party").transform;
            root.SetParent(parent, false);
            float y = DistrictGenerator.Curb;
            Hoarding(pal, root, y);
            Barriers(pal, root, y);
            Stage(pal, root, y);
            Dressing(pal, fx, root, y);
        }

        static void Hoarding(Palette pal, Transform root, float y)
        {
            var s = new Solid();
            const float h = 2.4f;
            void PanelX(float z, float x0, float x1) { if (x1 - x0 > 0.01f) s.Box(pal.boards, new Vector3((x0 + x1) / 2f, y + h / 2f, z), new Vector3(x1 - x0, h, 0.06f)); }
            void PanelZ(float x, float z0, float z1) { if (z1 - z0 > 0.01f) s.Box(pal.boards, new Vector3(x, y + h / 2f, (z0 + z1) / 2f), new Vector3(0.06f, h, z1 - z0)); }
            // South side: either side of the stairwell (its railings close the gap; the stairs come up through it).
            PanelX(BoxZ0, BoxX0, MetroBuilder.EX0 - 0.15f);
            PanelX(BoxZ0, MetroBuilder.EX1 + 0.15f, BoxX1);
            PanelX(BoxZ1, BoxX0, BoxX1);
            PanelZ(BoxX1, BoxZ0, BoxDoorZ0);
            PanelZ(BoxX1, BoxDoorZ1, BoxZ1);
            // Posts at the corners and the door, a top rail.
            foreach (var p in new[] { new Vector2(BoxX1, BoxZ0), new Vector2(BoxX1, BoxZ1), new Vector2(BoxX1, BoxDoorZ0), new Vector2(BoxX1, BoxDoorZ1) })
                s.mb.Box(pal.galvanized, new Vector3(p.x, y + h / 2f + 0.05f, p.y), new Vector3(0.09f, h + 0.1f, 0.09f));
            s.Build("Hoarding", root);
        }

        /// <summary>Steel crowd barriers ("bike racks"): 2.2 m sections of frame and uprights on flat feet.</summary>
        static void Barriers(Palette pal, Transform root, float y)
        {
            var s = new Solid();
            void Run(float x0, float x1)
            {
                for (float x = x0; x < x1 - 0.05f; x += 2.2f)
                {
                    float xe = Mathf.Min(x + 2.15f, x1);
                    float cx = (x + xe) / 2f, len = xe - x;
                    foreach (float hgt in new[] { 0.15f, 1.05f })
                        s.mb.Box(pal.galvanized, new Vector3(cx, y + hgt, BarrierZ), new Vector3(len, 0.04f, 0.04f));
                    for (float u = x + 0.05f; u <= xe; u += 0.15f)
                        s.mb.Box(pal.galvanized, new Vector3(u, y + 0.6f, BarrierZ), new Vector3(0.018f, 0.9f, 0.018f));
                    foreach (float u in new[] { x + 0.1f, xe - 0.1f })
                        s.mb.Box(pal.galvanized, new Vector3(u, y + 0.02f, BarrierZ), new Vector3(0.06f, 0.04f, 0.7f));
                }
                s.Collider(new Vector3((x0 + x1) / 2f, y + 0.6f, BarrierZ), new Vector3(x1 - x0, 1.2f, 0.12f));
            }
            Run(BoxX1, GapX0);
            Run(GapX1, StageX0);
            s.Build("CrowdBarriers", root);
        }

        static void Stage(Palette pal, Transform root, float y)
        {
            var s = new Solid();
            float top = y + StageH;
            // The deck on its frame, steps down at the south end.
            s.Box(pal.metalDark, new Vector3((StageX0 + StageX1) / 2f, y + StageH / 2f, (StageZ0 + StageZ1) / 2f), new Vector3(StageX1 - StageX0, StageH, StageZ1 - StageZ0));
            s.mb.Box(pal.boards, new Vector3((StageX0 + StageX1) / 2f, top + 0.005f, (StageZ0 + StageZ1) / 2f), new Vector3(StageX1 - StageX0, 0.01f, StageZ1 - StageZ0), 0b000100);
            for (int i = 0; i < 3; i++)
            {
                float h = StageH * (i + 1) / 4f, z = StageZ0 - 0.3f * (3 - i) + 0.15f;
                s.Box(pal.metalDark, new Vector3(-74f, y + h / 2f, z), new Vector3(2.4f, h, 0.3f));
            }
            // The DJ table: a long box with the decks on it, a banner hanging off the front.
            s.Box(pal.metalDark, new Vector3(DjSpot.x - 0.8f, top + 0.45f, DjSpot.z), new Vector3(0.7f, 0.9f, 2.4f));
            s.mb.Box(pal.signBand, new Vector3(DjSpot.x - 1.16f, top + 0.45f, DjSpot.z), new Vector3(0.02f, 0.8f, 2.3f));
            foreach (float dz in new[] { -0.6f, 0.6f })
                s.mb.Cylinder(pal.galvanized, new Vector3(DjSpot.x - 0.8f, top + 0.9f, DjSpot.z + dz), 0.17f, 0.03f, 18);
            // Speaker stacks at the front corners of the deck, facing the yard.
            foreach (float z in new[] { StageZ0 + 0.8f, StageZ1 - 1.2f })
                for (int k = 0; k < 3; k++)
                {
                    var c = new Vector3(StageX0 + 0.5f, top + 0.45f + k * 0.9f, z);
                    s.Box(pal.metalDark, c, new Vector3(0.9f, 0.88f, 1.1f));
                    s.mb.Cylinder(pal.tire, c + new Vector3(-0.455f, -0.1f, 0f), 0.3f, 0.01f, 20);
                }
            // The lighting truss over the front edge of the deck.
            foreach (float z in new[] { StageZ0 + 0.2f, StageZ1 - 0.3f })
                s.Box(pal.galvanized, new Vector3(StageX0 + 0.2f, y + 3f, z), new Vector3(0.25f, 6f, 0.25f));
            s.mb.Box(pal.galvanized, new Vector3(StageX0 + 0.2f, y + 5.9f, (StageZ0 + StageZ1) / 2f), new Vector3(0.3f, 0.3f, StageZ1 - StageZ0));
            // The backstage door in the wall behind the deck (the second wave comes through it).
            s.mb.Box(pal.doorWood, new Vector3(StageX1 + 0.33f, top + 1.05f, BackstageDoor.z), new Vector3(0.05f, 2.1f, 1.1f));
            s.mb.Box(pal.lampGlass, new Vector3(StageX1 + 0.3f, top + 2.4f, BackstageDoor.z), new Vector3(0.12f, 0.12f, 0.3f));
            s.Build("Stage", root);

            // Coloured washes from the truss onto the crowd (no shadows: there are many lights here).
            Spot(root, new Vector3(StageX0 + 0.1f, y + 5.7f, StageZ0 + 2f), new Vector3(-1f, -0.7f, 0.25f), new Color(1f, 0.2f, 0.6f), 7f, 50f, 24f);
            Spot(root, new Vector3(StageX0 + 0.1f, y + 5.7f, StageZ1 - 2f), new Vector3(-1f, -0.7f, -0.3f), new Color(0.2f, 0.7f, 1f), 7f, 50f, 24f);
            var dj = new GameObject("PartyLight", typeof(Light));
            dj.transform.SetParent(root, false);
            dj.transform.position = DjSpot + new Vector3(-0.8f, 1.4f, 0f);
            var dl = dj.GetComponent<Light>();
            dl.type = LightType.Point;
            dl.color = new Color(0.6f, 0.4f, 1f);
            dl.intensity = 2f;
            dl.range = 5f;
            dl.shadows = LightShadows.None;
        }

        static void Spot(Transform root, Vector3 p, Vector3 dir, Color c, float intensity, float angle, float range)
        {
            var go = new GameObject("PartySpot", typeof(Light));
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(dir.normalized));
            var l = go.GetComponent<Light>();
            l.type = LightType.Spot;
            l.color = c;
            l.intensity = intensity;
            l.spotAngle = angle;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        static void Dressing(Palette pal, FireFX.Materials fx, Transform root, float y)
        {
            var s = new Solid();
            // String lights from the school's facade to the south wall, sagging; a warm light at each middle.
            var bulb = pal.lampGlass;
            foreach (float x in new[] { -93f, -86f, -80.5f })
            {
                Vector3 a = new Vector3(x, y + 4.3f, AlleyBuilder.YardZ1 - 0.2f), b = new Vector3(x + 1.5f, y + 3.4f, AlleyBuilder.YardZ0 + 0.3f);
                Vector3 prev = a;
                for (int i = 1; i <= 24; i++)
                {
                    float u = i / 24f;
                    Vector3 p = Vector3.Lerp(a, b, u) + Vector3.down * Mathf.Sin(u * Mathf.PI) * 0.9f;
                    s.mb.BoxRotated(pal.metalDark, (prev + p) / 2f, new Vector3(0.012f, 0.012f, Vector3.Distance(prev, p)), Quaternion.LookRotation(p - prev));
                    if (i % 2 == 0) s.mb.Box(bulb, p + Vector3.down * 0.06f, new Vector3(0.06f, 0.08f, 0.06f));
                    prev = p;
                }
                var mid = Vector3.Lerp(a, b, 0.5f) + Vector3.down * 1.1f;
                var lg = new GameObject("PartyString", typeof(Light));
                lg.transform.SetParent(root, false);
                lg.transform.position = mid;
                var l = lg.GetComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.75f, 0.45f);
                l.intensity = 1.6f;
                l.range = 9f;
                l.shadows = LightShadows.None;
            }
            // A drinks table with coolers against the south wall, empties under it.
            float tz = AlleyBuilder.YardZ0 + 0.6f;
            s.Box(pal.doorWood, new Vector3(-92.5f, y + 0.75f, tz), new Vector3(2.4f, 0.05f, 0.75f), false);
            s.Collider(new Vector3(-92.5f, y + 0.38f, tz), new Vector3(2.4f, 0.76f, 0.75f));
            foreach (float dx in new[] { -1.1f, 1.1f })
                s.mb.Box(pal.metalDark, new Vector3(-92.5f + dx, y + 0.37f, tz), new Vector3(0.05f, 0.74f, 0.7f));
            s.Box(pal.signRed, new Vector3(-94.3f, y + 0.25f, tz), new Vector3(0.7f, 0.5f, 0.45f));
            s.Box(pal.mailboxBlue, new Vector3(-90.7f, y + 0.25f, tz), new Vector3(0.7f, 0.5f, 0.45f));
            s.Build("PartyDressing", root);
            DistrictGenerator.BurningBarrel(pal, fx, root, new Vector3(-95.5f, y, -18.5f));
        }
    }
}
