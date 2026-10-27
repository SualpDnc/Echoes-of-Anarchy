using UnityEngine;
using Echoes.Rendering;

namespace Echoes.World
{
    /// <summary>
    /// Scripted story vehicles, built at runtime: Medic 14 (a box ambulance with a full cab interior the player rides
    /// in) and the Smilers' ram truck. Frame: origin on the ground between the axles, +Z forward, +X to the right.
    /// </summary>
    public static class VehicleFactory
    {
        static Material L(float r, float g, float b, float smooth, float metal = 0f) => RuntimeMaterials.Lit(new Color(r, g, b), smooth, metal);

        // ------------------------------------------------------------------ ambulance

        public static VehicleRig Ambulance(Transform parent)
        {
            var root = new GameObject("Ambulance_Medic14");
            root.transform.SetParent(parent, false);
            var rig = root.AddComponent<VehicleRig>();
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            rig.body = body;

            var white = L(0.8f, 0.8f, 0.78f, 0.55f);
            var red = L(0.5f, 0.035f, 0.03f, 0.5f);
            var dark = L(0.03f, 0.03f, 0.032f, 0.3f);
            var trim = L(0.5f, 0.5f, 0.5f, 0.7f, 1f);
            var plastic = L(0.075f, 0.075f, 0.08f, 0.25f);
            var headliner = L(0.22f, 0.22f, 0.21f, 0.15f);
            var floor = L(0.035f, 0.035f, 0.035f, 0.2f);
            var seat = RuntimeMaterials.Fabric(new Color(0.05f, 0.055f, 0.07f), 0.15f, Fabric.Cotton);
            var cabinet = L(0.55f, 0.57f, 0.55f, 0.35f);
            var darkGlass = L(0.01f, 0.012f, 0.015f, 0.92f);

            const float W = 1.05f;            // cab half width
            var mb = new MeshBuilder();

            // --- Chassis and cab shell (outside).
            mb.Box(dark, new Vector3(0f, 0.62f, -0.2f), new Vector3(1.9f, 0.24f, 6.9f));
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * W;
                Vector3 o = Vector3.right * s;
                mb.Polygon(white, o, false, P(x, 1.35f, 0.62f), P(x, 3.32f, 0.62f), P(x, 3.32f, 1.3f), P(x, 2.55f, 1.45f), P(x, 1.35f, 1.45f));
                SideFrame(mb, white, x, o);
                mb.Box(red, new Vector3(x + s * 0.005f, 1.08f, 2.3f), new Vector3(0.01f, 0.2f, 1.9f), s < 0 ? 0b000010 : 0b000001);
                // Door handle and mirror (seen from inside through the side windows).
                mb.Box(trim, new Vector3(x + s * 0.02f, 1.38f, 1.65f), new Vector3(0.03f, 0.04f, 0.16f));
                mb.Box(dark, new Vector3(s * 1.13f, 1.72f, 2.42f), new Vector3(0.16f, 0.03f, 0.03f));
                mb.Box(dark, new Vector3(s * 1.23f, 1.75f, 2.42f), new Vector3(0.05f, 0.3f, 0.2f));
                mb.Box(trim, new Vector3(s * 1.23f - s * 0.026f, 1.75f, 2.42f), new Vector3(0.005f, 0.26f, 0.17f));
            }
            mb.Polygon(white, Vector3.forward, false, new Vector3(-W, 0.62f, 3.32f), new Vector3(W, 0.62f, 3.32f), new Vector3(W, 1.3f, 3.32f), new Vector3(-W, 1.3f, 3.32f));
            mb.Polygon(white, new Vector3(0f, 1f, 0.19f), false, new Vector3(-W, 1.45f, 2.55f), new Vector3(W, 1.45f, 2.55f), new Vector3(W, 1.3f, 3.32f), new Vector3(-W, 1.3f, 3.32f));
            mb.Polygon(white, Vector3.up, false, new Vector3(-W, 2.25f, 1.35f), new Vector3(W, 2.25f, 1.35f), new Vector3(W, 2.25f, 2.2f), new Vector3(-W, 2.25f, 2.2f));
            // Windscreen frame (pillars, header, cowl) on the screen's plane, visible from both sides.
            Vector3 wsN = new Vector3(0f, 0.35f, 0.8f);
            for (int s = -1; s <= 1; s += 2)
                mb.Polygon(dark, wsN, true, new Vector3(s * 0.96f, 1.45f, 2.555f), new Vector3(s * W, 1.45f, 2.555f), new Vector3(s * W, 2.25f, 2.205f), new Vector3(s * 0.96f, 2.25f, 2.205f));
            mb.Polygon(dark, wsN, true, new Vector3(-W, 2.19f, 2.231f), new Vector3(W, 2.19f, 2.231f), new Vector3(W, 2.25f, 2.205f), new Vector3(-W, 2.25f, 2.205f));
            mb.Polygon(dark, wsN, true, new Vector3(-W, 1.45f, 2.555f), new Vector3(W, 1.45f, 2.555f), new Vector3(W, 1.5f, 2.533f), new Vector3(-W, 1.5f, 2.533f));
            // Grille, bumper, wipers.
            mb.Box(dark, new Vector3(0f, 0.98f, 3.33f), new Vector3(1.2f, 0.4f, 0.02f));
            mb.Box(trim, new Vector3(0f, 1.2f, 3.335f), new Vector3(1.2f, 0.03f, 0.02f));
            mb.Box(dark, new Vector3(0f, 0.55f, 3.4f), new Vector3(2.15f, 0.22f, 0.18f));
            mb.BoxRotated(dark, new Vector3(-0.4f, 1.52f, 2.53f), new Vector3(0.65f, 0.015f, 0.02f), Quaternion.Euler(0f, 0f, 8f));
            mb.BoxRotated(dark, new Vector3(0.3f, 1.52f, 2.53f), new Vector3(0.65f, 0.015f, 0.02f), Quaternion.Euler(0f, 0f, 8f));

            // --- Cab interior (inward-facing).
            const float Wi = 1.0f;
            mb.Polygon(floor, Vector3.up, false, new Vector3(-Wi, 0.86f, 1.38f), new Vector3(Wi, 0.86f, 1.38f), new Vector3(Wi, 0.86f, 2.4f), new Vector3(-Wi, 0.86f, 2.4f));
            mb.Polygon(headliner, Vector3.down, false, new Vector3(-Wi, 2.2f, 1.38f), new Vector3(Wi, 2.2f, 1.38f), new Vector3(Wi, 2.2f, 2.22f), new Vector3(-Wi, 2.2f, 2.22f));
            mb.Polygon(plastic, Vector3.forward, false, new Vector3(-Wi, 0.86f, 1.38f), new Vector3(Wi, 0.86f, 1.38f), new Vector3(Wi, 2.2f, 1.38f), new Vector3(-Wi, 2.2f, 1.38f));
            mb.Box(darkGlass, new Vector3(0f, 1.78f, 1.386f), new Vector3(0.7f, 0.32f, 0.01f), 0b010000); // pass-through to the patient compartment
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * Wi;
                Vector3 i = Vector3.left * s;
                mb.Polygon(plastic, i, false, P(x, 1.38f, 0.86f), P(x, 2.42f, 0.86f), P(x, 2.42f, 1.48f), P(x, 1.38f, 1.48f));
                SideFrame(mb, plastic, s * (W - 0.035f), i);
                mb.Box(plastic, new Vector3(s * 0.95f, 1.3f, 1.9f), new Vector3(0.1f, 0.06f, 0.55f)); // armrest
                mb.Box(plastic, new Vector3(s * 1.025f, 1.495f, 1.93f), new Vector3(0.07f, 0.03f, 1.15f)); // window sill
            }
            // Dashboard, gauges, radio, the rest of the cockpit.
            mb.Box(plastic, new Vector3(0f, 1.16f, 2.42f), new Vector3(2.0f, 0.6f, 0.3f));
            mb.Box(dark, new Vector3(0f, 1.47f, 2.47f), new Vector3(2.0f, 0.03f, 0.2f));
            mb.Box(dark, new Vector3(-0.5f, 1.5f, 2.33f), new Vector3(0.5f, 0.09f, 0.14f));
            mb.Box(dark, new Vector3(0f, 1.3f, 2.265f), new Vector3(0.3f, 0.1f, 0.02f));
            mb.Box(plastic, new Vector3(0f, 1.05f, 1.95f), new Vector3(0.34f, 0.38f, 0.62f)); // console
            mb.Box(dark, new Vector3(0.05f, 1.25f, 1.75f), new Vector3(0.12f, 0.04f, 0.18f));  // radio handset
            mb.BoxRotated(dark, new Vector3(-0.5f, 1.38f, 2.17f), new Vector3(0.06f, 0.06f, 0.28f), Quaternion.Euler(-37f, 0f, 0f)); // column
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * 0.5f;
                mb.Box(dark, new Vector3(x, 1.0f, 1.75f), new Vector3(0.45f, 0.28f, 0.45f));
                mb.Box(seat, new Vector3(x, 1.19f, 1.78f), new Vector3(0.52f, 0.12f, 0.52f));
                mb.BoxRotated(seat, new Vector3(x, 1.58f, 1.5f), new Vector3(0.52f, 0.7f, 0.12f), Quaternion.Euler(-10f, 0f, 0f));
                mb.Box(seat, new Vector3(x, 2.0f, 1.44f), new Vector3(0.3f, 0.17f, 0.1f));
            }

            // --- Patient compartment (box module), open at the back for the doors.
            mb.Box(white, new Vector3(0f, 1.9f, -1.025f), new Vector3(2.4f, 2.2f, 4.75f), 0b011111);
            mb.Box(red, new Vector3(0f, 1.48f, -1.025f), new Vector3(2.42f, 0.24f, 4.77f), 0b010011);
            mb.Box(red, new Vector3(0f, 2.75f, -1.025f), new Vector3(2.42f, 0.06f, 4.77f), 0b010011);
            mb.Box(dark, new Vector3(0f, 0.62f, -3.5f), new Vector3(2.2f, 0.16f, 0.3f));       // rear step
            mb.Box(dark, new Vector3(0f, 3.06f, 1.15f), new Vector3(2.0f, 0.12f, 0.25f));      // light bar housing
            mb.Box(dark, new Vector3(0f, 2.29f, 1.75f), new Vector3(1.3f, 0.07f, 0.22f));      // cab light bar
            // Inside the module: light grey walls, cabinets, the stretcher.
            const float Mx = 1.17f, My0 = 0.83f, My1 = 2.97f, Mz0 = -3.38f, Mz1 = 1.32f;
            mb.Polygon(cabinet, Vector3.left, false, new Vector3(Mx, My0, Mz0), new Vector3(Mx, My0, Mz1), new Vector3(Mx, My1, Mz1), new Vector3(Mx, My1, Mz0));
            mb.Polygon(cabinet, Vector3.right, false, new Vector3(-Mx, My0, Mz0), new Vector3(-Mx, My0, Mz1), new Vector3(-Mx, My1, Mz1), new Vector3(-Mx, My1, Mz0));
            mb.Polygon(cabinet, Vector3.down, false, new Vector3(-Mx, My1, Mz0), new Vector3(Mx, My1, Mz0), new Vector3(Mx, My1, Mz1), new Vector3(-Mx, My1, Mz1));
            mb.Polygon(floor, Vector3.up, false, new Vector3(-Mx, My0, Mz0), new Vector3(Mx, My0, Mz0), new Vector3(Mx, My0, Mz1), new Vector3(-Mx, My0, Mz1));
            mb.Polygon(cabinet, Vector3.back, false, new Vector3(-Mx, My0, Mz1), new Vector3(Mx, My0, Mz1), new Vector3(Mx, My1, Mz1), new Vector3(-Mx, My1, Mz1));
            mb.Box(cabinet, new Vector3(-0.95f, 1.9f, -1.0f), new Vector3(0.4f, 1.6f, 3.6f));
            mb.Box(dark, new Vector3(-0.74f, 2.1f, -1.0f), new Vector3(0.01f, 0.02f, 3.5f));
            mb.Box(L(0.6f, 0.62f, 0.6f, 0.4f), new Vector3(0.45f, 1.25f, -1.4f), new Vector3(0.6f, 0.18f, 2.0f));
            mb.Build("Shell", body);

            // --- Glass: windscreen separate (it goes in the crash), side and rear windows.
            var glass = RuntimeMaterials.Glass(new Color(0.3f, 0.36f, 0.4f), 0.16f);
            var ws = new MeshBuilder();
            ws.Polygon(glass, wsN, true, new Vector3(-0.96f, 1.5f, 2.533f), new Vector3(0.96f, 1.5f, 2.533f), new Vector3(0.96f, 2.19f, 2.231f), new Vector3(-0.96f, 2.19f, 2.231f));
            rig.windshield = ws.Build("Windshield", body, false);
            var side = new MeshBuilder();
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * (W - 0.015f);
                side.Polygon(glass, Vector3.right * s, true, P(x, 1.5f, 1.52f), P(x, 2.419f, 1.52f), P(x, 2.1525f, 2.13f), P(x, 1.5f, 2.13f));
            }
            side.Build("SideWindows", body, false);

            // --- Rear doors, hinged at the outer edges.
            rig.rearDoorL = RearDoor(body, -1, white, red, darkGlass, trim);
            rig.rearDoorR = RearDoor(body, 1, white, red, darkGlass, trim);

            // --- Lights.
            var lensMat = RuntimeMaterials.Lit(new Color(0.9f, 0.9f, 0.88f), 0.9f, 0f, Color.black);
            rig.headlightLenses = new[]
            {
                Lens(body, "HeadlightL", new Vector3(-0.78f, 1.0f, 3.335f), new Vector3(0.36f, 0.2f, 0.02f), lensMat),
                Lens(body, "HeadlightR", new Vector3(0.78f, 1.0f, 3.335f), new Vector3(0.36f, 0.2f, 0.02f), lensMat)
            };
            rig.headlights = new[]
            {
                Spot(body, "HeadlightL", new Vector3(-0.78f, 1.0f, 3.4f), Quaternion.Euler(6f, -2f, 0f), new Color(1f, 0.94f, 0.82f), 9f, 38f, 70f),
                Spot(body, "HeadlightR", new Vector3(0.78f, 1.0f, 3.4f), Quaternion.Euler(6f, 2f, 0f), new Color(1f, 0.94f, 0.82f), 9f, 38f, 70f)
            };
            var tail = RuntimeMaterials.Lit(new Color(0.5f, 0.03f, 0.02f), 0.8f, 0f, new Color(1f, 0.05f, 0.02f) * 2.5f);
            var amber = RuntimeMaterials.Lit(new Color(0.6f, 0.35f, 0.05f), 0.8f, 0f, new Color(1f, 0.5f, 0.05f) * 2.5f);
            var markers = new MeshBuilder();
            for (int s = -1; s <= 1; s += 2)
            {
                markers.Box(tail, new Vector3(s * 1.0f, 0.95f, -3.47f), new Vector3(0.2f, 0.25f, 0.05f));
                markers.Box(amber, new Vector3(s * 1.205f, 2.9f, 1.2f), new Vector3(0.02f, 0.06f, 0.12f));
                markers.Box(tail, new Vector3(s * 1.205f, 2.9f, -3.25f), new Vector3(0.02f, 0.06f, 0.12f));
            }
            markers.Build("MarkerLights", body, false);

            var barLens = RuntimeMaterials.Lit(new Color(0.5f, 0.05f, 0.04f), 0.9f, 0f, Color.black);
            var barWhite = RuntimeMaterials.Lit(new Color(0.85f, 0.85f, 0.85f), 0.9f, 0f, Color.black);
            var bar = body.gameObject.AddComponent<LightBar>();
            bar.lensesA = new[]
            {
                Lens(body, "BarL", new Vector3(-0.7f, 3.06f, 1.28f), new Vector3(0.5f, 0.09f, 0.02f), barLens),
                Lens(body, "CabBarL", new Vector3(-0.4f, 2.3f, 1.865f), new Vector3(0.4f, 0.05f, 0.02f), barLens),
                Lens(body, "RearL", new Vector3(-1.0f, 2.9f, -3.405f), new Vector3(0.25f, 0.12f, 0.02f), barLens)
            };
            bar.lensesB = new[]
            {
                Lens(body, "BarR", new Vector3(0.7f, 3.06f, 1.28f), new Vector3(0.5f, 0.09f, 0.02f), barLens),
                Lens(body, "CabBarR", new Vector3(0.4f, 2.3f, 1.865f), new Vector3(0.4f, 0.05f, 0.02f), barLens),
                Lens(body, "RearR", new Vector3(1.0f, 2.9f, -3.405f), new Vector3(0.25f, 0.12f, 0.02f), barLens),
                Lens(body, "BarC", new Vector3(0f, 3.06f, 1.28f), new Vector3(0.3f, 0.09f, 0.02f), barWhite)
            };
            bar.lightsA = new[] { Point(body, "BarLightA", new Vector3(-0.8f, 3.1f, 1.6f), new Color(1f, 0.08f, 0.05f), 16f) };
            bar.lightsB = new[] { Point(body, "BarLightB", new Vector3(0.8f, 3.1f, 1.6f), new Color(1f, 0.08f, 0.05f), 16f) };
            rig.lightBar = bar;

            // Cockpit glow: the radio display and the MDT screen light the cab a little.
            var radio = RuntimeMaterials.Lit(Color.black, 0.9f, 0f, new Color(0.3f, 1f, 0.5f) * 1.6f);
            var gauges = RuntimeMaterials.Lit(Color.black, 0.9f, 0f, new Color(1f, 0.55f, 0.15f) * 1.2f);
            var screen = RuntimeMaterials.Lit(Color.black, 0.9f, 0f, new Color(0.25f, 0.5f, 1f) * 1.4f);
            var glow = new MeshBuilder();
            glow.Box(radio, new Vector3(0f, 1.3f, 2.254f), new Vector3(0.16f, 0.035f, 0.004f));
            glow.Box(gauges, new Vector3(-0.5f, 1.42f, 2.262f), new Vector3(0.36f, 0.09f, 0.004f));
            Vector3 mdt = new Vector3(0.14f, 1.46f, 2.12f);
            Quaternion mdtRot = Quaternion.LookRotation(mdt - new Vector3(0.5f, 1.66f, 1.62f));
            glow.BoxRotated(dark, mdt, new Vector3(0.32f, 0.22f, 0.02f), mdtRot);
            glow.BoxRotated(screen, mdt + mdtRot * new Vector3(0f, 0f, -0.012f), new Vector3(0.28f, 0.18f, 0.003f), mdtRot);
            glow.Build("Cockpit", body, false);
            // Between the seats at face height, so Luis's face reads in the dark cab (owner: "no face" with the old dim glow).
            rig.interiorLight = Point(body, "CabGlow", new Vector3(-0.05f, 1.72f, 2.0f), new Color(0.72f, 0.8f, 1f), 2.1f);
            rig.interiorLight.intensity = 0.9f;

            // Steering wheel on its own pivot (the hub, tilted toward the driver) so it can turn; grips at ten and two.
            var sw = Empty(body, "SteeringWheel", new Vector3(-0.5f, 1.5f, 2.03f));
            sw.localRotation = Quaternion.Euler(-37f, 0f, 0f);
            var swMesh = new MeshBuilder();
            SteeringWheel(swMesh, dark, Vector3.zero, Quaternion.identity);
            swMesh.Build("Wheel", sw);
            rig.steeringWheel = sw;
            rig.gripL = Empty(sw, "GripL", new Vector3(-0.2f, 0.1f, -0.05f));
            rig.gripR = Empty(sw, "GripR", new Vector3(0.2f, 0.1f, -0.05f));
            rig.compartmentLight = Point(body, "CompartmentLight", new Vector3(0f, 2.7f, -1.6f), new Color(0.9f, 0.95f, 1f), 3.8f);
            rig.compartmentLight.intensity = 0.9f;
            rig.compartmentLight.enabled = false;

            // --- Seats: Daniel's body root in the passenger seat. The camera pivot sits 1.46 m above it: at 0.2 it
            // landed at Luis's chest and the owner felt like a dwarf next to him; at 0.46 it is a hand under his eyes (2.0),
            // about where a seated man's eyes are (the roof liner is at 2.25).
            rig.passengerMount = Empty(body, "PassengerMount", new Vector3(0.5f, 0.46f, 1.62f));
            rig.driverSeat = Empty(body, "DriverSeat", new Vector3(-0.5f, 1.28f, 1.72f));

            // --- Wheels (not part of the sprung body).
            rig.wheelRadius = 0.4f;
            rig.wheels = new[]
            {
                Wheel(root.transform, new Vector3(-0.97f, 0.4f, 2.55f), -1, 0.4f), Wheel(root.transform, new Vector3(0.97f, 0.4f, 2.55f), 1, 0.4f),
                Wheel(root.transform, new Vector3(-1.02f, 0.4f, -2.05f), -1, 0.4f), Wheel(root.transform, new Vector3(1.02f, 0.4f, -2.05f), 1, 0.4f)
            };

            var cab = root.AddComponent<BoxCollider>();
            cab.center = new Vector3(0f, 1.45f, 2.35f);
            cab.size = new Vector3(2.1f, 1.7f, 2.0f);
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.9f, -1.025f);
            box.size = new Vector3(2.4f, 2.2f, 4.75f);
            root.AddComponent<Rigidbody>().isKinematic = true; // a moving collider
            return rig;
        }

        static Vector3 P(float x, float z, float y) => new Vector3(x, y, z);

        /// <summary>Door frame above the beltline at x, framing the side window (outline minus window as four strips).</summary>
        static void SideFrame(MeshBuilder mb, Material m, float x, Vector3 outward)
        {
            mb.Polygon(m, outward, false, P(x, 1.35f, 1.45f), P(x, 2.55f, 1.45f), P(x, 2.519f, 1.52f), P(x, 1.35f, 1.52f));
            mb.Polygon(m, outward, false, P(x, 2.419f, 1.52f), P(x, 2.519f, 1.52f), P(x, 2.2525f, 2.13f), P(x, 2.1525f, 2.13f));
            mb.Polygon(m, outward, false, P(x, 1.35f, 2.13f), P(x, 2.2525f, 2.13f), P(x, 2.2f, 2.25f), P(x, 1.35f, 2.25f));
            mb.Polygon(m, outward, false, P(x, 1.35f, 1.52f), P(x, 1.5f, 1.52f), P(x, 1.5f, 2.13f), P(x, 1.35f, 2.13f));
        }

        static void SteeringWheel(MeshBuilder mb, Material m, Vector3 center, Quaternion plane)
        {
            const int seg = 16;
            const float r = 0.19f;
            for (int k = 0; k < seg; k++)
            {
                float a = k * 360f / seg;
                Quaternion q = plane * Quaternion.Euler(0f, 0f, a);
                mb.BoxRotated(m, center + q * new Vector3(r, 0f, 0f), new Vector3(0.032f, 2f * Mathf.PI * r / seg * 1.12f, 0.032f), q);
            }
            mb.BoxRotated(m, center, new Vector3(0.12f, 0.12f, 0.05f), plane);
            for (int k = 0; k < 3; k++)
            {
                Quaternion q = plane * Quaternion.Euler(0f, 0f, -90f + (k - 1) * 100f);
                mb.BoxRotated(m, center + q * new Vector3(r * 0.5f, 0f, 0f), new Vector3(r, 0.03f, 0.015f), q);
            }
        }

        static Transform RearDoor(Transform body, int side, Material white, Material red, Material glass, Material trim)
        {
            var hinge = new GameObject(side < 0 ? "RearDoorL" : "RearDoorR").transform;
            hinge.SetParent(body, false);
            hinge.localPosition = new Vector3(side * 1.2f, 0f, -3.4f);
            var mb = new MeshBuilder();
            float cx = -side * 0.6f;
            mb.Box(white, new Vector3(cx, 1.9f, -0.025f), new Vector3(1.19f, 2.18f, 0.05f));
            mb.Box(red, new Vector3(cx, 1.48f, -0.052f), new Vector3(1.19f, 0.24f, 0.005f), 0b100000);
            mb.Box(glass, new Vector3(cx, 2.35f, -0.052f), new Vector3(0.6f, 0.5f, 0.005f), 0b100000);
            mb.Box(trim, new Vector3(-side * 1.05f, 1.6f, -0.07f), new Vector3(0.04f, 0.2f, 0.03f));
            mb.Build("Door", hinge);
            return hinge;
        }

        static Renderer Lens(Transform parent, string name, Vector3 center, Vector3 size, Material m)
        {
            var mb = new MeshBuilder();
            mb.Box(m, center, size);
            return mb.Build(name, parent, false).GetComponent<Renderer>();
        }

        static Light Spot(Transform parent, string name, Vector3 pos, Quaternion rot, Color c, float intensity, float range, float angle)
        {
            var go = new GameObject(name + "Light", typeof(Light));
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(pos, rot);
            var l = go.GetComponent<Light>();
            l.type = LightType.Spot;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = angle;
            l.innerSpotAngle = angle * 0.4f;
            l.shadows = LightShadows.None;
            return l;
        }

        static Light Point(Transform parent, string name, Vector3 pos, Color c, float range)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        static Transform Empty(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        /// <summary>A wheel that spins about its local X axis: tyre, rim and hub cap on the outer face.</summary>
        static Transform Wheel(Transform parent, Vector3 pos, int side, float radius, float width = 0.28f)
        {
            var t = new GameObject("Wheel").transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            var mb = new MeshBuilder();
            mb.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, side > 0 ? -90f : 90f), Vector3.one);
            mb.Cylinder(L(0.025f, 0.025f, 0.025f, 0.25f), new Vector3(0f, -width / 2f, 0f), radius, width, 20);
            mb.Cylinder(L(0.45f, 0.45f, 0.45f, 0.6f, 1f), new Vector3(0f, width / 2f, 0f), radius * 0.58f, 0.012f, 16);
            mb.Cylinder(L(0.1f, 0.1f, 0.1f, 0.4f, 1f), new Vector3(0f, width / 2f + 0.012f, 0f), radius * 0.18f, 0.03f, 10);
            mb.Build("Tyre", t);
            return t;
        }

        // ------------------------------------------------------------------ the ram truck

        /// <summary>
        /// A black pickup with a welded steel ram plate (a crude grin painted on it), off-road tyres and a roof light bar.
        /// No one inside is visible: the glass is dark.
        /// </summary>
        public static VehicleRig RamTruck(Transform parent)
        {
            var root = new GameObject("RamTruck");
            root.transform.SetParent(parent, false);
            var rig = root.AddComponent<VehicleRig>();
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            rig.body = body;

            var paint = L(0.025f, 0.025f, 0.028f, 0.3f);
            var steel = L(0.16f, 0.13f, 0.11f, 0.3f, 0.6f);
            var dark = L(0.015f, 0.015f, 0.015f, 0.3f);
            var glass = L(0.01f, 0.011f, 0.013f, 0.92f);
            var chalk = L(0.75f, 0.75f, 0.72f, 0.2f);

            var mb = new MeshBuilder();
            mb.Box(dark, new Vector3(0f, 0.62f, 0f), new Vector3(1.6f, 0.2f, 5.4f));
            mb.Box(paint, new Vector3(0f, 0.97f, 0f), new Vector3(2.0f, 0.66f, 5.4f));
            mb.Box(paint, new Vector3(0f, 1.38f, 1.85f), new Vector3(1.96f, 0.16f, 1.7f));
            mb.Box(paint, new Vector3(0f, 1.75f, -0.1f), new Vector3(1.94f, 0.9f, 2.2f));
            mb.Box(glass, new Vector3(0f, 1.8f, 1.005f), new Vector3(1.74f, 0.62f, 0.01f), 0b010000);
            mb.Box(glass, new Vector3(0f, 1.82f, -1.205f), new Vector3(1.5f, 0.45f, 0.01f), 0b100000);
            for (int s = -1; s <= 1; s += 2)
            {
                mb.Box(glass, new Vector3(s * 0.975f, 1.82f, -0.05f), new Vector3(0.01f, 0.55f, 1.85f), s < 0 ? 0b000010 : 0b000001);
                mb.Box(paint, new Vector3(s * 0.95f, 1.45f, -2.05f), new Vector3(0.1f, 0.32f, 1.5f));
                mb.Box(dark, new Vector3(s * 1.08f, 1.75f, 0.85f), new Vector3(0.14f, 0.2f, 0.12f)); // mirror
            }
            mb.Box(paint, new Vector3(0f, 1.45f, -2.75f), new Vector3(2.0f, 0.32f, 0.06f));
            // Ram plate on struts, with a grin daubed on it.
            mb.BoxRotated(steel, new Vector3(0f, 0.95f, 2.98f), new Vector3(2.15f, 0.85f, 0.08f), Quaternion.Euler(14f, 0f, 0f));
            for (int s = -1; s <= 1; s += 2)
                mb.Box(steel, new Vector3(s * 0.55f, 0.9f, 2.8f), new Vector3(0.1f, 0.12f, 0.4f));
            Quaternion plate = Quaternion.Euler(14f, 0f, 0f);
            for (int k = 0; k < 9; k++)
            {
                float u = (k - 4) / 4f;
                Vector3 p = new Vector3(u * 0.7f, 0.95f - 0.18f * (1f - u * u), 3.03f);
                mb.BoxRotated(chalk, p, new Vector3(0.18f, 0.035f, 0.01f), plate * Quaternion.Euler(0f, 0f, -u * 24f));
            }
            mb.Box(chalk, new Vector3(-0.35f, 1.2f, 3.06f), new Vector3(0.06f, 0.1f, 0.01f));
            mb.Box(chalk, new Vector3(0.35f, 1.2f, 3.06f), new Vector3(0.06f, 0.1f, 0.01f));
            // Roll bar with a light bar.
            for (int s = -1; s <= 1; s += 2)
                mb.Box(dark, new Vector3(s * 0.8f, 2.25f, 0.65f), new Vector3(0.05f, 0.12f, 0.05f));
            mb.Box(dark, new Vector3(0f, 2.33f, 0.65f), new Vector3(1.7f, 0.1f, 0.14f));
            mb.Build("Shell", body);

            var lensMat = RuntimeMaterials.Lit(new Color(0.9f, 0.9f, 0.88f), 0.9f, 0f, Color.black);
            rig.headlightLenses = new[]
            {
                Lens(body, "HeadlightL", new Vector3(-0.72f, 1.18f, 2.71f), new Vector3(0.32f, 0.14f, 0.02f), lensMat),
                Lens(body, "HeadlightR", new Vector3(0.72f, 1.18f, 2.71f), new Vector3(0.32f, 0.14f, 0.02f), lensMat),
                Lens(body, "LightBar", new Vector3(0f, 2.33f, 0.725f), new Vector3(1.5f, 0.06f, 0.01f), lensMat)
            };
            rig.headlightEmission = new Color(1f, 0.97f, 0.9f) * 12f;
            rig.headlights = new[]
            {
                Spot(body, "HighBeam", new Vector3(0f, 1.25f, 2.8f), Quaternion.Euler(3f, 0f, 0f), new Color(1f, 0.97f, 0.92f), 22f, 55f, 60f),
                Spot(body, "LightBar", new Vector3(0f, 2.35f, 0.8f), Quaternion.Euler(7f, 0f, 0f), new Color(0.92f, 0.96f, 1f), 30f, 60f, 75f)
            };

            rig.wheelRadius = 0.44f;
            rig.wheels = new[]
            {
                Wheel(root.transform, new Vector3(-0.92f, 0.44f, 1.75f), -1, 0.44f, 0.32f), Wheel(root.transform, new Vector3(0.92f, 0.44f, 1.75f), 1, 0.44f, 0.32f),
                Wheel(root.transform, new Vector3(-0.92f, 0.44f, -1.75f), -1, 0.44f, 0.32f), Wheel(root.transform, new Vector3(0.92f, 0.44f, -1.75f), 1, 0.44f, 0.32f)
            };
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.3f, 0.1f);
            col.size = new Vector3(2.05f, 1.9f, 5.9f);
            root.AddComponent<Rigidbody>().isKinematic = true;
            rig.SetHeadlights(false);
            return rig;
        }
    }
}
