using UnityEngine;
using UnityEngine.Rendering;
using Echoes.Rendering;
using Echoes.World;

namespace Echoes.Combat
{
    /// <summary>Procedural weapon and hand models. Local frame: +Z = muzzle direction, origin on the bore axis above the grip.</summary>
    public static class WeaponModels
    {
        public class PistolRig
        {
            public Transform root, slide, mag, muzzle, ejectPort;
        }

        static Material Slide => RuntimeMaterials.Lit(new Color(0.025f, 0.025f, 0.028f), 0.5f, 0.7f);
        static Material Polymer => RuntimeMaterials.Lit(new Color(0.03f, 0.03f, 0.032f), 0.28f);
        static Material Tritium => RuntimeMaterials.Lit(new Color(0.1f, 0.3f, 0.1f), 0.6f, 0f, new Color(0.25f, 1f, 0.3f) * 1.2f);
        public static Material Brass => RuntimeMaterials.Lit(new Color(0.62f, 0.43f, 0.16f), 0.75f, 1f);

        /// <summary>Polymer-frame striker pistol (Glock-like proportions, ~186 mm).</summary>
        public static PistolRig Pistol(Transform parent, bool viewModel)
        {
            var rig = new PistolRig();
            rig.root = new GameObject("Pistol").transform;
            rig.root.SetParent(parent, false);
            bool sh = !viewModel;

            var frame = new MeshBuilder();
            frame.Box(Polymer, new Vector3(0f, -0.021f, 0.004f), new Vector3(0.028f, 0.016f, 0.158f));
            frame.BoxRotated(Polymer, new Vector3(0f, -0.072f, -0.058f), new Vector3(0.03f, 0.1f, 0.05f), Quaternion.Euler(16f, 0f, 0f));
            frame.BoxRotated(Polymer, new Vector3(0f, -0.071f, -0.088f), new Vector3(0.028f, 0.09f, 0.012f), Quaternion.Euler(16f, 0f, 0f)); // backstrap
            frame.Box(Polymer, new Vector3(0f, -0.047f, 0.028f), new Vector3(0.008f, 0.005f, 0.05f));    // trigger guard bottom
            frame.Box(Polymer, new Vector3(0f, -0.037f, 0.052f), new Vector3(0.008f, 0.022f, 0.005f));   // guard front
            frame.Box(Slide, new Vector3(0f, -0.036f, 0.016f), new Vector3(0.006f, 0.018f, 0.006f));     // trigger
            frame.Box(Polymer, new Vector3(0f, -0.031f, 0.065f), new Vector3(0.022f, 0.004f, 0.03f));    // rail
            frame.Build("Frame", rig.root, sh);

            rig.slide = new GameObject("Slide").transform;
            rig.slide.SetParent(rig.root, false);
            var slide = new MeshBuilder();
            slide.Box(Slide, new Vector3(0f, 0f, 0f), new Vector3(0.025f, 0.027f, 0.186f));
            for (int i = 0; i < 7; i++) // rear serrations
                slide.Box(Polymer, new Vector3(0f, 0f, -0.082f + i * 0.005f), new Vector3(0.0255f, 0.02f, 0.0015f));
            slide.Box(Slide, new Vector3(0f, 0.0155f, 0.084f), new Vector3(0.004f, 0.005f, 0.004f));        // front sight
            slide.Box(Slide, new Vector3(0.0075f, 0.0155f, -0.084f), new Vector3(0.006f, 0.005f, 0.008f));  // rear sight L
            slide.Box(Slide, new Vector3(-0.0075f, 0.0155f, -0.084f), new Vector3(0.006f, 0.005f, 0.008f)); // rear sight R
            slide.Box(Tritium, new Vector3(0f, 0.0156f, 0.0818f), new Vector3(0.0018f, 0.0018f, 0.0005f));
            slide.Box(Tritium, new Vector3(0.0075f, 0.0156f, -0.0882f), new Vector3(0.0018f, 0.0018f, 0.0005f));
            slide.Box(Tritium, new Vector3(-0.0075f, 0.0156f, -0.0882f), new Vector3(0.0018f, 0.0018f, 0.0005f));
            slide.Box(Polymer, new Vector3(0.0128f, 0.004f, 0.012f), new Vector3(0.001f, 0.01f, 0.03f));    // ejection port
            var saved = slide.matrix;
            slide.matrix = Matrix4x4.TRS(new Vector3(0f, -0.002f, 0.093f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            slide.Cylinder(Polymer, Vector3.zero, 0.006f, 0.002f, 12);
            slide.matrix = saved;
            slide.Build("SlideMesh", rig.slide, sh);

            rig.mag = new GameObject("Mag").transform;
            rig.mag.SetParent(rig.root, false);
            // A whole magazine (body inside the grip, base plate, the top round showing) so a reload can be seen.
            var mag = new MeshBuilder();
            Quaternion grip = Quaternion.Euler(16f, 0f, 0f);
            mag.BoxRotated(Polymer, new Vector3(0f, -0.125f, -0.073f), new Vector3(0.031f, 0.012f, 0.052f), grip);
            mag.BoxRotated(Slide, new Vector3(0f, -0.07f, -0.062f), new Vector3(0.022f, 0.105f, 0.032f), grip);
            mag.BoxRotated(Brass, new Vector3(0f, -0.016f, -0.046f), new Vector3(0.009f, 0.009f, 0.022f), grip);
            mag.Build("MagMesh", rig.mag, sh);

            rig.muzzle = new GameObject("Muzzle").transform;
            rig.muzzle.SetParent(rig.root, false);
            rig.muzzle.localPosition = new Vector3(0f, -0.002f, 0.1f);
            rig.ejectPort = new GameObject("EjectPort").transform;
            rig.ejectPort.SetParent(rig.root, false);
            rig.ejectPort.localPosition = new Vector3(0.015f, 0.008f, 0.012f);
            return rig;
        }

        /// <summary>
        /// Daniel's hands in a two-handed grip: blue nitrile gloves (he's a paramedic), navy EMS jacket sleeves
        /// with reflective cuff bands. Forearms run back toward the chest-mounted camera.
        /// </summary>
        public static Transform Hands(Transform gunRoot, out Transform supportHand)
        {
            var root = new GameObject("Hands").transform;
            root.SetParent(gunRoot, false);
            // The support (left) hand is its own group: it lets go of the gun to swap magazines and rack the slide.
            supportHand = new GameObject("SupportHand").transform;
            supportHand.SetParent(root, false);
            var glove = RuntimeMaterials.Lit(new Color(0.07f, 0.13f, 0.42f), 0.55f);
            var sleeve = RuntimeMaterials.Lit(new Color(0.025f, 0.03f, 0.055f), 0.2f);
            var reflective = RuntimeMaterials.Lit(new Color(0.55f, 0.56f, 0.5f), 0.85f, 0.3f);

            // Strong hand wraps the grip; support hand cups it from the left.
            PrimitiveMeshes.Part(root, "RightHand", PrimitiveType.Sphere, new Vector3(0.006f, -0.07f, -0.062f), new Vector3(0.052f, 0.1f, 0.075f), glove, Quaternion.Euler(16f, 0f, 0f), false);
            PrimitiveMeshes.Part(root, "RightThumb", PrimitiveType.Capsule, new Vector3(-0.019f, -0.03f, -0.035f), new Vector3(0.018f, 0.03f, 0.018f), glove, Quaternion.Euler(70f, -10f, 0f), false);
            PrimitiveMeshes.Part(supportHand, "LeftHand", PrimitiveType.Sphere, new Vector3(-0.026f, -0.068f, -0.05f), new Vector3(0.05f, 0.095f, 0.08f), glove, Quaternion.Euler(16f, 0f, 10f), false);
            PrimitiveMeshes.Part(supportHand, "LeftThumb", PrimitiveType.Capsule, new Vector3(-0.017f, -0.024f, -0.005f), new Vector3(0.017f, 0.03f, 0.017f), glove, Quaternion.Euler(80f, 0f, 0f), false);
            Forearm(root, new Vector3(0.012f, -0.1f, -0.1f), new Vector3(0.14f, -0.26f, -0.42f), sleeve, reflective, glove);
            Forearm(supportHand, new Vector3(-0.034f, -0.095f, -0.085f), new Vector3(-0.15f, -0.25f, -0.4f), sleeve, reflective, glove);
            return root;
        }

        static void Forearm(Transform parent, Vector3 wrist, Vector3 elbow, Material sleeve, Material band, Material glove)
        {
            Vector3 d = elbow - wrist;
            var rot = Quaternion.FromToRotation(Vector3.up, d.normalized);
            float len = d.magnitude;
            PrimitiveMeshes.Part(parent, "Wrist", PrimitiveType.Capsule, wrist + d.normalized * 0.03f, new Vector3(0.045f, 0.035f, 0.045f), glove, rot, false);
            PrimitiveMeshes.Part(parent, "Sleeve", PrimitiveType.Capsule, wrist + d * 0.55f, new Vector3(0.075f, len * 0.55f, 0.075f), sleeve, rot, false);
            PrimitiveMeshes.Part(parent, "CuffBand", PrimitiveType.Cylinder, wrist + d.normalized * 0.075f, new Vector3(0.077f, 0.008f, 0.077f), band, rot, false);
        }

        public static GameObject Bottle(Transform parent)
        {
            var mb = new MeshBuilder();
            var glass = RuntimeMaterials.Lit(new Color(0.03f, 0.09f, 0.04f), 0.95f, 0.1f);
            var label = RuntimeMaterials.Lit(new Color(0.35f, 0.28f, 0.15f), 0.3f);
            mb.Cylinder(glass, new Vector3(0f, -0.1f, 0f), 0.034f, 0.15f, 14);
            mb.Cylinder(label, new Vector3(0f, -0.06f, 0f), 0.0345f, 0.06f, 14, false);
            mb.Cylinder(glass, new Vector3(0f, 0.05f, 0f), 0.022f, 0.025f, 12);
            mb.Cylinder(glass, new Vector3(0f, 0.075f, 0f), 0.012f, 0.06f, 10);
            var go = mb.Build("Bottle", parent);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        public static GameObject Bat(Transform parent)
        {
            var mb = new MeshBuilder();
            var wood = RuntimeMaterials.Lit(new Color(0.42f, 0.3f, 0.17f), 0.45f);
            var tape = RuntimeMaterials.Lit(new Color(0.03f, 0.03f, 0.03f), 0.2f);
            mb.Cylinder(tape, new Vector3(0f, -0.05f, 0f), 0.016f, 0.22f, 10);
            mb.Cylinder(wood, new Vector3(0f, 0.17f, 0f), 0.022f, 0.2f, 12);
            mb.Cylinder(wood, new Vector3(0f, 0.37f, 0f), 0.032f, 0.42f, 12);
            return mb.Build("Bat", parent);
        }
    }
}
