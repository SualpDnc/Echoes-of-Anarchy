using System.Collections.Generic;
using UnityEngine;
using Echoes.Rendering;

namespace Echoes.World
{
    /// <summary>
    /// Kessler St subway station (8c), dead in the blackout, under the back alley and the school yard. Down a stairwell
    /// in the back alley (north of Daniel's back gate) into a ticket hall with turnstiles; a platform hall along the
    /// track: the south platform, the track bed a metre lower with rails and sleepers, the north platform; a train stands
    /// at the platforms with its doors open on both sides (the way across); ramps down into the track bed at the east end;
    /// short tunnel stubs at both ends closed by rubble. From the north platform a passage (a rusted gate across it) leads
    /// to stairs that come up in the school yard. North of the entrance the back alley is closed by a burning wreck.
    /// Static with the scene (the NavMesh covers it); MetroSequence adds the hunter, the gates and the signs.
    /// The ground has holes cut for the two stairwells (DistrictGenerator.BuildGround reads Holes).
    /// </summary>
    public static class MetroBuilder
    {
        public const float FL = -5.4f, CL = -1.0f;                                   // floor and ceiling of the station
        public const float AX0 = -83.5f, AX1 = -81.3f, AZTop = -57.5f, AZBot = -48f; // entrance stairs, down northward
        public const float CX0 = -88f, CX1 = -76f, CZ0 = -48f, CZ1 = -40f, TurnZ = -43f; // ticket hall, turnstile line
        public const float HX0 = -112f, HX1 = -66f;                                  // platform hall, along X
        public const float SP0 = -40f, SP1 = -34f, TR0 = -34f, TR1 = -30.6f, NP0 = -30.6f, NP1 = -24f; // platforms and track
        public const float Bed = FL - 1.1f;
        public const float TrainX0 = -108f, TrainX1 = -88f, TrainZ0 = -33.8f, TrainZ1 = -30.8f;
        public const float EX0 = -99.2f, EX1 = -97f, GateZ = -23f, BZBot = -21.5f, BZTop = -12f; // exit passage and stairs up
        public const float BlockZ = -44f;                                            // the burning wreck across the back alley

        public static readonly float[] TrainDoors = { -104f, -98f, -92f };

        /// <summary>Openings cut in the ground for the stairwells (x0, x1, z0, z1).</summary>
        public static readonly Vector4[] Holes =
        {
            new Vector4(AX0, AX1, AZTop, AZBot),
            new Vector4(EX0, EX1, BZBot, BZTop),
        };

        public static Vector3 EntranceTop => new Vector3((AX0 + AX1) / 2f, DistrictGenerator.Curb, AZTop - 0.8f);
        public static Vector3 HallStart => new Vector3((AX0 + AX1) / 2f, FL, CZ0 + 1.5f);
        public static Vector3 ExitGate => new Vector3((EX0 + EX1) / 2f, FL, GateZ);
        public static Vector3 ExitTop => new Vector3((EX0 + EX1) / 2f, DistrictGenerator.Curb, BZTop);

        public static void Build(Palette pal, FireFX.Materials fx, Transform parent, System.Random rng)
        {
            var root = new GameObject("Metro").transform;
            root.SetParent(parent, false);
            Shell(pal, root);
            Stairs(pal, root, AX0, AX1, AZTop, AZBot, DistrictGenerator.Curb, FL, "EntranceStairs");
            Stairs(pal, root, EX0, EX1, BZTop, BZBot, DistrictGenerator.Curb, FL, "ExitStairs");
            Street(pal, root);
            Track(pal, root);
            Train(pal, root);
            Concourse(pal, root);
            Lights(root);
            Blockade(pal, fx, root, rng);

            var reverb = new GameObject("MetroReverb", typeof(AudioReverbZone));
            reverb.transform.SetParent(root, false);
            reverb.transform.position = new Vector3((HX0 + HX1) / 2f, FL + 2f, -34f);
            var rz = reverb.GetComponent<AudioReverbZone>();
            rz.reverbPreset = AudioReverbPreset.StoneCorridor;
            rz.minDistance = 22f;
            rz.maxDistance = 34f;
        }

        // ------------------------------------------------------------------ the box it all sits in

        static void Shell(Palette pal, Transform root)
        {
            var s = new Solid();
            var wall = pal.trimStone;
            var floor = pal.sidewalk;
            var ceiling = pal.curb;
            float h = CL - FL;

            // Floors: ticket hall, south and north platforms, the exit passage; the track bed is lower.
            void Floor(float x0, float x1, float z0, float z1, float top, Material m)
            {
                s.mb.Box(m, new Vector3((x0 + x1) / 2f, top - 0.15f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.3f, z1 - z0));
                s.Collider(new Vector3((x0 + x1) / 2f, top - 0.15f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.3f, z1 - z0));
            }
            Floor(CX0, CX1, CZ0, CZ1, FL, floor);
            Floor(HX0, HX1, SP0, SP1, FL, floor);
            Floor(HX0, HX1, NP0, NP1, FL, floor);
            Floor(EX0, EX1, NP1, BZBot, FL, floor);
            Floor(HX0 - 8f, HX1 + 8f, TR0, TR1, Bed, pal.asphalt);
            // The platform edges: the drop face and a yellow line.
            s.mb.Box(pal.curb, new Vector3((HX0 + HX1) / 2f, (FL + Bed) / 2f, SP1 + 0.02f), new Vector3(HX1 - HX0, FL - Bed, 0.04f));
            s.mb.Box(pal.curb, new Vector3((HX0 + HX1) / 2f, (FL + Bed) / 2f, NP0 - 0.02f), new Vector3(HX1 - HX0, FL - Bed, 0.04f));
            s.mb.Box(pal.roadPaint, new Vector3((HX0 + HX1) / 2f, FL + 0.004f, SP1 - 0.35f), new Vector3(HX1 - HX0, 0.008f, 0.25f), 0b000100);
            s.mb.Box(pal.roadPaint, new Vector3((HX0 + HX1) / 2f, FL + 0.004f, NP0 + 0.35f), new Vector3(HX1 - HX0, 0.008f, 0.25f), 0b000100);

            // Ceilings.
            void Ceil(float x0, float x1, float z0, float z1)
                => s.mb.Box(ceiling, new Vector3((x0 + x1) / 2f, CL + 0.15f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.3f, z1 - z0));
            Ceil(CX0, CX1, CZ0, CZ1);
            Ceil(HX0, HX1, SP0, NP1);
            Ceil(EX0, EX1, NP1, BZBot);

            // Walls. Ticket hall: west, east, south (with the stairs coming in).
            void WallX(float z, float x0, float x1, float y0, float y1) { s.Box(wall, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z), new Vector3(x1 - x0, y1 - y0, 0.3f)); }
            void WallZ(float x, float z0, float z1, float y0, float y1) { s.Box(wall, new Vector3(x, (y0 + y1) / 2f, (z0 + z1) / 2f), new Vector3(0.3f, y1 - y0, z1 - z0)); }
            WallZ(CX0 - 0.15f, CZ0, CZ1, FL, CL);
            WallZ(CX1 + 0.15f, CZ0, CZ1, FL, CL);
            WallX(CZ0 - 0.15f, CX0, AX0, FL, CL);
            WallX(CZ0 - 0.15f, AX1, CX1, FL, CL);
            // Platform hall, south wall (open to the ticket hall), north wall (open to the exit passage), the ends.
            WallX(SP0 - 0.15f, HX0, CX0 + 2f, FL, CL);
            WallX(SP0 - 0.15f, CX1 - 2f, HX1, FL, CL);
            WallX(SP0 - 0.15f, CX0 + 2f, CX1 - 2f, FL + 2.8f, CL);  // lintel over the wide opening
            WallX(NP1 + 0.15f, HX0, EX0, FL, CL);
            WallX(NP1 + 0.15f, EX1, HX1, FL, CL);
            WallZ(EX0 - 0.15f, NP1, BZBot, FL, CL);
            WallZ(EX1 + 0.15f, NP1, BZBot, FL, CL);
            foreach (float x in new[] { HX0 - 0.15f, HX1 + 0.15f })
            {
                WallZ(x, SP0, TR0, FL, CL);
                WallZ(x, TR1, NP1, FL, CL);
                WallZ(x, TR0, TR1, FL + 3.1f, CL);                  // over the tunnel mouth
            }
            // Tunnel stubs: walls, roof and a rubble plug 7 m in.
            foreach (float dir in new[] { -1f, 1f })
            {
                float xa = dir < 0f ? HX0 - 8f : HX1, xb = dir < 0f ? HX0 : HX1 + 8f;
                WallX(TR0 - 0.15f, xa, xb, Bed, FL + 3.1f);
                WallX(TR1 + 0.15f, xa, xb, Bed, FL + 3.1f);
                s.mb.Box(ceiling, new Vector3((xa + xb) / 2f, FL + 3.25f, (TR0 + TR1) / 2f), new Vector3(xb - xa, 0.3f, TR1 - TR0 + 0.6f));
                float px = dir < 0f ? xa + 0.6f : xb - 0.6f;
                s.Box(pal.burnt, new Vector3(px, Bed + 1.6f, (TR0 + TR1) / 2f), new Vector3(1.2f, 3.2f, TR1 - TR0));
            }
            // Pillars down the middle of both platforms.
            for (float x = HX0 + 6f; x < HX1 - 3f; x += 8f)
            {
                s.Box(wall, new Vector3(x, (FL + CL) / 2f, (SP0 + SP1) / 2f + 0.5f), new Vector3(0.6f, h, 0.6f));
                s.Box(wall, new Vector3(x, (FL + CL) / 2f, (NP0 + NP1) / 2f - 0.5f), new Vector3(0.6f, h, 0.6f));
            }
            // Benches against the walls, and torn posters.
            for (float x = HX0 + 10f; x < HX1 - 4f; x += 16f)
            {
                s.Box(pal.boards, new Vector3(x, FL + 0.23f, SP0 + 0.5f), new Vector3(1.8f, 0.46f, 0.45f));
                s.Box(pal.boards, new Vector3(x - 5f, FL + 0.23f, NP1 - 0.5f), new Vector3(1.8f, 0.46f, 0.45f)); // clear of the exit passage
                s.mb.Box(x % 3f > 1.5f ? pal.hydrantRed : pal.mailboxBlue, new Vector3(x - 3f, FL + 1.7f, SP0 + 0.01f), new Vector3(1.2f, 1.6f, 0.02f));
                s.mb.Box(pal.signBand, new Vector3(x + 1f, FL + 1.7f, NP1 - 0.01f), new Vector3(1.2f, 1.6f, 0.02f));
            }
            s.Build("MetroShell", root);
        }

        /// <summary>A straight stairwell from (z at the street) down to (z at the station): steps over a ramp, side walls, the lintel.</summary>
        static void Stairs(Palette pal, Transform root, float x0, float x1, float zTop, float zBot, float yTop, float yBot, string name)
        {
            var s = new Solid();
            int n = Mathf.RoundToInt((yTop - yBot) / 0.18f);
            float rise = (yTop - yBot) / n, run = (zBot - zTop) / n; // run is signed: the way down
            for (int i = 0; i < n; i++)
            {
                float za = zTop + i * run, zb = za + run, top = yTop - (i + 1) * rise;
                s.mb.Box(pal.trimStone, new Vector3((x0 + x1) / 2f, (yBot - 0.3f + top) / 2f, (za + zb) / 2f), new Vector3(x1 - x0, top - yBot + 0.3f, Mathf.Abs(run)), 0b111111, pal.sidewalk);
            }
            float zLo = Mathf.Min(zTop, zBot), zHi = Mathf.Max(zTop, zBot);
            foreach (float x in new[] { x0 - 0.15f, x1 + 0.15f })
                s.Box(pal.trimStone, new Vector3(x, (yBot + yTop) / 2f - 0.05f, (zLo + zHi) / 2f), new Vector3(0.3f, yTop - yBot + 0.1f, zHi - zLo));
            // Over the bottom of the stairs, the wall from the station ceiling up to the street.
            s.Box(pal.curb, new Vector3((x0 + x1) / 2f, (CL + yTop) / 2f, zBot + Mathf.Sign(zBot - zTop) * 0.15f), new Vector3(x1 - x0 + 0.6f, yTop - CL, 0.3f));
            // Handrails.
            foreach (float x in new[] { x0 + 0.05f, x1 - 0.05f })
            {
                Vector3 a = new Vector3(x, yTop + 0.9f, zTop), b = new Vector3(x, yBot + 0.9f, zBot);
                s.mb.BoxRotated(pal.galvanized, (a + b) / 2f, new Vector3(0.05f, 0.05f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
            }
            s.Build(name, root);

            Vector3 top3 = new Vector3((x0 + x1) / 2f, yTop, zTop), bot3 = new Vector3((x0 + x1) / 2f, yBot, zBot);
            var ramp = new GameObject(name + "Ramp");
            ramp.transform.SetParent(root, false);
            ramp.isStatic = true;
            var rot = Quaternion.LookRotation((bot3 - top3).normalized, Vector3.up);
            ramp.transform.SetPositionAndRotation((top3 + bot3) / 2f - rot * Vector3.up * 0.05f, rot);
            ramp.AddComponent<BoxCollider>().size = new Vector3(x1 - x0, 0.1f, Vector3.Distance(top3, bot3) + 0.2f);
        }

        // ------------------------------------------------------------------ up top

        static void Street(Palette pal, Transform root)
        {
            var s = new Solid();
            float y = DistrictGenerator.Curb;
            // Railings round both stairwells; the way in is open at the far end of each.
            void Rail(Vector3 a, Vector3 b)
            {
                Vector3 d = b - a;
                s.mb.Box(pal.galvanized, (a + b) / 2f + Vector3.up * 1f, new Vector3(Mathf.Abs(d.x) + 0.05f, 0.05f, Mathf.Abs(d.z) + 0.05f));
                for (float u = 0f; u <= 1.001f; u += 0.5f / Mathf.Max(0.5f, d.magnitude))
                    s.mb.Box(pal.galvanized, Vector3.Lerp(a, b, u) + Vector3.up * 0.5f, new Vector3(0.04f, 1f, 0.04f));
                s.Collider((a + b) / 2f + Vector3.up * 0.55f, new Vector3(Mathf.Abs(d.x) + 0.08f, 1.1f, Mathf.Abs(d.z) + 0.08f));
            }
            Rail(new Vector3(AX0 - 0.1f, y, AZTop), new Vector3(AX0 - 0.1f, y, AZBot));
            Rail(new Vector3(AX1 + 0.1f, y, AZTop), new Vector3(AX1 + 0.1f, y, AZBot));
            Rail(new Vector3(AX0 - 0.1f, y, AZBot + 0.1f), new Vector3(AX1 + 0.1f, y, AZBot + 0.1f));
            Rail(new Vector3(EX0 - 0.1f, y, BZTop), new Vector3(EX0 - 0.1f, y, BZBot));
            Rail(new Vector3(EX1 + 0.1f, y, BZTop), new Vector3(EX1 + 0.1f, y, BZBot));
            Rail(new Vector3(EX0 - 0.1f, y, BZBot - 0.1f), new Vector3(EX1 + 0.1f, y, BZBot - 0.1f));
            // The station's post at each stairwell (the name goes on it at runtime).
            foreach (var p in new[] { new Vector3(AX1 + 0.45f, y, AZTop + 0.2f), new Vector3(EX1 + 0.45f, y, BZTop - 0.2f) })
            {
                s.Box(pal.metalDark, p + Vector3.up * 1.4f, new Vector3(0.1f, 2.8f, 0.1f));
                s.mb.Box(pal.signBand, p + Vector3.up * 2.75f, new Vector3(0.5f, 0.5f, 0.06f));
            }
            s.Build("MetroStreet", root);
        }

        // ------------------------------------------------------------------ the track and the train

        static void Track(Palette pal, Transform root)
        {
            var s = new Solid();
            float x0 = HX0 - 7f, x1 = HX1 + 7f;
            for (float x = x0 + 0.3f; x < x1; x += 0.7f)
                s.mb.Box(pal.boards, new Vector3(x, Bed + 0.06f, (TR0 + TR1) / 2f), new Vector3(0.25f, 0.12f, 2.4f));
            foreach (float z in new[] { (TR0 + TR1) / 2f - 0.72f, (TR0 + TR1) / 2f + 0.72f })
                s.mb.Box(pal.galvanized, new Vector3((x0 + x1) / 2f, Bed + 0.19f, z), new Vector3(x1 - x0, 0.14f, 0.07f));
            s.mb.Box(pal.rustyMetal, new Vector3((x0 + x1) / 2f, Bed + 0.25f, TR1 - 0.35f), new Vector3(x1 - x0, 0.1f, 0.12f)); // third rail
            // Ramps down into the track bed at the east end, from both platforms: the other way across.
            float rx0 = HX1 - 5f, rx1 = HX1 - 3.5f;
            for (int side = 0; side < 2; side++)
            {
                float zEdge = side == 0 ? TR0 : TR1, zFoot = side == 0 ? TR0 + 1.4f : TR1 - 1.4f;
                for (int i = 0; i < 4; i++)
                {
                    float za = Mathf.Lerp(zEdge, zFoot, i / 4f), zb = Mathf.Lerp(zEdge, zFoot, (i + 1) / 4f), top = FL - (i + 1) * (FL - Bed) / 4f;
                    s.mb.Box(pal.galvanized, new Vector3((rx0 + rx1) / 2f, (Bed + top) / 2f, (za + zb) / 2f), new Vector3(rx1 - rx0, top - Bed, Mathf.Abs(zb - za)));
                }
                var ramp = new GameObject("TrackRamp");
                ramp.transform.SetParent(root, false);
                ramp.isStatic = true;
                Vector3 a = new Vector3((rx0 + rx1) / 2f, FL, zEdge), b = new Vector3((rx0 + rx1) / 2f, Bed, zFoot);
                var rot = Quaternion.LookRotation((b - a).normalized, Vector3.up);
                ramp.transform.SetPositionAndRotation((a + b) / 2f - rot * Vector3.up * 0.05f, rot);
                ramp.AddComponent<BoxCollider>().size = new Vector3(rx1 - rx0, 0.1f, Vector3.Distance(a, b) + 0.1f);
            }
            s.Build("MetroTrack", root);
        }

        /// <summary>
        /// The train standing at the platforms, dead: a stainless shell with dark windows, floor level with the
        /// platforms, three door pairs open on both sides (the way across the track), seats along the walls and poles.
        /// </summary>
        static void Train(Palette pal, Transform root)
        {
            var s = new Solid();
            float x0 = TrainX0, x1 = TrainX1, z0 = TrainZ0, z1 = TrainZ1, top = FL + 2.6f;
            // Undercarriage down to the track, the floor, the roof.
            s.Box(pal.metalDark, new Vector3((x0 + x1) / 2f, (Bed + FL) / 2f, (z0 + z1) / 2f), new Vector3(x1 - x0, FL - Bed - 0.02f, z1 - z0 - 0.3f));
            s.mb.Box(pal.metalDark, new Vector3((x0 + x1) / 2f, FL - 0.06f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.12f, z1 - z0), 0b111111, pal.boards);
            s.Collider(new Vector3((x0 + x1) / 2f, FL - 0.06f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.12f, z1 - z0));
            // The gaps between the platforms and the floor are covered so nobody drops into them.
            s.Collider(new Vector3((x0 + x1) / 2f, FL - 0.06f, (SP1 + z0) / 2f), new Vector3(x1 - x0, 0.12f, Mathf.Abs(z0 - SP1) + 0.1f));
            s.Collider(new Vector3((x0 + x1) / 2f, FL - 0.06f, (NP0 + z1) / 2f), new Vector3(x1 - x0, 0.12f, Mathf.Abs(NP0 - z1) + 0.1f));
            s.mb.Box(pal.galvanized, new Vector3((x0 + x1) / 2f, top + 0.08f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.16f, z1 - z0));
            // Ends.
            foreach (float x in new[] { x0, x1 }) s.Box(pal.galvanized, new Vector3(x, (FL + top) / 2f, (z0 + z1) / 2f), new Vector3(0.1f, top - FL, z1 - z0));
            // Sides with the door openings; a window band above waist height.
            foreach (float z in new[] { z0, z1 })
            {
                float c = x0;
                foreach (float dx in TrainDoors)
                {
                    SideSeg(s, pal, c, dx - 0.65f, z, top);
                    s.mb.Box(pal.galvanized, new Vector3(dx, top - 0.25f, z), new Vector3(1.3f, 0.5f, 0.06f));          // over the door
                    c = dx + 0.65f;
                }
                SideSeg(s, pal, c, x1, z, top);
            }
            // Seats along both walls between the doors, poles in the doorways.
            float prev = x0 + 0.3f;
            foreach (float dx in new List<float>(TrainDoors) { x1 + 0.65f })
            {
                float a = prev, b = dx - 0.9f;
                if (b > a + 0.5f)
                    foreach (float z in new[] { z0 + 0.35f, z1 - 0.35f })
                        s.Box(pal.hydrantRed, new Vector3((a + b) / 2f, FL + 0.23f, z), new Vector3(b - a, 0.46f, 0.45f));
                prev = dx + 0.9f;
            }
            foreach (float dx in TrainDoors)
                s.mb.Cylinder(pal.galvanized, new Vector3(dx, FL, (z0 + z1) / 2f), 0.025f, top - FL, 8, false);
            s.Build("MetroTrain", root);
        }

        static void SideSeg(Solid s, Palette pal, float a, float b, float z, float top)
        {
            if (b - a < 0.05f) return;
            s.Box(pal.galvanized, new Vector3((a + b) / 2f, FL + 0.5f, z), new Vector3(b - a, 1f, 0.06f));
            s.mb.Box(pal.glassDark, new Vector3((a + b) / 2f, FL + 1.55f, z), new Vector3(b - a, 1.1f, 0.04f));
            s.Collider(new Vector3((a + b) / 2f, FL + 1.55f, z), new Vector3(b - a, 1.1f, 0.06f));
            s.mb.Box(pal.galvanized, new Vector3((a + b) / 2f, top - 0.25f, z), new Vector3(b - a, 0.5f, 0.06f));
        }

        // ------------------------------------------------------------------ the ticket hall

        static void Concourse(Palette pal, Transform root)
        {
            var s = new Solid();
            // Turnstiles across the hall, one service gate hanging open in line with the stairs.
            float gate0 = (AX0 + AX1) / 2f - 0.5f, gate1 = gate0 + 1f;
            for (float x = CX0 + 0.4f; x < CX1 - 0.3f; x += 1.1f)
            {
                if (x + 0.3f > gate0 && x - 0.3f < gate1) continue;
                s.Box(pal.galvanized, new Vector3(x, FL + 0.5f, TurnZ), new Vector3(0.3f, 1f, 0.8f));
                s.mb.Box(pal.metalDark, new Vector3(x + 0.38f, FL + 0.85f, TurnZ), new Vector3(0.45f, 0.04f, 0.04f)); // arm
            }
            // Bars between the turnstiles so nobody walks round them.
            for (float x = CX0; x < CX1; x += 1.1f)
                if (!(x + 1.1f > gate0 && x < gate1)) s.Collider(new Vector3(x + 0.55f, FL + 0.5f, TurnZ), new Vector3(1.1f, 1f, 0.1f));
            s.mb.BoxRotated(pal.galvanized, new Vector3(gate1 + 0.1f, FL + 0.55f, TurnZ + 0.45f), new Vector3(0.04f, 0.9f, 0.9f), Quaternion.Euler(0f, 0f, 0f));
            // The ticket booth: a glass box with a counter, dark inside.
            s.Box(pal.metalDark, new Vector3(CX1 - 1.5f, FL + 0.5f, CZ0 + 2f), new Vector3(2.6f, 1f, 2.2f));
            s.mb.Box(pal.glassDark, new Vector3(CX1 - 1.5f, FL + 1.6f, CZ0 + 2f), new Vector3(2.6f, 1.2f, 2.2f));
            s.Collider(new Vector3(CX1 - 1.5f, FL + 1.6f, CZ0 + 2f), new Vector3(2.6f, 1.2f, 2.2f));
            // Ticket machines on the west wall.
            for (int i = 0; i < 3; i++) s.Box(pal.metalDark, new Vector3(CX0 + 0.35f, FL + 0.9f, CZ0 + 2f + i * 1.4f), new Vector3(0.6f, 1.8f, 1f));
            s.Build("MetroConcourse", root);
        }

        // ------------------------------------------------------------------ light

        /// <summary>Battery emergency lamps (red, dim), a dying tube in the train. Unshadowed: the flashlight is the real light here.</summary>
        static void Lights(Transform root)
        {
            void Lamp(Vector3 p, Color c, float intensity, float range, LightFlicker.Mode? flicker)
            {
                var go = new GameObject("MetroLight", typeof(Light));
                go.transform.SetParent(root, false);
                go.transform.position = p;
                var l = go.GetComponent<Light>();
                l.type = LightType.Point;
                l.color = c;
                l.intensity = intensity;
                l.range = range;
                l.shadows = LightShadows.None;
                if (flicker.HasValue)
                {
                    var f = go.AddComponent<LightFlicker>();
                    f.mode = flicker.Value;
                    f.baseIntensity = intensity;
                }
            }
            var red = new Color(1f, 0.28f, 0.2f);
            Lamp(new Vector3(CX0 + 1f, CL - 0.3f, CZ0 + 1f), red, 0.9f, 8f, null);
            Lamp(new Vector3(CX1 - 1f, CL - 0.3f, CZ1 - 1f), red, 0.9f, 8f, LightFlicker.Mode.FailingLamp);
            for (float x = HX0 + 8f; x < HX1; x += 14f)
            {
                Lamp(new Vector3(x, CL - 0.3f, SP0 + 0.6f), red, 0.8f, 7f, null);
                Lamp(new Vector3(x + 7f, CL - 0.3f, NP1 - 0.6f), red, 0.8f, 7f, null);
            }
            Lamp(new Vector3((TrainX0 + TrainX1) / 2f, FL + 2.3f, (TrainZ0 + TrainZ1) / 2f), new Color(0.75f, 0.9f, 1f), 1.3f, 9f, LightFlicker.Mode.Neon);
            Lamp(new Vector3((EX0 + EX1) / 2f, CL - 0.3f, (NP1 + BZBot) / 2f), new Color(0.6f, 1f, 0.7f), 0.7f, 5f, null); // the exit sign's glow
        }

        // ------------------------------------------------------------------ the back alley closed off

        /// <summary>A burnt-out car across the back alley north of the entrance, on fire, and a wall nobody climbs.</summary>
        static void Blockade(Palette pal, FireFX.Materials fx, Transform root, System.Random rng)
        {
            float y = DistrictGenerator.Curb, x = (AlleyBuilder.BackAlleyX0 + AlleyBuilder.BackAlleyX1) / 2f;
            var car = PropFactory.Sedan(pal, root, new Vector3(x, y, BlockZ), 0f, pal.carPaint[0], true, rng);
            FireFX.Fire(fx, root, car.transform.position + Vector3.up * 0.9f, 1.2f, true, false);
            var s = new Solid();
            s.Collider(new Vector3(x, y + 1.6f, BlockZ), new Vector3(AlleyBuilder.BackAlleyX1 - AlleyBuilder.BackAlleyX0 + 0.4f, 3.2f, 2.2f));
            s.Build("BackAlleyBlocked", root);
        }
    }
}
