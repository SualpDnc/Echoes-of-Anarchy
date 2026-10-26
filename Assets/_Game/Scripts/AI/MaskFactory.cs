using UnityEngine;
using Echoes.Rendering;

namespace Echoes.AI
{
    /// <summary>
    /// Faces are never modelled — Purge masks, hoods and the dark do that work. The Smilers' LED grin is the
    /// signature image of the game: it reads from 50 m away in the dark and blooms on the bodycam sensor.
    /// </summary>
    public static class MaskFactory
    {
        // Skull ellipsoid on the head bone (matches HumanoidBuilder).
        static readonly Vector3 C = new Vector3(0f, 0.1f, 0.01f);
        static readonly Vector3 R = new Vector3(0.1f, 0.1225f, 0.1125f);

        public static void Apply(Faction f, Transform head, System.Random rng)
        {
            switch (f)
            {
                case Faction.Smiler: LedSmile(head, rng); break;
                case Faction.Looter: HoodAndBandana(head, rng); break;
                default: Hair(head, rng); break;
            }
        }

        /// <summary>Point on the front of the skull at normalized face coords (x,y in −1..1).</summary>
        static Vector3 Face(float x, float y, float lift = 0.004f)
        {
            float k = 1f - x * x - y * y;
            float z = Mathf.Sqrt(Mathf.Max(0.05f, k));
            return C + new Vector3(x * R.x, y * R.y, z * R.z + lift);
        }

        static void LedSmile(Transform head, System.Random rng)
        {
            Color[] palette = { new Color(0.2f, 1f, 1f), new Color(1f, 0.15f, 0.6f), new Color(1f, 1f, 1f), new Color(0.4f, 1f, 0.3f) };
            Color c = palette[rng.Next(palette.Length)];
            var led = RuntimeMaterials.Lit(c * 0.3f, 0.6f, 0f, c * 12f);
            // Grin: 13 LEDs along an upturned arc.
            for (int i = 0; i < 13; i++)
            {
                float t = i / 12f * 2f - 1f;
                float x = t * 0.62f, y = -0.28f - (1f - t * t) * 0.2f;
                var p = Face(x, y);
                var seg = PrimitiveMeshes.Part(head, "LED", PrimitiveType.Cube, p, new Vector3(0.018f, 0.013f, 0.007f), led, null, false);
                seg.transform.localRotation = Quaternion.LookRotation(p - C) * Quaternion.Euler(0f, 0f, Mathf.Atan(-2f * t * 0.2f / 0.62f) * Mathf.Rad2Deg);
            }
            // Slanted "happy" eyes.
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i < 4; i++)
                {
                    float t = i / 3f;
                    var p = Face(side * (0.22f + t * 0.24f), 0.22f + Mathf.Sin(t * Mathf.PI) * 0.09f);
                    var seg = PrimitiveMeshes.Part(head, "LED", PrimitiveType.Cube, p, new Vector3(0.018f, 0.012f, 0.007f), led, null, false);
                    seg.transform.localRotation = Quaternion.LookRotation(p - C);
                }
        }

        static void HoodAndBandana(Transform head, System.Random rng)
        {
            Color[] bandanas = { new Color(0.25f, 0.03f, 0.03f), new Color(0.03f, 0.03f, 0.03f), new Color(0.05f, 0.08f, 0.2f) };
            var bandana = RuntimeMaterials.Lit(bandanas[rng.Next(bandanas.Length)], 0.25f);
            var skull = head.Find("Skull");
            var hoodMat = skull != null ? skull.GetComponent<Renderer>().sharedMaterial : bandana;
            // Hood: a bigger shell offset back, leaving the face open.
            PrimitiveMeshes.Part(head, "Hood", PrimitiveType.Sphere, C + new Vector3(0f, 0.012f, -0.03f), new Vector3(0.235f, 0.275f, 0.25f), hoodMat);
            // Bandana over nose and mouth.
            PrimitiveMeshes.Part(head, "Bandana", PrimitiveType.Sphere, C + new Vector3(0f, -0.035f, 0.025f), new Vector3(0.205f, 0.13f, 0.215f), bandana);
            if (skull != null)
                skull.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Lit(new Color(0.3f, 0.2f, 0.15f), 0.35f); // the eye strip
        }

        static void Hair(Transform head, System.Random rng)
        {
            Color[] hair = { new Color(0.02f, 0.015f, 0.01f), new Color(0.12f, 0.07f, 0.03f), new Color(0.3f, 0.25f, 0.18f) };
            var m = RuntimeMaterials.Lit(hair[rng.Next(hair.Length)], 0.3f);
            PrimitiveMeshes.Part(head, "Hair", PrimitiveType.Sphere, C + new Vector3(0f, 0.035f, -0.012f), new Vector3(0.208f, 0.2f, 0.232f), m);
        }
    }
}
