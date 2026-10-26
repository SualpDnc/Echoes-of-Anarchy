using System;
using UnityEngine;

namespace Echoes.AI
{
    /// <summary>
    /// Garment details that sit on top of the lofted body: pockets, drawstrings, zips, lapels, the suit's shirt
    /// front and tie. They are patches that hug the body profile, so they follow its curvature and skinning.
    /// Angles: π/2 is the front, −π/2 the back, 0 the character's right side.
    /// </summary>
    public static class OutfitDetails
    {
        const float Front = Mathf.PI * 0.5f;

        public static void Add(SkinnedBodyBuilder mb, CharacterLook look, BodyProfile torso, BodyProfile[] legs,
                               Func<Vector3, Skin> torsoSkin, Func<Vector3, Skin> legSkinL, Func<Vector3, Skin> legSkinR)
        {
            switch (look.top)
            {
                case TopKind.Hoodie:
                    // Kangaroo pocket, narrower at the top.
                    mb.Patch(torso, 0.95f, 1.11f, y => Around(Front, Mathf.Lerp(0.5f, 0.36f, Mathf.InverseLerp(0.95f, 1.11f, y))), 0.005f, look.topMat, torsoSkin);
                    foreach (float side in new[] { -1f, 1f })
                        Strand(mb, torso, Front - side * 0.22f, 1.36f, 1.53f, 0.0035f, look.accentMat, torsoSkin);
                    break;

                case TopKind.WorkJacket:
                    mb.Patch(torso, 0.905f, 1.55f, _ => Around(Front, 0.025f), 0.0025f, look.accentMat, torsoSkin, 2, 10);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        float c = Front + side * 0.55f;
                        mb.Patch(torso, 1.3f, 1.4f, _ => Around(c, 0.17f), 0.005f, look.topMat, torsoSkin, 4, 3);
                        mb.Patch(torso, 1.385f, 1.415f, _ => Around(c, 0.18f), 0.009f, look.topMat, torsoSkin, 4, 1);
                    }
                    break;

                case TopKind.OpenJacket:
                {
                    Func<float, float> w = y => Mathf.Lerp(0.22f, 0.32f, Mathf.InverseLerp(0.9f, 1.53f, y));
                    mb.Patch(torso, 0.9f, 1.53f, y => Around(Front, w(y)), 0.0015f, look.innerMat, torsoSkin, 6, 10);
                    mb.Patch(torso, 0.9f, 1.53f, y => new Vector2(Front + w(y), Front + w(y) + 0.07f), 0.004f, look.topMat, torsoSkin, 2, 10);
                    mb.Patch(torso, 0.9f, 1.53f, y => new Vector2(Front - w(y) - 0.07f, Front - w(y)), 0.004f, look.topMat, torsoSkin, 2, 10);
                    break;
                }

                case TopKind.Suit:
                {
                    // White shirt in a V, the tie (in the mask's LED colour) and lapels along the V edges.
                    Func<float, float> v = y => Mathf.Lerp(0.05f, 0.45f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.18f, 1.49f, y)));
                    mb.Patch(torso, 1.18f, 1.495f, y => Around(Front, v(y)), 0.0015f, look.innerMat, torsoSkin, 6, 8);
                    mb.Patch(torso, 1.16f, 1.48f, y => Around(Front, TieHalfWidth(y)), 0.0045f, look.accentMat, torsoSkin, 3, 10);
                    mb.Patch(torso, 1.19f, 1.49f, y => new Vector2(Front + v(y) - 0.01f, Front + v(y) + 0.17f), 0.004f, look.topMat, torsoSkin, 3, 8);
                    mb.Patch(torso, 1.19f, 1.49f, y => new Vector2(Front - v(y) - 0.17f, Front - v(y) + 0.01f), 0.004f, look.topMat, torsoSkin, 3, 8);
                    foreach (float side in new[] { -1f, 1f })
                        mb.Patch(torso, 0.95f, 0.975f, _ => Around(Front + side * 0.85f, 0.25f), 0.004f, look.topMat, torsoSkin, 4, 1);
                    var button = Echoes.Rendering.RuntimeMaterials.Lit(new Color(0.01f, 0.01f, 0.01f), 0.7f);
                    foreach (float y in new[] { 1.0f, 1.1f })
                    {
                        torso.Cover(y, out _, out float t);
                        mb.Box(button, torso.Point(y, Front, t + 0.002f), new Vector3(0.012f, 0.012f, 0.005f), Quaternion.identity, torsoSkin(new Vector3(0f, y, 0f)));
                    }
                    break;
                }
            }

            if (look.pants == PantsKind.Cargo)
            {
                for (int i = 0; i < 2; i++)
                {
                    float c = i == 0 ? Mathf.PI : 0f; // outer side of each thigh
                    var skin = i == 0 ? legSkinL : legSkinR;
                    mb.Patch(legs[i], 0.56f, 0.72f, _ => Around(c, 0.55f), 0.006f, look.pantsMat, skin, 4, 4);
                    mb.Patch(legs[i], 0.70f, 0.735f, _ => Around(c, 0.58f), 0.01f, look.pantsMat, skin, 4, 1);
                }
            }
        }

        static Vector2 Around(float centre, float half) => new Vector2(centre - half, centre + half);

        static float TieHalfWidth(float y)
        {
            if (y < 1.19f) return Mathf.Lerp(0.005f, 0.075f, Mathf.InverseLerp(1.16f, 1.19f, y)); // the point
            if (y < 1.44f) return Mathf.Lerp(0.075f, 0.055f, Mathf.InverseLerp(1.19f, 1.44f, y));
            return 0.05f; // knot
        }

        /// <summary>A thin cord hanging on the surface (drawstring) between two heights at a fixed angle.</summary>
        static void Strand(SkinnedBodyBuilder mb, BodyProfile body, float theta, float y0, float y1, float r, Material mat, Func<Vector3, Skin> skin)
        {
            var p = new BodyProfile { baseMat = mat };
            for (int i = 0; i <= 3; i++)
            {
                float y = Mathf.Lerp(y0, y1, i / 3f);
                body.Cover(y, out _, out float t);
                Vector3 s = body.Point(y, theta, t + r + 0.001f);
                p.Add(y, r, r, r, 2f, s.x, s.z);
            }
            mb.Tube(p, 5, 0.04f, skin);
        }
    }
}
