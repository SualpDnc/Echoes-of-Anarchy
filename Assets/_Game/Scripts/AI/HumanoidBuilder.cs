using System;
using UnityEngine;
using UnityEngine.Rendering;
using Echoes.Combat;
using Echoes.Rendering;

namespace Echoes.AI
{
    public enum Faction { Looter, Smiler, Civilian }
    public enum NPCWeapon { None, Pistol, Bat }

    /// <summary>
    /// Builds a humanoid (16 bones, ~1.75 m) with one skinned mesh: lofted torso, arms and legs with garment layers
    /// (hems, cuffs, waistbands), shoes, sculpted head and mask (MaskFactory), clothing details (OutfitDetails),
    /// plus bone hitboxes and colliders. Bones have identity rest rotations and limbs hang along −Y, which keeps
    /// procedural animation simple. Geometry is authored for a 1.0-scale body and scaled by look.height.
    /// </summary>
    public static class HumanoidBuilder
    {
        // Rest heights (unscaled) of the joints the skinning blends around.
        const float NeckY = 1.585f, ChestY = 1.27f, SpineY = 1.07f, ElbowY = 1.185f, WristY = 0.935f, KneeY = 0.49f, AnkleY = 0.07f;

        public static HumanoidRig Build(Transform parent, Faction faction, int seed, out Health health)
        {
            var rng = new System.Random(seed);
            return Build(parent, CharacterLook.Random(faction, rng), rng, out health);
        }

        public static HumanoidRig Build(Transform parent, CharacterLook look, System.Random rng, out Health health)
        {
            var root = new GameObject(look.faction.ToString()).transform;
            root.SetParent(parent, false);
            health = root.gameObject.AddComponent<Health>();
            var rig = root.gameObject.AddComponent<HumanoidRig>();
            float s = look.height;
            float armX = look.feminine ? 0.172f : 0.185f;

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
            var uaL = B(Bone.UpperArmL, chest, new Vector3(-armX, 0.17f, 0f));
            var laL = B(Bone.LowerArmL, uaL, new Vector3(0f, -0.29f, 0f));
            var hL = B(Bone.HandL, laL, new Vector3(0f, -0.26f, 0f));
            var uaR = B(Bone.UpperArmR, chest, new Vector3(armX, 0.17f, 0f));
            var laR = B(Bone.LowerArmR, uaR, new Vector3(0f, -0.29f, 0f));
            var hR = B(Bone.HandR, laR, new Vector3(0f, -0.26f, 0f));
            var ulL = B(Bone.UpperLegL, pelvis, new Vector3(-0.1f, -0.05f, 0f));
            var llL = B(Bone.LowerLegL, ulL, new Vector3(0f, -0.44f, 0f));
            var fL = B(Bone.FootL, llL, new Vector3(0f, -0.44f, 0f));
            var ulR = B(Bone.UpperLegR, pelvis, new Vector3(0.1f, -0.05f, 0f));
            var llR = B(Bone.LowerLegR, ulR, new Vector3(0f, -0.44f, 0f));
            var fR = B(Bone.FootR, llR, new Vector3(0f, -0.44f, 0f));

            // ---- Skinned body.
            var mb = new SkinnedBodyBuilder();
            // Dense lofts (owner: characters as realistic as possible): round silhouettes, smooth shading at the
            // joints and room for the garment hems to read.
            var torso = TorsoProfile(look);
            mb.Tube(torso, 34, 0.018f, TorsoSkin);
            var legs = new BodyProfile[2];
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = ArmProfile(look, side * armX);
                Func<Vector3, Skin> armSkin = ArmSkin(side);
                mb.Tube(arm, 18, 0.018f, armSkin);
                mb.Tube(ThumbProfile(look, side * (armX - 0.006f)), 8, 0.012f, armSkin);
                Fingers(mb, look, side * armX, side, armSkin);
                var leg = LegProfile(look, side * 0.1f);
                legs[side < 0 ? 0 : 1] = leg;
                mb.Tube(leg, 22, 0.022f, LegSkin(side));
                Shoe(mb, look, side * 0.1f, Skin.On(side < 0 ? Bone.FootL : Bone.FootR));
            }
            OutfitDetails.Add(mb, look, torso, legs, TorsoSkin, LegSkin(-1), LegSkin(1));
            if (look.armor) Armour(mb, look);
            MaskFactory.Head(mb, look, rng);
            mb.ScaleVertices(s);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(root, false);
            var smr = bodyGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mb.ToMesh(look.faction + "_Body", rig.bones, bodyGo.transform, out var mats);
            smr.sharedMaterials = mats;
            smr.bones = rig.bones;
            smr.rootBone = pelvis;
            smr.quality = SkinQuality.Bone2;
            smr.localBounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(2.4f, 2.6f, 2.4f)); // generous: ragdolls sprawl
            smr.shadowCastingMode = ShadowCastingMode.On;

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

        // ------------------------------------------------------------------ skinning

        static Skin TorsoSkin(Vector3 p)
        {
            if (p.y > 1.5f) return Skin.Joint(Bone.Head, Bone.Chest, p.y, NeckY, 0.035f);
            if (p.y > 1.17f) return Skin.Joint(Bone.Chest, Bone.Spine, p.y, ChestY, 0.06f);
            if (p.y > 0.93f) return Skin.Joint(Bone.Spine, Bone.Pelvis, p.y, SpineY, 0.05f);
            // Buttocks and hip sides partly follow the thighs, so a raised leg doesn't tear out of the hips.
            float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.93f, 0.82f, p.y)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.03f, 0.1f, Mathf.Abs(p.x))) * 0.55f;
            return Skin.Blend(Bone.Pelvis, p.x < 0f ? Bone.UpperLegL : Bone.UpperLegR, w);
        }

        static Func<Vector3, Skin> ArmSkin(int side)
        {
            Bone ua = side < 0 ? Bone.UpperArmL : Bone.UpperArmR, la = side < 0 ? Bone.LowerArmL : Bone.LowerArmR, h = side < 0 ? Bone.HandL : Bone.HandR;
            return p => p.y > 1.06f ? Skin.Joint(ua, la, p.y, ElbowY, 0.04f) : Skin.Joint(la, h, p.y, WristY, 0.025f);
        }

        static Func<Vector3, Skin> LegSkin(int side)
        {
            Bone ul = side < 0 ? Bone.UpperLegL : Bone.UpperLegR, ll = side < 0 ? Bone.LowerLegL : Bone.LowerLegR, f = side < 0 ? Bone.FootL : Bone.FootR;
            return p => p.y > 0.28f ? Skin.Joint(ul, ll, p.y, KneeY, 0.05f) : Skin.Joint(ll, f, p.y, AnkleY, 0.025f);
        }

        // ------------------------------------------------------------------ profiles

        static BodyProfile TorsoProfile(CharacterLook look)
        {
            var p = new BodyProfile { baseMat = look.skin };
            p.Add(0.80f, 0.10f, 0.06f, 0.07f, 2.2f)
             .Add(0.84f, 0.15f, 0.08f, 0.095f, 2.4f)
             .Add(0.90f, 0.172f, 0.09f, 0.11f, 2.5f)
             .Add(0.96f, 0.172f, 0.09f, 0.105f, 2.5f)
             .Add(1.02f, 0.16f, 0.09f, 0.095f, 2.5f)
             .Add(1.08f, 0.148f, 0.092f, 0.088f, 2.5f)
             .Add(1.15f, 0.152f, 0.098f, 0.09f, 2.6f)
             .Add(1.22f, 0.162f, 0.105f, 0.095f, 2.7f)
             .Add(1.30f, 0.172f, 0.112f, 0.10f, 2.8f)
             .Add(1.38f, 0.178f, 0.11f, 0.10f, 2.9f)
             .Add(1.44f, 0.178f, 0.095f, 0.095f, 3.0f)
             .Add(1.49f, 0.155f, 0.08f, 0.085f, 2.6f)
             .Add(1.53f, 0.085f, 0.065f, 0.07f, 2.2f)
             .Add(1.57f, 0.055f, 0.055f, 0.055f, 2f)
             .Add(1.64f, 0.05f, 0.05f, 0.05f, 2f);
            if (look.feminine)
            {
                p.Bulge(0.93f, 0.06f, 0f, 0.01f, 0.012f);   // hips
                p.Bulge(1.09f, 0.06f, -0.004f, 0f, -0.014f); // waist
                p.Bulge(1.42f, 0.05f, 0f, 0f, -0.016f);      // narrower shoulders
                p.Bulge(1.32f, 0.05f, 0.03f);                // bust
            }
            p.Inflate(look.bulk);
            if (look.belly > 0f) p.Bulge(1.1f, 0.09f, 0.045f * look.belly, 0.005f * look.belly, 0.015f * look.belly);

            // Garments, inner to outer.
            switch (look.pants)
            {
                case PantsKind.Sweatpants: p.layers.Add(new BodyProfile.Layer(look.pantsMat, 0.8f, 1.04f, 0.008f, 0.008f, 0f, 0.035f)); break;
                default: p.layers.Add(new BodyProfile.Layer(look.pantsMat, 0.8f, 1.03f, 0.006f, 0.006f, 0f, 0.03f)); break;
            }
            switch (look.top)
            {
                case TopKind.Hoodie: p.layers.Add(new BodyProfile.Layer(look.topMat, 0.9f, 1.545f, 0.014f, 0.011f, 0.045f, 0.015f)); break;
                case TopKind.TShirt: p.layers.Add(new BodyProfile.Layer(look.topMat, 0.93f, 1.535f, 0.008f, 0.006f, 0f, 0.012f)); break;
                case TopKind.Sweater: p.layers.Add(new BodyProfile.Layer(look.topMat, 0.92f, 1.555f, 0.013f, 0.012f, 0.035f, 0.035f)); break;
                case TopKind.OpenJacket: p.layers.Add(new BodyProfile.Layer(look.topMat, 0.89f, 1.545f, 0.016f, 0.013f)); break;
                case TopKind.WorkJacket: p.layers.Add(new BodyProfile.Layer(look.topMat, 0.9f, 1.56f, 0.017f, 0.014f, 0.03f, 0.03f)); break;
                case TopKind.Suit:
                    p.layers.Add(new BodyProfile.Layer(look.topMat, 0.86f, 1.5f, 0.014f, 0.011f));
                    p.layers.Add(new BodyProfile.Layer(look.innerMat, 1.49f, 1.565f, 0.008f, 0.008f)); // shirt collar
                    break;
            }
            return p;
        }

        static BodyProfile ArmProfile(CharacterLook look, float x)
        {
            float k = 1f + (look.bulk - 1f) * 0.8f;
            float f = look.feminine ? 0.88f : 1f;
            var p = new BodyProfile { baseMat = look.skin };
            p.Add(1.535f, 0.03f * k, 0.03f * k, 0.03f * k, 2f, x)
             .Add(1.52f, 0.05f * k, 0.05f * k, 0.05f * k, 2f, x)
             .Add(1.49f, 0.058f * k * f, 0.055f * k * f, 0.055f * k * f, 2f, x)
             .Add(1.42f, 0.052f * k * f, 0.05f * k * f, 0.05f * k * f, 2f, x)
             .Add(1.34f, 0.046f * k * f, 0.048f * k * f, 0.045f * k * f, 2f, x)
             .Add(1.25f, 0.042f * k * f, 0.042f * k * f, 0.04f * k * f, 2f, x)
             .Add(1.18f, 0.038f * k * f, 0.037f * k * f, 0.038f * k * f, 2f, x)
             .Add(1.12f, 0.042f * k * f, 0.04f * k * f, 0.04f * k * f, 2f, x)
             .Add(1.04f, 0.036f * k * f, 0.034f * k * f, 0.033f * k * f, 2f, x)
             // Palm: faces the thigh, so it is thin in X and wide in Z. It ends at the knuckles; Fingers() hangs below.
             .Add(0.94f, 0.022f * f, 0.028f * f, 0.028f * f, 2f, x)
             .Add(0.91f, 0.02f * f, 0.038f * f, 0.034f * f, 2.4f, x)
             .Add(0.87f, 0.019f * f, 0.046f * f, 0.04f * f, 2.6f, x)
             .Add(0.848f, 0.017f * f, 0.044f * f, 0.039f * f, 2.8f, x)
             .Add(0.835f, 0.013f * f, 0.036f * f, 0.032f * f, 2.4f, x);

            if (look.gloves) p.layers.Add(new BodyProfile.Layer(look.gloveMat, 0.835f, 0.975f, 0.003f, 0.003f));
            switch (look.top)
            {
                case TopKind.Hoodie: p.layers.Add(new BodyProfile.Layer(look.topMat, 0.95f, 1.54f, 0.013f, 0.012f, 0.045f)); break;
                case TopKind.Sweater: p.layers.Add(new BodyProfile.Layer(look.topMat, 0.955f, 1.54f, 0.012f, 0.012f, 0.04f)); break;
                case TopKind.TShirt:
                    if (look.shortSleeves) p.layers.Add(new BodyProfile.Layer(look.topMat, 1.28f, 1.54f, 0.012f, 0.007f, 0.012f));
                    else p.layers.Add(new BodyProfile.Layer(look.topMat, 0.96f, 1.54f, 0.008f, 0.007f, 0.015f));
                    break;
                case TopKind.OpenJacket:
                case TopKind.WorkJacket: p.layers.Add(new BodyProfile.Layer(look.topMat, 0.955f, 1.54f, 0.016f, 0.015f, look.top == TopKind.WorkJacket ? 0.03f : 0f)); break;
                case TopKind.Suit:
                    p.layers.Add(new BodyProfile.Layer(look.innerMat, 0.945f, 0.975f, 0.009f, 0.009f)); // shirt cuff
                    p.layers.Add(new BodyProfile.Layer(look.topMat, 0.965f, 1.54f, 0.013f, 0.012f));
                    break;
            }
            return p;
        }

        static BodyProfile ThumbProfile(CharacterLook look, float x)
        {
            var p = new BodyProfile { baseMat = look.gloves ? look.gloveMat : look.skin };
            float f = look.feminine ? 0.88f : 1f;
            p.Add(0.925f, 0.013f * f, 0.013f * f, 0.013f * f, 2f, x, 0.022f)
             .Add(0.895f, 0.0125f * f, 0.012f * f, 0.012f * f, 2f, x, 0.04f)
             .Add(0.865f, 0.01f * f, 0.01f * f, 0.01f * f, 2f, x, 0.05f)
             .Add(0.85f, 0.006f * f, 0.006f * f, 0.006f * f, 2f, x, 0.052f);
            return p;
        }

        /// <summary>
        /// Four fingers from the knuckles down, relaxed: each tapers, the tips curl in toward the palm (which faces
        /// the thigh) and the hand fans out slightly. They start inside the palm, so the joint never shows a gap.
        /// </summary>
        static void Fingers(SkinnedBodyBuilder mb, CharacterLook look, float x, int side, Func<Vector3, Skin> skin)
        {
            float f = look.feminine ? 0.88f : 1f;
            var mat = look.gloves ? look.gloveMat : look.skin;
            float[] z = { 0.027f, 0.009f, -0.009f, -0.025f };       // index (thumb side, +Z) to little finger
            float[] len = { 0.074f, 0.082f, 0.077f, 0.062f };
            float[] r = { 0.0088f, 0.0092f, 0.0086f, 0.0074f };
            for (int i = 0; i < 4; i++)
            {
                var p = new BodyProfile { baseMat = mat };
                const float top = 0.852f;
                float l = len[i] * f, rr = r[i] * f;
                float Curl(float u) => u * u * 0.016f * f;
                float Fan(float u) => z[i] * f * (1f + 0.18f * u);
                for (int k = 0; k <= 5; k++)
                {
                    float u = k / 5f;
                    // Knuckle joints show as a slight swelling at a third and two thirds of the length.
                    float knuckle = 1f + 0.06f * Mathf.Exp(-Mathf.Pow((u - 0.38f) / 0.08f, 2f)) + 0.05f * Mathf.Exp(-Mathf.Pow((u - 0.7f) / 0.07f, 2f));
                    float w = rr * (1f - 0.2f * u) * knuckle;
                    p.Add(top - u * l, w * 0.95f, w, w, 2f, x - side * Curl(u), Fan(u));
                }
                p.Add(top - l - rr * 0.55f, rr * 0.4f, rr * 0.45f, rr * 0.4f, 2f, x - side * Curl(1.08f), Fan(1.08f)); // round fingertip
                mb.Tube(p, 8, 0.011f, skin);
            }
        }

        static BodyProfile LegProfile(CharacterLook look, float x)
        {
            float k = 1f + (look.bulk - 1f) * 0.9f;
            float f = look.feminine ? 1.04f : 1f;
            var p = new BodyProfile { baseMat = look.skin };
            p.Add(0.04f, 0.03f, 0.03f, 0.032f, 2f, x)
             .Add(0.08f, 0.032f, 0.035f, 0.035f, 2f, x)
             .Add(0.14f, 0.033f * k, 0.033f * k, 0.035f * k, 2f, x)
             .Add(0.24f, 0.042f * k, 0.04f * k, 0.05f * k, 2f, x)
             .Add(0.34f, 0.05f * k, 0.045f * k, 0.065f * k, 2f, x)
             .Add(0.43f, 0.05f * k, 0.05f * k, 0.055f * k, 2f, x)
             .Add(0.50f, 0.052f * k, 0.058f * k, 0.05f * k, 2f, x)
             .Add(0.58f, 0.058f * k * f, 0.06f * k * f, 0.055f * k * f, 2f, x)
             .Add(0.70f, 0.07f * k * f, 0.068f * k * f, 0.07f * k * f, 2f, x)
             .Add(0.82f, 0.077f * k * f, 0.076f * k * f, 0.082f * k * f, 2f, x)
             .Add(0.92f, 0.08f * k * f, 0.078f * k * f, 0.088f * k * f, 2f, x)
             .Add(0.98f, 0.065f * k, 0.065f * k, 0.07f * k, 2f, x);

            switch (look.pants)
            {
                case PantsKind.Cargo: p.layers.Add(new BodyProfile.Layer(look.pantsMat, 0.06f, 0.98f, 0.02f, 0.014f)); break;
                case PantsKind.Sweatpants: p.layers.Add(new BodyProfile.Layer(look.pantsMat, 0.07f, 0.98f, 0.006f, 0.013f, 0.045f)); break;
                case PantsKind.Suit: p.layers.Add(new BodyProfile.Layer(look.pantsMat, 0.055f, 0.98f, 0.014f, 0.008f)); break;
                default: p.layers.Add(new BodyProfile.Layer(look.pantsMat, 0.06f, 0.98f, 0.014f, 0.006f)); break;
            }
            if (look.shoes == ShoeKind.Boot) p.layers.Add(new BodyProfile.Layer(look.shoeMat, 0.04f, 0.17f, 0.016f, 0.016f, 0f, 0.012f));
            return p;
        }

        /// <summary>Shoe upper lofted heel-to-toe (a tube along +Z) on a flat, slightly wider sole.</summary>
        static void Shoe(SkinnedBodyBuilder mb, CharacterLook look, float x, Skin skin)
        {
            bool dress = look.shoes == ShoeKind.Dress;
            float w = dress ? 0.9f : look.shoes == ShoeKind.Boot ? 1.08f : 1.04f;
            float toeH = dress ? 0.8f : 1.05f;
            // Local tube space: y = world forward (+Z), z = world down (so cz = −height of the section centre).
            mb.matrix = Matrix4x4.TRS(new Vector3(x, 0.012f, 0f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            var upper = new BodyProfile { baseMat = look.shoeMat };
            upper.Add(-0.075f, 0.028f * w, 0.026f, 0.018f, 2.2f, 0f, -0.038f)
                 .Add(-0.062f, 0.04f * w, 0.038f, 0.032f, 2.6f, 0f, -0.04f)
                 .Add(-0.03f, 0.044f * w, 0.042f, 0.045f, 2.8f, 0f, -0.042f)
                 .Add(0.02f, 0.046f * w, 0.04f, 0.04f, 2.8f, 0f, -0.04f)
                 .Add(0.08f, 0.05f * w, 0.032f * toeH, 0.026f * toeH, 3f, 0f, -0.032f * toeH)
                 .Add(0.14f, 0.048f * w, 0.026f * toeH, 0.02f * toeH, 3f, 0f, -0.026f * toeH)
                 .Add(0.18f, 0.038f * w, 0.022f * toeH, 0.016f * toeH, 2.6f, 0f, -0.022f * toeH)
                 .Add(0.2f, 0.02f * w, 0.014f, 0.01f, 2.2f, 0f, -0.018f);
            mb.Tube(upper, 14, 0.02f, _ => skin);

            mb.matrix = Matrix4x4.TRS(new Vector3(x, 0f, 0f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            float sole = dress ? 0.006f : 0.009f;
            var soleP = new BodyProfile { baseMat = look.soleMat };
            soleP.Add(-0.078f, 0.03f * w, sole, sole, 4f, 0f, -sole)
                 .Add(-0.065f, 0.043f * w, sole, sole, 4f, 0f, -sole)
                 .Add(0.02f, 0.048f * w, sole, sole, 4f, 0f, -sole)
                 .Add(0.09f, 0.053f * w, sole, sole, 4f, 0f, -sole)
                 .Add(0.18f, 0.041f * w, sole, sole, 4f, 0f, -sole)
                 .Add(0.205f, 0.02f * w, sole, sole, 3f, 0f, -sole);
            mb.Tube(soleP, 14, 0.03f, _ => skin);
            mb.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// Bullet-proof plates over the suit (8c, the blind Smiler): a front and a back plate on the chest with straps,
        /// a plated belt, shoulder plates and a raised collar. Dull gunmetal, scuffed.
        /// </summary>
        static void Armour(SkinnedBodyBuilder mb, CharacterLook look)
        {
            var steel = RuntimeMaterials.Lit(new Color(0.12f, 0.12f, 0.13f), 0.45f, 0.85f);
            var strap = RuntimeMaterials.Fabric(new Color(0.06f, 0.06f, 0.06f), 0.2f, Fabric.Cotton);
            var chest = Skin.On(Bone.Chest);
            float fz = 0.112f * look.bulk + 0.03f, bz = 0.1f * look.bulk + 0.03f, w = 0.17f * look.bulk;
            mb.Box(steel, new Vector3(0f, 1.3f, fz), new Vector3(w * 1.6f, 0.34f, 0.028f), Quaternion.Euler(-6f, 0f, 0f), chest);
            mb.Box(steel, new Vector3(0f, 1.3f, -bz), new Vector3(w * 1.6f, 0.36f, 0.028f), Quaternion.Euler(6f, 0f, 0f), chest);
            foreach (float s in new[] { -1f, 1f })
            {
                mb.Box(strap, new Vector3(s * w * 0.55f, 1.46f, 0f), new Vector3(0.05f, 0.03f, fz + bz), Quaternion.identity, chest);
                mb.Box(strap, new Vector3(s * w * 0.95f, 1.24f, 0f), new Vector3(0.03f, 0.06f, fz + bz - 0.02f), Quaternion.identity, chest);
                mb.Ellipsoid(steel, new Vector3(s * (0.185f + 0.03f), 1.5f, 0f), new Vector3(0.085f, 0.05f, 0.095f), Quaternion.Euler(0f, 0f, s * -18f), _ => chest, 8, 12);
            }
            // Plated belt over the waist (spine bone), and a raised collar round the neck.
            mb.Torus(steel, new Vector3(0f, 1.06f, 0f), Quaternion.identity, 0.16f * look.bulk, 0.035f, Skin.On(Bone.Spine), 24, 6);
            mb.Torus(steel, new Vector3(0f, 1.56f, 0f), Quaternion.identity, 0.075f, 0.028f, chest, 20, 6);
        }

        // ------------------------------------------------------------------ physics

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
    }
}
