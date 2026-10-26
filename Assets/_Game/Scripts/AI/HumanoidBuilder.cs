using UnityEngine;
using Echoes.Combat;
using Echoes.Rendering;

namespace Echoes.AI
{
    public enum Faction { Looter, Smiler, Civilian }
    public enum NPCWeapon { None, Pistol, Bat }

    /// <summary>
    /// Builds a segmented humanoid (16 bones, ~1.75 m) with per-faction clothing and masks, bone hitboxes and colliders.
    /// Bones have identity rest rotations and limbs hang along −Y, which keeps procedural animation simple.
    /// </summary>
    public static class HumanoidBuilder
    {
        struct Outfit
        {
            public Material top, sleeves, pants, shoes, skin, hands, head;
        }

        public static HumanoidRig Build(Transform parent, Faction faction, int seed, out Health health)
        {
            var rng = new System.Random(seed);
            var root = new GameObject(faction.ToString()).transform;
            root.SetParent(parent, false);
            health = root.gameObject.AddComponent<Health>();
            var rig = root.gameObject.AddComponent<HumanoidRig>();
            var o = Dress(faction, rng);
            float s = 0.94f + (float)rng.NextDouble() * 0.12f; // height variation

            Transform B(Bone b, Transform p, Vector3 lp)
            {
                var t = new GameObject(b.ToString()).transform;
                t.SetParent(p, false);
                t.localPosition = lp * s;
                rig.bones[(int)b] = t;
                return t;
            }

            var pelvis = B(Bone.Pelvis, root, new Vector3(0f, 0.98f, 0f));
            var spine = B(Bone.Spine, pelvis, new Vector3(0f, 0.12f, 0f));
            var chest = B(Bone.Chest, spine, new Vector3(0f, 0.2f, 0f));
            var head = B(Bone.Head, chest, new Vector3(0f, 0.3f, 0f));
            var uaL = B(Bone.UpperArmL, chest, new Vector3(-0.2f, 0.17f, 0f));
            var laL = B(Bone.LowerArmL, uaL, new Vector3(0f, -0.29f, 0f));
            var hL = B(Bone.HandL, laL, new Vector3(0f, -0.26f, 0f));
            var uaR = B(Bone.UpperArmR, chest, new Vector3(0.2f, 0.17f, 0f));
            var laR = B(Bone.LowerArmR, uaR, new Vector3(0f, -0.29f, 0f));
            var hR = B(Bone.HandR, laR, new Vector3(0f, -0.26f, 0f));
            var ulL = B(Bone.UpperLegL, pelvis, new Vector3(-0.1f, -0.05f, 0f));
            var llL = B(Bone.LowerLegL, ulL, new Vector3(0f, -0.44f, 0f));
            var fL = B(Bone.FootL, llL, new Vector3(0f, -0.44f, 0f));
            var ulR = B(Bone.UpperLegR, pelvis, new Vector3(0.1f, -0.05f, 0f));
            var llR = B(Bone.LowerLegR, ulR, new Vector3(0f, -0.44f, 0f));
            var fR = B(Bone.FootR, llR, new Vector3(0f, -0.44f, 0f));

            // ---- Visual parts (renderers only; colliders live on the bones).
            var P = (System.Func<Transform, string, PrimitiveType, Vector3, Vector3, Material, GameObject>)((t, n, type, pos, scale, m) => PrimitiveMeshes.Part(t, n, type, pos, scale, m));
            P(pelvis, "Hips", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.33f, 0.2f, 0.21f), o.pants);
            P(spine, "Belly", PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(0.31f, 0.22f, 0.19f), o.top);
            P(chest, "Torso", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.005f), new Vector3(0.38f, 0.3f, 0.23f), o.top);
            P(chest, "Shoulders", PrimitiveType.Capsule, new Vector3(0f, 0.19f, 0f), new Vector3(0.2f, 0.23f, 0.2f), o.top).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            P(chest, "Neck", PrimitiveType.Capsule, new Vector3(0f, 0.28f, 0f), new Vector3(0.1f, 0.05f, 0.1f), o.skin);
            P(head, "Skull", PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0.01f), new Vector3(0.2f, 0.245f, 0.225f), o.head);
            foreach (var (ua, la, h) in new[] { (uaL, laL, hL), (uaR, laR, hR) })
            {
                P(ua, "UpperArm", PrimitiveType.Capsule, new Vector3(0f, -0.14f, 0f), new Vector3(0.105f, 0.16f, 0.105f), o.sleeves);
                P(la, "Forearm", PrimitiveType.Capsule, new Vector3(0f, -0.13f, 0f), new Vector3(0.088f, 0.145f, 0.088f), o.sleeves);
                P(h, "Hand", PrimitiveType.Sphere, new Vector3(0f, -0.05f, 0.005f), new Vector3(0.075f, 0.11f, 0.045f), o.hands);
            }
            foreach (var (ul, ll, f) in new[] { (ulL, llL, fL), (ulR, llR, fR) })
            {
                P(ul, "Thigh", PrimitiveType.Capsule, new Vector3(0f, -0.22f, 0f), new Vector3(0.155f, 0.24f, 0.155f), o.pants);
                P(ll, "Shin", PrimitiveType.Capsule, new Vector3(0f, -0.22f, 0f), new Vector3(0.125f, 0.23f, 0.125f), o.pants);
                P(f, "Shoe", PrimitiveType.Cube, new Vector3(0f, -0.015f, 0.05f), new Vector3(0.105f, 0.085f, 0.26f), o.shoes);
            }
            MaskFactory.Apply(faction, head, rng);

            // ---- Colliders + hitboxes.
            Capsule(pelvis, health, BodyPart.Torso, 1f, Vector3.zero, 0.16f, 0.34f, 0);
            Capsule(spine, health, BodyPart.Torso, 1f, new Vector3(0f, 0.09f, 0f), 0.15f, 0.3f, 1);
            Capsule(chest, health, BodyPart.Torso, 1.1f, new Vector3(0f, 0.13f, 0f), 0.17f, 0.4f, 0);
            Sphere(head, health, new Vector3(0f, 0.1f, 0.01f), 0.12f);
            foreach (var b in new[] { uaL, uaR }) Capsule(b, health, BodyPart.Limb, 0.6f, new Vector3(0f, -0.14f, 0f), 0.055f, 0.32f, 1);
            foreach (var b in new[] { laL, laR }) Capsule(b, health, BodyPart.Limb, 0.5f, new Vector3(0f, -0.13f, 0f), 0.045f, 0.3f, 1);
            foreach (var b in new[] { ulL, ulR }) Capsule(b, health, BodyPart.Limb, 0.65f, new Vector3(0f, -0.22f, 0f), 0.08f, 0.46f, 1);
            foreach (var b in new[] { llL, llR }) Capsule(b, health, BodyPart.Limb, 0.5f, new Vector3(0f, -0.22f, 0f), 0.065f, 0.45f, 1);
            foreach (var b in new[] { fL, fR }) { var c = b.gameObject.AddComponent<BoxCollider>(); c.center = new Vector3(0f, -0.015f, 0.05f); c.size = new Vector3(0.1f, 0.08f, 0.25f); }

            rig.weaponSocket = new GameObject("WeaponSocket").transform;
            rig.weaponSocket.SetParent(hR, false);
            rig.weaponSocket.localPosition = new Vector3(0f, -0.07f, 0.02f);
            rig.eyes = new GameObject("Eyes").transform;
            rig.eyes.SetParent(head, false);
            rig.eyes.localPosition = new Vector3(0f, 0.11f, 0.12f);
            return rig;
        }

        static void Capsule(Transform t, Health h, BodyPart part, float mult, Vector3 center, float radius, float height, int dir)
        {
            var c = t.gameObject.AddComponent<CapsuleCollider>();
            c.center = center;
            c.radius = radius;
            c.height = height;
            c.direction = dir; // 0 = X (hips/shoulders wide), 1 = Y (limbs)
            var hb = t.gameObject.AddComponent<Hitbox>();
            hb.owner = h;
            hb.part = part;
            hb.multiplier = mult;
        }

        static void Sphere(Transform t, Health h, Vector3 center, float radius)
        {
            var c = t.gameObject.AddComponent<SphereCollider>();
            c.center = center;
            c.radius = radius;
            var hb = t.gameObject.AddComponent<Hitbox>();
            hb.owner = h;
            hb.part = BodyPart.Head;
            hb.multiplier = 3.5f;
        }

        static Material L(float r, float g, float b, float smooth = 0.2f) => RuntimeMaterials.Lit(new Color(r, g, b), smooth);

        static Outfit Dress(Faction f, System.Random rng)
        {
            Color[] skins = { new Color(0.45f, 0.3f, 0.22f), new Color(0.3f, 0.19f, 0.13f), new Color(0.6f, 0.43f, 0.33f), new Color(0.18f, 0.11f, 0.08f) };
            var skin = RuntimeMaterials.Lit(skins[rng.Next(skins.Length)], 0.35f);
            switch (f)
            {
                case Faction.Smiler:
                    // Matching black suits with white gloves: a "uniform" for the stream.
                    var suit = L(0.015f, 0.015f, 0.018f, 0.35f);
                    return new Outfit { top = suit, sleeves = suit, pants = suit, shoes = L(0.01f, 0.01f, 0.01f, 0.7f), skin = skin, hands = L(0.7f, 0.7f, 0.68f, 0.3f), head = L(0.02f, 0.02f, 0.02f, 0.85f) };
                case Faction.Looter:
                    Material[] hoodies = { L(0.08f, 0.08f, 0.085f), L(0.12f, 0.1f, 0.05f), L(0.05f, 0.07f, 0.04f), L(0.15f, 0.03f, 0.03f) };
                    var hoodie = hoodies[rng.Next(hoodies.Length)];
                    return new Outfit { top = hoodie, sleeves = hoodie, pants = rng.NextDouble() < 0.6 ? L(0.04f, 0.06f, 0.12f) : L(0.06f, 0.06f, 0.06f), shoes = L(0.25f, 0.25f, 0.25f), skin = skin, hands = skin, head = hoodie };
                default:
                    Material[] tops = { L(0.35f, 0.33f, 0.3f), L(0.08f, 0.15f, 0.3f), L(0.3f, 0.12f, 0.1f), L(0.2f, 0.22f, 0.15f) };
                    var top = tops[rng.Next(tops.Length)];
                    return new Outfit { top = top, sleeves = rng.NextDouble() < 0.5 ? top : skin, pants = L(0.12f, 0.1f, 0.08f), shoes = L(0.1f, 0.07f, 0.05f), skin = skin, hands = skin, head = skin };
            }
        }
    }
}
