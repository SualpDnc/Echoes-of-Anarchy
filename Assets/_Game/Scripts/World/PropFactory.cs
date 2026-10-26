using UnityEngine;
using UnityEngine.Rendering;
using Echoes.Rendering;
using Echoes.Audio;

namespace Echoes.World
{
    /// <summary>Street furniture and vehicles. Each prop is one mesh (plus colliders/lights where needed).</summary>
    public static class PropFactory
    {
        static readonly Color Sodium = new Color(1f, 0.6f, 0.28f);

        // ------------------------------------------------------------------ vehicles

        /// <summary>Four-door sedan, length along local X, centred on the origin at ground level.</summary>
        public static GameObject Sedan(Palette pal, Transform parent, Vector3 pos, float yaw, Material paint, bool burnt, System.Random rng)
        {
            var mb = new MeshBuilder();
            Material body = burnt ? pal.burnt : paint;
            Material glass = burnt ? pal.burnt : pal.carGlass;

            Vector2[] lower =
            {
                new Vector2(-2.35f, 0.3f), new Vector2(2.35f, 0.3f), new Vector2(2.42f, 0.6f), new Vector2(2.28f, 0.86f),
                new Vector2(1.05f, 0.99f), new Vector2(-1.72f, 1.02f), new Vector2(-2.32f, 0.96f), new Vector2(-2.44f, 0.6f)
            };
            mb.Extrude(body, body, lower, -0.9f, 0.9f);

            Vector2[] cabin = { new Vector2(1.05f, 0.98f), new Vector2(0.28f, 1.42f), new Vector2(-0.9f, 1.45f), new Vector2(-1.72f, 1.01f) };
            mb.Extrude(glass, glass, cabin, -0.76f, 0.76f);
            // Painted roof skin and pillars over the glasshouse.
            mb.Box(body, new Vector3(-0.31f, 1.455f, 0f), new Vector3(1.15f, 0.04f, 1.5f));
            mb.Box(body, new Vector3(-0.3f, 1.2f, 0.75f), new Vector3(0.12f, 0.46f, 0.04f));
            mb.Box(body, new Vector3(-0.3f, 1.2f, -0.75f), new Vector3(0.12f, 0.46f, 0.04f));

            // Lamps, bumpers, mirrors.
            if (!burnt)
            {
                mb.Box(pal.headlight, new Vector3(2.36f, 0.72f, 0.6f), new Vector3(0.1f, 0.14f, 0.38f));
                mb.Box(pal.headlight, new Vector3(2.36f, 0.72f, -0.6f), new Vector3(0.1f, 0.14f, 0.38f));
                mb.Box(pal.taillight, new Vector3(-2.38f, 0.78f, 0.62f), new Vector3(0.1f, 0.12f, 0.32f));
                mb.Box(pal.taillight, new Vector3(-2.38f, 0.78f, -0.62f), new Vector3(0.1f, 0.12f, 0.32f));
                mb.Box(pal.chrome, new Vector3(2.44f, 0.42f, 0f), new Vector3(0.08f, 0.14f, 1.7f));
                mb.Box(pal.chrome, new Vector3(-2.45f, 0.42f, 0f), new Vector3(0.08f, 0.14f, 1.7f));
            }
            mb.Box(body, new Vector3(0.95f, 1.06f, 0.98f), new Vector3(0.18f, 0.1f, 0.14f));
            mb.Box(body, new Vector3(0.95f, 1.06f, -0.98f), new Vector3(0.18f, 0.1f, 0.14f));

            // Wheels (burnt cars sit on their rims).
            float wr = burnt ? 0.26f : 0.34f;
            foreach (var w in new[] { new Vector3(1.45f, 0, 0.78f), new Vector3(1.45f, 0, -0.78f), new Vector3(-1.45f, 0, 0.78f), new Vector3(-1.45f, 0, -0.78f) })
            {
                var saved = mb.matrix;
                float side = Mathf.Sign(w.z);
                mb.matrix = saved * Matrix4x4.TRS(new Vector3(w.x, wr, w.z - side * 0.12f), Quaternion.Euler(90f * side, 0f, 0f), Vector3.one);
                mb.Cylinder(burnt ? pal.burnt : pal.tire, Vector3.zero, wr, 0.24f, 18);
                if (!burnt) mb.Cylinder(pal.chrome, new Vector3(0f, 0.241f, 0f), wr * 0.55f, 0.01f, 14);
                mb.matrix = saved;
            }
            // Dark underbody so the car doesn't look like it floats.
            mb.Box(pal.tire, new Vector3(0f, 0.28f, 0f), new Vector3(4.5f, 0.05f, 1.6f), 0b001000);

            var go = mb.Build(burnt ? "BurntSedan" : "Sedan", parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.size = new Vector3(4.85f, 1.3f, 1.85f);
            go.isStatic = true;
            return go;
        }

        // ------------------------------------------------------------------ street furniture

        /// <summary>Cobra-head street lamp. facing = direction the arm reaches (toward the road).</summary>
        public static Light StreetLamp(Palette pal, Transform parent, Vector3 pos, Vector3 facing, bool shadows, bool failing)
        {
            var root = new GameObject("StreetLamp").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(pos, Quaternion.LookRotation(facing, Vector3.up));

            var mb = new MeshBuilder();
            mb.Cylinder(pal.metalDark, Vector3.zero, 0.16f, 0.5f, 12);             // base collar
            mb.Cylinder(pal.metalDark, Vector3.zero, 0.09f, 7.6f, 12);             // pole
            mb.BoxRotated(pal.metalDark, new Vector3(0f, 7.75f, 0.55f), new Vector3(0.08f, 0.08f, 1.25f), Quaternion.Euler(-14f, 0f, 0f));
            mb.Box(pal.metalDark, new Vector3(0f, 7.9f, 1.55f), new Vector3(0.08f, 0.08f, 1.0f));
            mb.Box(pal.galvanized, new Vector3(0f, 7.92f, 2.25f), new Vector3(0.42f, 0.16f, 0.8f));  // head housing
            var housing = mb.Build("Lamp", root);
            housing.isStatic = true;
            var col = housing.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 3.8f, 0f);
            col.height = 7.6f;
            col.radius = 0.12f;

            var lensMb = new MeshBuilder();
            lensMb.Box(pal.lampGlass, new Vector3(0f, 7.83f, 2.25f), new Vector3(0.34f, 0.04f, 0.66f), 0b001000);
            var lens = lensMb.Build("Lens", root, false);

            var lightGo = new GameObject("SodiumLight", typeof(Light));
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(0f, 7.7f, 2.25f);
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var l = lightGo.GetComponent<Light>();
            l.type = LightType.Spot;
            l.color = Sodium;
            l.intensity = 14f;
            l.range = 22f;
            l.spotAngle = 130f;
            l.innerSpotAngle = 55f;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowNearPlane = 0.4f;
            if (failing)
            {
                var f = lightGo.AddComponent<LightFlicker>();
                f.mode = LightFlicker.Mode.FailingLamp;
                f.baseIntensity = l.intensity;
                f.emissiveRenderer = lens.GetComponent<Renderer>();
                f.emissiveColor = Sodium * 8f;
                ProceduralAudioSource.Attach(lightGo, SoundId.LampBuzz, 0.35f, 1f, 10f);
            }
            return l;
        }

        public static GameObject Hydrant(Palette pal, Transform parent, Vector3 pos, float yaw)
        {
            var mb = new MeshBuilder();
            mb.Cylinder(pal.hydrantRed, Vector3.zero, 0.15f, 0.08f, 12);
            mb.Cylinder(pal.hydrantRed, new Vector3(0, 0.08f, 0), 0.11f, 0.5f, 12);
            mb.Cylinder(pal.hydrantRed, new Vector3(0, 0.58f, 0), 0.13f, 0.06f, 12);
            mb.Cylinder(pal.hydrantRed, new Vector3(0, 0.64f, 0), 0.08f, 0.12f, 10);
            mb.Box(pal.hydrantRed, new Vector3(0.15f, 0.4f, 0f), new Vector3(0.12f, 0.1f, 0.1f));
            mb.Box(pal.hydrantRed, new Vector3(-0.15f, 0.4f, 0f), new Vector3(0.12f, 0.1f, 0.1f));
            mb.Box(pal.hydrantRed, new Vector3(0f, 0.35f, -0.15f), new Vector3(0.14f, 0.14f, 0.12f));
            return Finish(mb, "Hydrant", parent, pos, yaw, new Vector3(0f, 0.38f, 0f), new Vector3(0.34f, 0.76f, 0.34f));
        }

        public static GameObject Mailbox(Palette pal, Transform parent, Vector3 pos, float yaw)
        {
            var mb = new MeshBuilder();
            foreach (var x in new[] { -0.22f, 0.22f })
            foreach (var z in new[] { -0.2f, 0.2f })
                mb.Box(pal.mailboxBlue, new Vector3(x, 0.15f, z), new Vector3(0.05f, 0.3f, 0.05f));
            mb.Box(pal.mailboxBlue, new Vector3(0f, 0.75f, 0f), new Vector3(0.52f, 0.9f, 0.5f));
            var saved = mb.matrix;
            mb.matrix = saved * Matrix4x4.TRS(new Vector3(0f, 1.2f, -0.25f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1f, 1f, 0.55f));
            mb.Cylinder(pal.mailboxBlue, Vector3.zero, 0.26f, 0.5f, 16);
            mb.matrix = saved;
            mb.Box(pal.metalDark, new Vector3(0f, 1.0f, -0.255f), new Vector3(0.36f, 0.06f, 0.02f)); // letter slot
            return Finish(mb, "Mailbox", parent, pos, yaw, new Vector3(0f, 0.7f, 0f), new Vector3(0.52f, 1.4f, 0.5f));
        }

        public static GameObject TrashCan(Palette pal, Transform parent, Vector3 pos, bool tipped, System.Random rng)
        {
            var mb = new MeshBuilder();
            mb.Cylinder(pal.galvanized, Vector3.zero, 0.28f, 0.9f, 16, true);
            mb.Cylinder(pal.galvanized, new Vector3(0, 0.88f, 0), 0.3f, 0.04f, 16);
            float yaw = (float)rng.NextDouble() * 360f;
            var go = Finish(mb, "TrashCan", parent, pos, yaw, new Vector3(0, 0.45f, 0), new Vector3(0.56f, 0.9f, 0.56f));
            if (tipped)
            {
                go.transform.position = pos + Vector3.up * 0.28f;
                go.transform.rotation = Quaternion.Euler(0f, yaw, 90f);
            }
            return go;
        }

        public static GameObject Dumpster(Palette pal, Transform parent, Vector3 pos, float yaw, System.Random rng)
        {
            var mb = new MeshBuilder();
            mb.Box(pal.dumpsterGreen, new Vector3(0f, 0.75f, 0f), new Vector3(1.9f, 1.2f, 1.2f));
            mb.Box(pal.dumpsterGreen, new Vector3(0f, 0.75f, -0.62f), new Vector3(1.95f, 0.1f, 0.06f)); // rib
            float lid = rng.NextDouble() < 0.4 ? -55f : -3f;
            mb.BoxRotated(pal.tire, new Vector3(0f, 1.38f + (lid < -10f ? 0.35f : 0f), lid < -10f ? 0.75f : 0f), new Vector3(1.9f, 0.04f, 1.2f), Quaternion.Euler(lid < -10f ? -lid : 0f, 0f, 0f));
            foreach (var x in new[] { -0.8f, 0.8f })
            foreach (var z in new[] { -0.45f, 0.45f })
                mb.Cylinder(pal.tire, new Vector3(x, 0f, z), 0.07f, 0.15f, 8);
            return Finish(mb, "Dumpster", parent, pos, yaw, new Vector3(0f, 0.75f, 0f), new Vector3(1.9f, 1.3f, 1.2f));
        }

        public static GameObject JerseyBarrier(Palette pal, Transform parent, Vector3 pos, float yaw)
        {
            var mb = new MeshBuilder();
            Vector2[] p = { new Vector2(-0.3f, 0f), new Vector2(0.3f, 0f), new Vector2(0.3f, 0.08f), new Vector2(0.18f, 0.3f), new Vector2(0.1f, 0.81f), new Vector2(-0.1f, 0.81f), new Vector2(-0.18f, 0.3f), new Vector2(-0.3f, 0.08f) };
            mb.Extrude(pal.curb, pal.curb, p, -1.5f, 1.5f);
            return Finish(mb, "JerseyBarrier", parent, pos, yaw, new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.81f, 3f));
        }

        public static GameObject Tire(Palette pal, Transform parent, Vector3 pos, float yaw)
        {
            var mb = new MeshBuilder();
            mb.Cylinder(pal.tire, Vector3.zero, 0.34f, 0.22f, 18);
            return Finish(mb, "Tire", parent, pos, yaw, new Vector3(0f, 0.11f, 0f), new Vector3(0.68f, 0.22f, 0.68f));
        }

        public static GameObject GarbageBag(Palette pal, Transform parent, Vector3 pos, System.Random rng)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "GarbageBag";
            go.transform.SetParent(parent, false);
            float s = 0.45f + (float)rng.NextDouble() * 0.25f;
            go.transform.position = pos + Vector3.up * s * 0.3f;
            go.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 20f);
            go.transform.localScale = new Vector3(s, s * 0.75f, s * 0.9f);
            go.GetComponent<Renderer>().sharedMaterial = pal.tire;
            go.isStatic = true;
            return go;
        }

        /// <summary>Span-wire style signal head hung from a mast arm, flashing amber (signals are on flash during the Purge).</summary>
        public static void TrafficSignal(Palette pal, Transform parent, Vector3 pos, Vector3 armDir, float armLength)
        {
            var root = new GameObject("TrafficSignal").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(pos, Quaternion.LookRotation(armDir, Vector3.up));

            var mb = new MeshBuilder();
            mb.Cylinder(pal.signalHousing, Vector3.zero, 0.14f, 6.2f, 12);
            mb.Box(pal.signalHousing, new Vector3(0f, 6f, armLength / 2f), new Vector3(0.12f, 0.12f, armLength));
            var headPos = new Vector3(0f, 5.2f, armLength - 0.3f);
            mb.Box(pal.signalHousing, headPos, new Vector3(0.36f, 1.05f, 0.3f));
            mb.Box(pal.signalHousing, headPos + new Vector3(0f, 0.6f, 0f), new Vector3(0.04f, 0.4f, 0.04f));
            // Visors over each lens (lenses face the arm's left side: approaching traffic).
            for (int i = 0; i < 3; i++)
                mb.Box(pal.signalHousing, headPos + new Vector3(-0.2f, 0.33f - i * 0.33f + 0.12f, 0f), new Vector3(0.12f, 0.02f, 0.26f));
            var pole = mb.Build("Signal", root);
            pole.isStatic = true;
            var col = pole.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 3.1f, 0f);
            col.height = 6.2f;
            col.radius = 0.15f;

            for (int i = 0; i < 3; i++)
            {
                bool amber = i == 1;
                var lmb = new MeshBuilder();
                var saved = lmb.matrix;
                lmb.matrix = Matrix4x4.TRS(headPos + new Vector3(-0.181f, 0.33f - i * 0.33f, 0f), Quaternion.Euler(0f, 0f, 90f), Vector3.one);
                lmb.Cylinder(amber ? pal.signalAmber : pal.signalOff, Vector3.zero, 0.11f, 0.01f, 14);
                lmb.matrix = saved;
                var lensGo = lmb.Build(amber ? "Lens_Amber" : "Lens_Off", root, false);
                if (!amber) continue;

                var lightGo = new GameObject("SignalLight", typeof(Light));
                lightGo.transform.SetParent(root, false);
                lightGo.transform.localPosition = headPos + new Vector3(-0.5f, 0f, 0f);
                var l = lightGo.GetComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.62f, 0.1f);
                l.range = 7f;
                l.shadows = LightShadows.None;
                var f = lightGo.AddComponent<LightFlicker>();
                f.mode = LightFlicker.Mode.Blink;
                f.baseIntensity = 2.2f;
                f.emissiveRenderer = lensGo.GetComponent<Renderer>();
                f.emissiveColor = new Color(1f, 0.55f, 0.08f) * 12f;
                ProceduralAudioSource.Attach(lightGo, SoundId.SignalTick, 0.3f, 1f, 9f, true);
            }
        }

        static GameObject Finish(MeshBuilder mb, string name, Transform parent, Vector3 pos, float yaw, Vector3 colCenter, Vector3 colSize)
        {
            var go = mb.Build(name, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var col = go.AddComponent<BoxCollider>();
            col.center = colCenter;
            col.size = colSize;
            go.isStatic = true;
            return go;
        }
    }
}
