using UnityEngine;
using Echoes.Rendering;

namespace Echoes.World
{
    /// <summary>
    /// The school's front door for demo sequence 5, built at runtime (it uses runtime textures: the spray paint and the
    /// photo). Steel double doors in a stone surround on the facade facing the yard, chained shut; "SMILE, DANNY." and
    /// "YOU'RE LIVE." sprayed across them in red with Daniel's Marine photo taped between the lines; the stencilled school
    /// name on the board above; and in front, a phone on a tripod, filming, its torch on the door.
    /// </summary>
    public static class SchoolDoorBuilder
    {
        public class Parts
        {
            public GameObject door;        // collider + where the interactable goes
            public Transform photo;
            public Transform phone;        // the stream's camera and speaker
            public Light torch;
            public Renderer recDot;
        }

        public const string SchoolName = "KESSLER STREET MIDDLE SCHOOL";

        public static Parts Build(Transform parent)
        {
            var p = new Parts();
            var root = new GameObject("SchoolDoor").transform;
            root.SetParent(parent, false);
            Vector3 o = AlleyBuilder.SchoolDoor;          // facade plane, door centre, yard level
            float fz = o.z;                                // the facade faces −Z
            var stone = RuntimeMaterials.Lit(new Color(0.45f, 0.43f, 0.4f), 0.15f);
            var steel = RuntimeMaterials.Lit(new Color(0.07f, 0.16f, 0.13f), 0.35f, 0.5f);
            var dark = RuntimeMaterials.Lit(new Color(0.03f, 0.03f, 0.035f), 0.85f);
            var galv = RuntimeMaterials.Lit(new Color(0.55f, 0.56f, 0.56f), 0.5f, 0.8f);

            var mb = new MeshBuilder();
            // Surround, step.
            mb.Box(stone, new Vector3(o.x - 1.35f, o.y + 1.45f, fz - 0.15f), new Vector3(0.3f, 2.9f, 0.3f));
            mb.Box(stone, new Vector3(o.x + 1.35f, o.y + 1.45f, fz - 0.15f), new Vector3(0.3f, 2.9f, 0.3f));
            mb.Box(stone, new Vector3(o.x, o.y + 2.75f, fz - 0.17f), new Vector3(3f, 0.35f, 0.34f));
            mb.Box(stone, new Vector3(o.x, o.y + 0.06f, fz - 0.9f), new Vector3(4f, 0.12f, 1.2f));
            // Two steel leaves with wired glass and push bars, chained together.
            for (int s = -1; s <= 1; s += 2)
            {
                float cx = o.x + s * 0.6f;
                mb.Box(steel, new Vector3(cx, o.y + 1.32f, fz - 0.14f), new Vector3(1.18f, 2.4f, 0.06f));
                mb.Box(dark, new Vector3(cx, o.y + 1.85f, fz - 0.172f), new Vector3(0.34f, 0.6f, 0.01f));
                mb.Box(galv, new Vector3(cx, o.y + 1.05f, fz - 0.23f), new Vector3(0.95f, 0.05f, 0.05f));
                mb.Box(galv, new Vector3(cx - s * 0.45f, o.y + 1.05f, fz - 0.2f), new Vector3(0.05f, 0.08f, 0.06f));
                mb.Box(galv, new Vector3(cx + s * 0.45f, o.y + 1.05f, fz - 0.2f), new Vector3(0.05f, 0.08f, 0.06f));
            }
            for (int i = 0; i < 12; i++)
            {
                float x = o.x - 0.55f + i * 0.1f;
                mb.BoxRotated(galv, new Vector3(x, o.y + 1.05f + (i % 2 == 0 ? 0.02f : -0.02f), fz - 0.27f), new Vector3(0.09f, 0.025f, 0.012f),
                              Quaternion.Euler(0f, 0f, i % 2 == 0 ? 25f : -25f));
            }
            mb.Box(RuntimeMaterials.Lit(new Color(0.6f, 0.48f, 0.2f), 0.6f, 0.9f), new Vector3(o.x, o.y + 0.98f, fz - 0.29f), new Vector3(0.07f, 0.09f, 0.03f)); // padlock
            p.door = mb.Build("Doors", root);
            var bc = p.door.AddComponent<BoxCollider>();
            bc.center = new Vector3(o.x, o.y + 1.45f, fz - 0.16f);
            bc.size = new Vector3(3f, 2.9f, 0.32f);
            var step = p.door.AddComponent<BoxCollider>();
            step.center = new Vector3(o.x, o.y + 0.06f, fz - 0.9f);
            step.size = new Vector3(4f, 0.12f, 1.2f);

            // The paint: two lines across the doors, the photo between them.
            var paint = new Color(0.72f, 0.04f, 0.035f);
            Text(root, "SMILE, DANNY.", true, new Vector3(o.x, o.y + 2.08f, fz - 0.18f), 2.3f, paint, paint * 0.18f, 71);
            Text(root, "YOU'RE LIVE.", true, new Vector3(o.x + 0.05f, o.y + 0.62f, fz - 0.18f), 2.1f, paint, paint * 0.18f, 72);
            var photoMat = new Material(RuntimeMaterials.Lit(Color.white, 0.35f)) { name = "RT_MarinePhoto", hideFlags = HideFlags.DontSave };
            photoMat.SetTexture("_BaseMap", ProcTextures.MarinePhoto());
            p.photo = PrimitiveMeshes.Part(root, "Photo", PrimitiveType.Quad, new Vector3(o.x + 0.02f, o.y + 1.45f, fz - 0.268f),
                                           new Vector3(0.2f, 0.25f, 1f), photoMat, Quaternion.Euler(0f, 0f, -4f), false).transform;
            var tape = RuntimeMaterials.Lit(new Color(0.85f, 0.8f, 0.6f), 0.4f);
            PrimitiveMeshes.Part(root, "Tape", PrimitiveType.Cube, new Vector3(o.x - 0.07f, o.y + 1.575f, fz - 0.272f), new Vector3(0.08f, 0.025f, 0.002f), tape, Quaternion.Euler(0f, 0f, 35f), false);
            PrimitiveMeshes.Part(root, "Tape", PrimitiveType.Cube, new Vector3(o.x + 0.12f, o.y + 1.57f, fz - 0.272f), new Vector3(0.08f, 0.025f, 0.002f), tape, Quaternion.Euler(0f, 0f, -30f), false);

            // The school's name, stencilled on the board AlleyBuilder put over the doors.
            Text(root, SchoolName, false, new Vector3(o.x, o.y + 4.9f, AlleyBuilder.YardZ1 - 0.19f), 8.2f, new Color(0.8f, 0.78f, 0.7f), null, 73);

            // The phone on a tripod, three metres out, filming the door; screen toward the yard, torch toward the door.
            Vector3 hub = new Vector3(o.x, o.y + 1.45f, fz - 3.2f);
            var tripod = RuntimeMaterials.Lit(new Color(0.05f, 0.05f, 0.05f), 0.4f, 0.3f);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 30f) * Mathf.Deg2Rad;
                Vector3 foot = new Vector3(hub.x + Mathf.Cos(a) * 0.38f, o.y, hub.z + Mathf.Sin(a) * 0.38f);
                Vector3 mid = (hub + foot) * 0.5f;
                PrimitiveMeshes.Part(root, "Leg", PrimitiveType.Cylinder, mid, new Vector3(0.018f, Vector3.Distance(hub, foot) * 0.5f, 0.018f), tripod,
                                     Quaternion.FromToRotation(Vector3.up, hub - foot), false);
            }
            var phone = new GameObject("StreamPhone").transform;
            phone.SetParent(root, false);
            phone.SetPositionAndRotation(hub + Vector3.up * 0.1f, Quaternion.identity);
            PrimitiveMeshes.Part(phone, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.075f, 0.15f, 0.009f), RuntimeMaterials.Lit(new Color(0.02f, 0.02f, 0.022f), 0.85f), null, false);
            PrimitiveMeshes.Part(phone, "Screen", PrimitiveType.Cube, new Vector3(0f, 0f, -0.005f), new Vector3(0.067f, 0.138f, 0.002f),
                                 RuntimeMaterials.Lit(Color.black, 0.9f, 0f, new Color(0.9f, 0.35f, 0.4f) * 1.6f), null, false);
            p.recDot = PrimitiveMeshes.Part(phone, "Rec", PrimitiveType.Sphere, new Vector3(0.025f, 0.06f, -0.007f), Vector3.one * 0.008f,
                                            RuntimeMaterials.Lit(Color.black, 0.5f, 0f, new Color(1f, 0.1f, 0.1f) * 6f), null, false).GetComponent<Renderer>();
            PrimitiveMeshes.Part(phone, "TorchLED", PrimitiveType.Sphere, new Vector3(0.02f, 0.06f, 0.006f), Vector3.one * 0.01f,
                                 RuntimeMaterials.Lit(Color.white, 0.9f, 0f, Color.white * 12f), null, false);
            var tl = new GameObject("PhoneTorch", typeof(Light));
            tl.transform.SetParent(phone, false);
            tl.transform.localPosition = new Vector3(0.02f, 0.06f, 0.02f);
            tl.transform.rotation = Quaternion.LookRotation(new Vector3(o.x, o.y + 1.5f, fz) - tl.transform.position);
            p.torch = tl.GetComponent<Light>();
            p.torch.type = LightType.Spot;
            p.torch.color = new Color(0.95f, 0.97f, 1f);
            p.torch.intensity = 6f;
            p.torch.range = 9f;
            p.torch.spotAngle = 55f;
            p.torch.innerSpotAngle = 25f;
            p.torch.shadows = LightShadows.Soft; // Daniel's own shadow lands on the door when he steps into the shot
            p.phone = phone;
            return p;
        }

        /// <summary>A painted/stencilled text quad on a wall facing −Z, `width` metres wide, centred on `centre`.</summary>
        static void Text(Transform root, string text, bool spray, Vector3 centre, float width, Color tint, Color? glow, int seed)
        {
            var aspect = PixelFont.Aspect(text, spray);
            var mat = RuntimeMaterials.Decal("Paint_" + text, PixelFont.Paint(text, spray, spray ? 10 : 8, seed), tint, spray ? 0.55f : 0.2f, glow);
            PrimitiveMeshes.Part(root, "Text", PrimitiveType.Quad, centre, new Vector3(width, width * aspect.y / aspect.x, 1f), mat,
                                 Quaternion.Euler(0f, 0f, spray ? Random.Range(-2.5f, 2.5f) : 0f), false);
        }
    }
}
