using System;
using UnityEngine;
using Echoes.Rendering;

namespace Echoes.AI
{
    /// <summary>
    /// Heads: a sculpted skull (jaw, chin, nose, brow, eye sockets), simple faces for the unmasked, hair and hats,
    /// and the Purge masks. The Smilers' LED grin is the signature image of the game: it reads from 50 m away in
    /// the dark and blooms on the bodycam sensor. Looters wear hoods with bandanas, rubber pig masks, or porcelain
    /// angel faces under a hood with a wire halo. Everything is added to the character's skinned mesh on the head bone.
    /// </summary>
    public static class MaskFactory
    {
        // Skull ellipsoid in rest space (head bone at y = 1.60; unscaled body).
        static readonly Vector3 C = new Vector3(0f, 1.70f, 0.01f);
        static readonly Vector3 R = new Vector3(0.095f, 0.12f, 0.108f);
        static readonly Skin HeadSkin = Skin.On(Bone.Head);
        static readonly Func<Vector3, Skin> OnHead = _ => HeadSkin;

        public static void Head(SkinnedBodyBuilder mb, CharacterLook look, System.Random rng)
        {
            switch (look.head)
            {
                case HeadKind.LedSmile:
                    LedSmile(mb, look);
                    return;
                case HeadKind.Pig:
                    Skull(mb, look.skin, false);
                    Pig(mb);
                    return;
                case HeadKind.Angel:
                    Skull(mb, look.skin, false);
                    Angel(mb);
                    Hood(mb, look.topMat);
                    return;
                case HeadKind.Hood:
                    Skull(mb, look.skin, true);
                    Eyes(mb, look, false);
                    Bandana(mb, rng);
                    Hood(mb, look.topMat);
                    return;
                default:
                    Skull(mb, look.skin, true, look.closeUp);
                    if (look.closeUp) CloseUpFace(mb, look);
                    else Eyes(mb, look, true);
                    Hair(mb, look);
                    return;
            }
        }

        // ------------------------------------------------------------------ skull and face

        static float G(float x, float y, float sx, float sy) => Mathf.Exp(-(x * x / (sx * sx) + y * y / (sy * sy)));

        /// <summary>Face sculpt as an offset per unit direction (+Z front). Only the front half is shaped.</summary>
        static Vector3 Sculpt(Vector3 d)
        {
            float front = Mathf.Clamp01(d.z * 1.5f);
            float low = Mathf.Clamp01(-d.y);
            var o = Vector3.zero;
            o.x -= d.x * 0.024f * low * low;                                    // jaw narrows to the chin
            o.z += 0.012f * G(d.x, d.y + 0.72f, 0.28f, 0.16f) * front;          // chin
            float nose = G(d.x, d.y + 0.1f, 0.09f, 0.22f) * front;
            o.z += 0.026f * nose;
            o.y -= 0.006f * nose;
            o.z += 0.007f * G(0f, d.y - 0.22f, 1f, 0.08f) * Mathf.Clamp01(1f - Mathf.Abs(d.x) * 1.6f) * front; // brow ridge
            o.z -= 0.01f * (G(d.x - 0.3f, d.y - 0.07f, 0.13f, 0.09f) + G(d.x + 0.3f, d.y - 0.07f, 0.13f, 0.09f)) * front; // eye sockets
            o.x += Mathf.Sign(d.x) * 0.004f * G(Mathf.Abs(d.x) - 0.55f, d.y + 0.08f, 0.15f, 0.12f);         // cheekbones
            o.z -= 0.012f * Mathf.Clamp01(-d.z) * low;                          // back of the neck tucks in
            return o;
        }

        /// <summary>
        /// Extra shape for faces seen at arm's length: nose bridge, tip and wings, philtrum, lip mound, the dip under
        /// the lower lip, cheeks and deeper eye sockets. Needs the denser close-up skull to show.
        /// </summary>
        static Vector3 SculptCloseUp(Vector3 d)
        {
            float front = Mathf.Clamp01(d.z * 1.5f);
            var o = Sculpt(d);
            o.z += (0.006f * G(d.x, d.y - 0.02f, 0.045f, 0.16f)
                  + 0.009f * G(d.x, d.y + 0.24f, 0.06f, 0.06f)
                  + 0.005f * (G(d.x - 0.085f, d.y + 0.27f, 0.035f, 0.035f) + G(d.x + 0.085f, d.y + 0.27f, 0.035f, 0.035f))
                  - 0.0025f * G(d.x, d.y + 0.37f, 0.025f, 0.04f)
                  + 0.004f * G(d.x, d.y + 0.45f, 0.2f, 0.05f)
                  - 0.004f * G(d.x, d.y + 0.57f, 0.16f, 0.035f)
                  + 0.003f * (G(d.x - 0.42f, d.y + 0.18f, 0.14f, 0.12f) + G(d.x + 0.42f, d.y + 0.18f, 0.14f, 0.12f))
                  - 0.003f * (G(d.x - 0.3f, d.y - 0.07f, 0.09f, 0.06f) + G(d.x + 0.3f, d.y - 0.07f, 0.09f, 0.06f))) * front;
            // Age and structure (owner asked for a more realistic face): smile lines from the nose wings to the
            // mouth corners, bags under the eyes, a chin cleft, a forehead that slopes back.
            float ax = Mathf.Abs(d.x), side = Mathf.Sign(d.x);
            o.z += (-0.0028f * G(ax - 0.17f - (-d.y - 0.28f) * 0.5f, d.y + 0.38f, 0.025f, 0.11f)
                  + 0.0018f * G(ax - 0.3f, d.y + 0.03f, 0.09f, 0.03f)
                  - 0.0016f * G(d.x, d.y + 0.76f, 0.025f, 0.05f)
                  - 0.006f * Mathf.Clamp01(d.y - 0.42f)) * front;
            // Hollow temples and a squarer jaw angle under the ears.
            o.x -= side * 0.004f * G(ax - 0.78f, d.y - 0.25f, 0.15f, 0.2f);
            o.x += side * 0.005f * G(ax - 0.8f, d.y + 0.55f, 0.15f, 0.15f);
            return o;
        }

        static Vector3 Dir(float fx, float fy) => new Vector3(fx, fy, Mathf.Sqrt(Mathf.Max(0.05f, 1f - fx * fx - fy * fy))).normalized;

        /// <summary>Point on the sculpted skull for face coordinates (−1..1), pushed out by lift (times scale for shells).</summary>
        static Vector3 SkullPoint(float fx, float fy, float lift, float scale = 1f, bool closeUp = false)
        {
            var d = Dir(fx, fy);
            return C + Vector3.Scale(d, R) * scale + (closeUp ? SculptCloseUp(d) : Sculpt(d)) + d * lift;
        }

        static void Skull(SkinnedBodyBuilder mb, Material skin, bool ears, bool closeUp = false)
        {
            if (closeUp) mb.Ellipsoid(skin, C, R, Quaternion.identity, OnHead, 26, 36, SculptCloseUp);
            else mb.Ellipsoid(skin, C, R, Quaternion.identity, OnHead, 16, 22, Sculpt);
            if (!ears) return;
            foreach (float side in new[] { -1f, 1f })
                mb.Ellipsoid(skin, C + new Vector3(side * 0.094f, -0.008f, -0.006f), new Vector3(0.012f, 0.028f, 0.018f),
                             Quaternion.Euler(0f, side * 20f, 0f), OnHead, 6, 8);
        }

        /// <summary>
        /// A face that holds up at arm's length (owner: Luis looked faceless in the cab): eyeballs with iris and pupil
        /// under upper and lower lids, fuller brows, two lips and the line between them, and a shadow of stubble.
        /// </summary>
        static void CloseUpFace(SkinnedBodyBuilder mb, CharacterLook look)
        {
            var white = RuntimeMaterials.Lit(new Color(0.7f, 0.62f, 0.58f), 0.93f); // a little pink, wet
            var iris = RuntimeMaterials.Lit(new Color(0.07f, 0.04f, 0.02f), 0.9f);
            var irisInner = RuntimeMaterials.Lit(new Color(0.16f, 0.09f, 0.04f), 0.9f);
            var lash = RuntimeMaterials.Lit(new Color(0.015f, 0.01f, 0.008f), 0.3f);
            var pupil = RuntimeMaterials.Lit(new Color(0.004f, 0.004f, 0.004f), 0.95f);
            var line = RuntimeMaterials.Lit(new Color(look.skinTone.r * 0.35f, look.skinTone.g * 0.25f, look.skinTone.b * 0.25f), 0.4f);
            var stubble = RuntimeMaterials.Lit(look.skinTone * 0.68f, 0.25f);
            var brow = look.hair == HairKind.Long || look.hair == HairKind.Short ? look.hairMat : RuntimeMaterials.Lit(new Color(0.03f, 0.02f, 0.015f), 0.2f);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 eye = SkullPoint(side * 0.3f, 0.07f, -0.006f, 1f, true);
                mb.Ellipsoid(white, eye, new Vector3(0.012f, 0.0095f, 0.009f), Quaternion.identity, OnHead, 8, 12);
                mb.Ellipsoid(iris, eye + new Vector3(0f, 0f, 0.0078f), new Vector3(0.0052f, 0.0052f, 0.0016f), Quaternion.identity, OnHead, 6, 10);
                mb.Ellipsoid(irisInner, eye + new Vector3(0f, 0f, 0.0083f), new Vector3(0.0038f, 0.0038f, 0.0013f), Quaternion.identity, OnHead, 6, 10); // dark limbal ring around a lighter iris
                // Lash line along the edge of the upper lid.
                mb.Ellipsoid(lash, eye + new Vector3(0f, 0.0052f, 0.0094f), new Vector3(0.0125f, 0.001f, 0.0016f), Quaternion.identity, OnHead, 4, 10);
                mb.Ellipsoid(pupil, eye + new Vector3(0f, 0f, 0.0092f), new Vector3(0.0024f, 0.0024f, 0.0008f), Quaternion.identity, OnHead, 4, 8);
                // Lids: skin shells over the top and bottom of the eyeball.
                mb.Ellipsoid(look.skin, eye + new Vector3(0f, 0.0015f, 0.0005f), new Vector3(0.0135f, 0.0105f, 0.0098f), Quaternion.identity, OnHead, 8, 12,
                             null, d => d.y > 0.35f && d.z > -0.2f);
                mb.Ellipsoid(look.skin, eye + new Vector3(0f, -0.001f, 0.0003f), new Vector3(0.0132f, 0.0102f, 0.0096f), Quaternion.identity, OnHead, 8, 12,
                             null, d => d.y < -0.55f && d.z > -0.2f);
                // Brows as a row of short tufts: thick at the inner end, thinning toward the temple.
                for (int k = 0; k < 6; k++)
                {
                    float u = k / 5f;
                    float fx = side * (0.17f + u * 0.26f), fy = 0.205f + 0.03f * Mathf.Sin(u * Mathf.PI * 0.8f);
                    mb.Ellipsoid(brow, SkullPoint(fx, fy, 0.0015f, 1f, true), new Vector3(0.0065f, 0.0042f - u * 0.0018f, 0.0045f),
                                 Quaternion.Euler(0f, side * 18f, side * (-25f + u * 30f)), OnHead, 3, 6);
                }
            }
            mb.Ellipsoid(look.lips, SkullPoint(0f, -0.44f, -0.0035f, 1f, true), new Vector3(0.021f, 0.0045f, 0.0065f), Quaternion.identity, OnHead, 6, 10);
            mb.Ellipsoid(look.lips, SkullPoint(0f, -0.505f, -0.003f, 1f, true), new Vector3(0.019f, 0.006f, 0.007f), Quaternion.identity, OnHead, 6, 10);
            mb.Ellipsoid(line, SkullPoint(0f, -0.472f, 0.0005f, 1f, true), new Vector3(0.0185f, 0.0011f, 0.003f), Quaternion.identity, OnHead, 4, 8);
            // Stubble: the same skull mesh lifted 1.4 mm (identical tessellation, so nothing pokes through), jaw and upper lip only.
            mb.Ellipsoid(stubble, C, R, Quaternion.identity, OnHead, 26, 36, d => SculptCloseUp(d) + d * 0.0014f,
                         d => d.y < -0.2f && d.y > -0.95f && d.z > 0.05f && Mathf.Abs(d.x) < 0.8f
                              && !(Mathf.Abs(d.x) < 0.22f && d.y > -0.56f && d.y < -0.4f) && !(Mathf.Abs(d.x) < 0.14f && d.y > -0.33f));
        }

        static void Eyes(SkinnedBodyBuilder mb, CharacterLook look, bool fullFace)
        {
            var eye = RuntimeMaterials.Lit(new Color(0.02f, 0.018f, 0.016f), 0.9f);
            var brow = look.hair == HairKind.Long || look.hair == HairKind.Short ? look.hairMat : RuntimeMaterials.Lit(new Color(0.03f, 0.02f, 0.015f), 0.2f);
            foreach (float side in new[] { -1f, 1f })
            {
                mb.Ellipsoid(eye, SkullPoint(side * 0.3f, 0.07f, -0.002f), new Vector3(0.011f, 0.007f, 0.006f), Quaternion.identity, OnHead, 5, 8);
                mb.Ellipsoid(brow, SkullPoint(side * 0.3f, 0.21f, 0.001f), new Vector3(0.019f, 0.0035f, 0.005f), Quaternion.Euler(0f, side * 18f, side * -6f), OnHead, 4, 8);
            }
            if (fullFace)
                mb.Ellipsoid(look.lips, SkullPoint(0f, -0.47f, -0.002f), new Vector3(0.021f, 0.0055f, 0.006f), Quaternion.identity, OnHead, 4, 8);
        }

        // ------------------------------------------------------------------ hair and hats

        static void Hair(SkinnedBodyBuilder mb, CharacterLook look)
        {
            switch (look.hair)
            {
                case HairKind.Short:
                case HairKind.Long:
                {
                    // Hairline: high at the forehead, down to the nape at the back, clear of the ears.
                    Func<Vector3, bool> keep = d =>
                        d.y > Mathf.Lerp(-0.55f, 0.42f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.3f, 0.75f, d.z))) &&
                        !(Mathf.Abs(d.x) > 0.75f && d.y < 0.2f && d.z > -0.4f);
                    mb.Ellipsoid(look.hairMat, C + new Vector3(0f, 0.008f, -0.004f), R * 1.07f, Quaternion.identity, OnHead, 14, 22,
                                 d => Sculpt(d) * 0.4f + d * 0.003f * Mathf.Sin(d.x * 23f + d.z * 17f), keep);
                    if (look.hair == HairKind.Long)
                    {
                        // Curtain down the back to the shoulders; the lower part follows the chest.
                        Func<Vector3, Skin> drape = p => Skin.Blend(Bone.Head, Bone.Chest, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.62f, 1.5f, p.y)));
                        mb.Ellipsoid(look.hairMat, C + new Vector3(0f, -0.085f, -0.04f), new Vector3(0.1f, 0.15f, 0.07f), Quaternion.Euler(-8f, 0f, 0f), drape, 10, 16,
                                     null, d => d.z < 0.25f);
                    }
                    break;
                }
                case HairKind.Beanie:
                    mb.Ellipsoid(look.hairMat, C + new Vector3(0f, 0.018f, -0.004f), R * 1.1f, Quaternion.identity, OnHead, 12, 20,
                                 d => Sculpt(d) * 0.3f, d => d.y > Mathf.Lerp(-0.15f, 0.2f, Mathf.InverseLerp(-0.5f, 0.8f, d.z)));
                    mb.Ellipsoid(look.hairMat, C + new Vector3(0f, 0.018f, -0.004f), R * 1.14f, Quaternion.identity, OnHead, 12, 20,
                                 d => Sculpt(d) * 0.3f, d =>
                                 {
                                     float edge = Mathf.Lerp(-0.15f, 0.2f, Mathf.InverseLerp(-0.5f, 0.8f, d.z));
                                     return d.y > edge && d.y < edge + 0.22f; // folded cuff
                                 });
                    break;
                case HairKind.Cap:
                    mb.Ellipsoid(look.hairMat, C + new Vector3(0f, 0.022f, -0.004f), R * 1.08f, Quaternion.identity, OnHead, 12, 20, null, d => d.y > 0.22f);
                    mb.Box(look.hairMat, C + new Vector3(0f, 0.1f, 0.135f), new Vector3(0.17f, 0.007f, 0.085f), Quaternion.Euler(-10f, 0f, 0f), HeadSkin);
                    break;
            }
        }

        // ------------------------------------------------------------------ masks

        static void LedSmile(SkinnedBodyBuilder mb, CharacterLook look)
        {
            // Smooth black full-head mask; LEDs sit on its surface.
            var shell = RuntimeMaterials.Fabric(new Color(0.02f, 0.02f, 0.02f), 0.85f, Fabric.Rubber);
            Vector3 rs = R * 1.04f;
            mb.Ellipsoid(shell, C, rs, Quaternion.identity, OnHead, 16, 22, d => Sculpt(d) * 0.35f);
            Color c = look.ledColor;
            var led = RuntimeMaterials.Lit(c * 0.3f, 0.6f, 0f, c * 12f);
            Vector3 Face(float x, float y)
            {
                float z = Mathf.Sqrt(Mathf.Max(0.05f, 1f - x * x - y * y));
                return C + new Vector3(x * rs.x, y * rs.y, z * rs.z + 0.008f); // clears the shell's nose bump
            }
            // Grin: 13 LEDs along an upturned arc.
            for (int i = 0; i < 13; i++)
            {
                float t = i / 12f * 2f - 1f;
                var p = Face(t * 0.62f, -0.28f - (1f - t * t) * 0.2f);
                mb.Box(led, p, new Vector3(0.018f, 0.013f, 0.007f), Quaternion.LookRotation(p - C) * Quaternion.Euler(0f, 0f, Mathf.Atan(-2f * t * 0.2f / 0.62f) * Mathf.Rad2Deg), HeadSkin);
            }
            // Slanted "happy" eyes.
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i < 4; i++)
                {
                    float t = i / 3f;
                    var p = Face(side * (0.22f + t * 0.24f), 0.22f + Mathf.Sin(t * Mathf.PI) * 0.09f);
                    mb.Box(led, p, new Vector3(0.018f, 0.012f, 0.007f), Quaternion.LookRotation(p - C), HeadSkin);
                }
        }

        static void Hood(SkinnedBodyBuilder mb, Material cloth)
        {
            // Shell offset back, open around the face; a drape joins it to the hoodie's neckline.
            mb.Ellipsoid(cloth, C + new Vector3(0f, 0.012f, -0.026f), Vector3.Scale(R, new Vector3(1.24f, 1.17f, 1.22f)), Quaternion.identity, OnHead, 14, 22,
                         d => d * 0.004f * Mathf.Sin(d.y * 19f + d.x * 7f),
                         d => !(d.z > 0.3f && Mathf.Abs(d.x) < 0.74f && d.y > -0.72f && d.y < 0.64f));
            Func<Vector3, Skin> drape = p => Skin.Blend(Bone.Head, Bone.Chest, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.63f, 1.52f, p.y)));
            mb.Ellipsoid(cloth, C + new Vector3(0f, -0.115f, -0.055f), new Vector3(0.125f, 0.085f, 0.095f), Quaternion.identity, drape, 8, 16, null, d => d.z < 0.3f);
        }

        static void Bandana(SkinnedBodyBuilder mb, System.Random rng)
        {
            Color[] colours = { new Color(0.25f, 0.03f, 0.03f), new Color(0.03f, 0.03f, 0.03f), new Color(0.05f, 0.08f, 0.2f) };
            var cloth = RuntimeMaterials.Fabric(colours[rng.Next(colours.Length)], 0.2f, Fabric.Cotton);
            // A slightly inflated copy of the face, so it drapes over the nose; the front corner hangs below the chin.
            mb.Ellipsoid(cloth, C, R * 1.07f, Quaternion.identity, OnHead, 14, 22,
                         d => Sculpt(d) * 1.15f + Vector3.down * 0.035f * G(d.x, d.y + 1f, 0.35f, 0.45f) * Mathf.Clamp01(d.z * 2f),
                         d => d.y < -0.02f && d.z > -0.45f);
        }

        static void Pig(SkinnedBodyBuilder mb)
        {
            var rubber = RuntimeMaterials.Fabric(new Color(0.5f, 0.26f, 0.27f), 0.55f, Fabric.Rubber);
            var dark = RuntimeMaterials.Lit(new Color(0.015f, 0.01f, 0.01f), 0.4f);
            mb.Ellipsoid(rubber, C, R * 1.07f, Quaternion.identity, OnHead, 16, 22, d => Sculpt(d) * 0.4f + d * 0.006f * Mathf.Clamp01(-d.y));

            // Snout: a short tube pointing forward, built along local Y then turned to +Z.
            Vector3 snoutBase = C + new Vector3(0f, -0.03f, 0.1f);
            mb.matrix = Matrix4x4.TRS(snoutBase, Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            var snout = new BodyProfile { baseMat = rubber };
            snout.Add(0f, 0.045f, 0.036f, 0.036f, 2.2f).Add(0.025f, 0.042f, 0.033f, 0.033f, 2.4f).Add(0.045f, 0.038f, 0.03f, 0.03f, 2.6f).Add(0.05f, 0.03f, 0.024f, 0.024f, 2.4f);
            mb.Tube(snout, 14, 0.015f, OnHead, false, true);
            mb.matrix = Matrix4x4.identity;
            foreach (float side in new[] { -1f, 1f })
            {
                mb.Ellipsoid(dark, snoutBase + new Vector3(side * 0.014f, 0f, 0.05f), new Vector3(0.007f, 0.011f, 0.004f), Quaternion.identity, OnHead, 4, 8);
                // Floppy ears, tilted out and forward.
                mb.Ellipsoid(rubber, C + new Vector3(side * 0.075f, 0.105f, 0.0f), new Vector3(0.04f, 0.052f, 0.011f),
                             Quaternion.Euler(-25f, side * -15f, side * -38f), OnHead, 6, 10, d => new Vector3(0f, 0f, 0.012f * (1f - d.y * d.y)));
                // Eye holes.
                mb.Ellipsoid(dark, SkullPoint(side * 0.32f, 0.12f, 0.004f, 1.07f), new Vector3(0.016f, 0.011f, 0.006f), Quaternion.identity, OnHead, 4, 8);
            }
            mb.Ellipsoid(dark, SkullPoint(0f, -0.55f, 0.004f, 1.07f), new Vector3(0.03f, 0.004f, 0.006f), Quaternion.Euler(0f, 0f, 0f), OnHead, 4, 8);
        }

        static void Angel(SkinnedBodyBuilder mb)
        {
            var porcelain = RuntimeMaterials.Fabric(new Color(0.75f, 0.73f, 0.7f), 0.8f, Fabric.Porcelain);
            var dark = RuntimeMaterials.Lit(new Color(0.03f, 0.025f, 0.025f), 0.5f);
            var rose = RuntimeMaterials.Lit(new Color(0.45f, 0.2f, 0.22f), 0.7f);
            mb.Ellipsoid(porcelain, C + new Vector3(0f, 0f, 0.004f), R * 1.05f, Quaternion.identity, OnHead, 16, 22, d => Sculpt(d) * 1.05f,
                         d => d.z > -0.05f && d.y > -0.95f);
            // Closed, downturned eyes and small painted lips: serene, which is the point.
            foreach (float side in new[] { -1f, 1f })
                mb.Ellipsoid(dark, SkullPoint(side * 0.3f, 0.06f, 0.006f, 1.05f), new Vector3(0.019f, 0.0022f, 0.004f), Quaternion.Euler(0f, side * 15f, side * 12f), OnHead, 4, 8);
            mb.Ellipsoid(rose, SkullPoint(0f, -0.47f, 0.005f, 1.05f), new Vector3(0.017f, 0.0045f, 0.005f), Quaternion.identity, OnHead, 4, 8);

            // Wire halo above the hood, faintly glowing so it reads on the bodycam at night.
            var gold = RuntimeMaterials.Lit(new Color(0.8f, 0.6f, 0.25f), 0.75f, 1f, new Color(1f, 0.75f, 0.3f) * 1.4f);
            mb.Torus(gold, C + new Vector3(0f, 0.2f, -0.02f), Quaternion.Euler(-14f, 0f, 0f), 0.095f, 0.0065f, HeadSkin);
            mb.Box(gold, C + new Vector3(0f, 0.16f, -0.105f), new Vector3(0.003f, 0.09f, 0.003f), Quaternion.Euler(-30f, 0f, 0f), HeadSkin);
        }
    }
}
