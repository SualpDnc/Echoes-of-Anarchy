using UnityEngine;
using Echoes.Rendering;

namespace Echoes.AI
{
    public enum TopKind { Hoodie, TShirt, Sweater, OpenJacket, WorkJacket, Suit }
    public enum PantsKind { Jeans, Cargo, Chinos, Sweatpants, Suit }
    public enum ShoeKind { Sneaker, Boot, Dress }
    public enum HeadKind { Face, LedSmile, Hood, Pig, Angel }
    public enum HairKind { None, Short, Long, Beanie, Cap }

    /// <summary>
    /// Everything that decides how one character looks: proportions, garments, head/mask and the materials for
    /// them. Pick() rolls the kinds for a faction, Dress() turns them into materials; tools (the lineup) can
    /// override kinds between the two calls.
    /// </summary>
    public class CharacterLook
    {
        public Faction faction;

        public float height = 1f, bulk = 1f, belly;
        public bool feminine;

        public TopKind top;
        public PantsKind pants;
        public ShoeKind shoes;
        public HeadKind head;
        public HairKind hair;
        public bool shortSleeves, gloves, hoodUp;
        /// <summary>
        /// The detailed face (iris and lids, lips, nostrils, stubble, shaped ears). Dress() turns it on for every bare or
        /// half-covered face: the owner asked for every character as realistic as possible. Costs more vertices.
        /// </summary>
        public bool closeUp;

        public Color skinTone, ledColor;
        public Color irisColor = new Color(0.16f, 0.09f, 0.04f);
        /// <summary>Extra bone structure and folds on a detailed face, 0..1 (Luis: 1). Costs a denser skull.</summary>
        public float definition;
        /// <summary>8c, the blind Smiler: a cloth band tied over the mask's eyes, and bullet-proof plates over the suit.</summary>
        public bool blindfold, armor;
        public Material skin, lips, topMat, innerMat, accentMat, pantsMat, shoeMat, soleMat, gloveMat, hairMat, beltMat;

        public static CharacterLook Random(Faction f, System.Random rng)
        {
            var look = Pick(f, rng);
            look.Dress(rng);
            return look;
        }

        static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static T Of<T>(System.Random rng, params T[] items) => items[rng.Next(items.Length)];

        public static CharacterLook Pick(Faction f, System.Random rng)
        {
            var l = new CharacterLook { faction = f };
            l.feminine = rng.NextDouble() < (f == Faction.Civilian ? 0.5 : f == Faction.Smiler ? 0.3 : 0.25);
            l.height = l.feminine ? R(rng, 0.92f, 1.0f) : R(rng, 0.95f, 1.06f);
            l.bulk = l.feminine ? R(rng, 0.9f, 1.05f) : R(rng, 0.92f, 1.16f);
            l.belly = f == Faction.Smiler ? 0f : Mathf.Max(0f, R(rng, -0.6f, 1f)) * (l.feminine ? 0.4f : 1f);
            switch (f)
            {
                case Faction.Smiler:
                    l.top = TopKind.Suit; l.pants = PantsKind.Suit; l.shoes = ShoeKind.Dress;
                    l.head = HeadKind.LedSmile; l.hair = HairKind.None; l.gloves = true;
                    break;
                case Faction.Looter:
                    l.top = rng.NextDouble() < 0.65 ? TopKind.Hoodie : TopKind.WorkJacket;
                    l.pants = Of(rng, PantsKind.Jeans, PantsKind.Cargo, PantsKind.Sweatpants, PantsKind.Jeans);
                    l.shoes = rng.NextDouble() < 0.7 ? ShoeKind.Sneaker : ShoeKind.Boot;
                    double m = rng.NextDouble();
                    l.head = m < 0.45 ? HeadKind.Hood : m < 0.65 ? HeadKind.Pig : m < 0.85 ? HeadKind.Angel : HeadKind.Face;
                    l.hair = l.head == HeadKind.Face ? Of(rng, HairKind.Beanie, HairKind.Cap, HairKind.Short) : HairKind.None;
                    l.gloves = rng.NextDouble() < 0.3;
                    break;
                default:
                    l.top = Of(rng, TopKind.TShirt, TopKind.Sweater, TopKind.OpenJacket);
                    l.pants = rng.NextDouble() < 0.6 ? PantsKind.Jeans : PantsKind.Chinos;
                    l.shoes = rng.NextDouble() < 0.6 ? ShoeKind.Sneaker : ShoeKind.Dress;
                    l.head = HeadKind.Face;
                    l.hair = l.feminine ? (rng.NextDouble() < 0.75 ? HairKind.Long : HairKind.Short) : Of(rng, HairKind.Short, HairKind.Short, HairKind.Cap, HairKind.None);
                    break;
            }
            l.shortSleeves = l.top == TopKind.TShirt && rng.NextDouble() < 0.8;
            return l;
        }

        static Material M(Color c, float smooth, Fabric fab) => RuntimeMaterials.Fabric(c, smooth, fab);

        public void Dress(System.Random rng)
        {
            // Hoods only go up over a mask that leaves the back of the head bare.
            hoodUp = head == HeadKind.Hood || head == HeadKind.Angel;
            if (hoodUp && top != TopKind.Hoodie) top = TopKind.Hoodie;

            Color[] skins = { new Color(0.45f, 0.3f, 0.22f), new Color(0.3f, 0.19f, 0.13f), new Color(0.6f, 0.43f, 0.33f), new Color(0.18f, 0.11f, 0.08f) };
            skinTone = Of(rng, skins);
            skin = M(skinTone, 0.38f, Fabric.Skin);
            lips = feminine ? M(new Color(skinTone.r * 0.85f + 0.04f, skinTone.g * 0.45f, skinTone.b * 0.48f), 0.55f, Fabric.Skin)
                            : M(new Color(skinTone.r * 0.8f, skinTone.g * 0.55f, skinTone.b * 0.55f), 0.45f, Fabric.Skin);
            // Every face that shows gets the detailed build. No rng draws here: adding one would change every look
            // rolled after this one (the gas station's looters and clerk come from one shared rng).
            if (head == HeadKind.Face || head == HeadKind.Hood) closeUp = true;
            irisColor = skinTone.r > 0.55f ? new Color(0.1f, 0.13f, 0.16f)   // the lightest skin: grey-blue eyes
                      : skinTone.r > 0.4f ? new Color(0.13f, 0.1f, 0.05f)   // hazel
                      : new Color(0.1f, 0.055f, 0.025f);                    // brown
            Color[] leds = { new Color(0.2f, 1f, 1f), new Color(1f, 0.15f, 0.6f), new Color(1f, 1f, 1f), new Color(0.4f, 1f, 0.3f) };
            ledColor = Of(rng, leds);

            switch (top)
            {
                case TopKind.Suit:
                    topMat = M(new Color(0.015f, 0.015f, 0.018f), 0.3f, Fabric.Wool);
                    innerMat = M(new Color(0.62f, 0.62f, 0.6f), 0.25f, Fabric.Cotton);
                    accentMat = M(ledColor * 0.22f, 0.55f, Fabric.Cotton); // tie in the mask's colour
                    break;
                case TopKind.Hoodie:
                    topMat = M(Of(rng, new Color(0.08f, 0.08f, 0.085f), new Color(0.12f, 0.1f, 0.05f), new Color(0.05f, 0.07f, 0.04f), new Color(0.15f, 0.03f, 0.03f), new Color(0.2f, 0.2f, 0.21f)), 0.15f, Fabric.Knit);
                    accentMat = M(new Color(0.5f, 0.5f, 0.48f), 0.2f, Fabric.Cotton); // drawstrings
                    break;
                case TopKind.WorkJacket:
                    topMat = M(Of(rng, new Color(0.22f, 0.16f, 0.08f), new Color(0.07f, 0.08f, 0.05f), new Color(0.04f, 0.04f, 0.045f)), 0.22f, Fabric.Cotton);
                    accentMat = RuntimeMaterials.Lit(new Color(0.2f, 0.2f, 0.2f), 0.6f, 0.8f); // zip
                    break;
                case TopKind.OpenJacket:
                    bool leather = rng.NextDouble() < 0.4;
                    topMat = leather ? M(new Color(0.05f, 0.035f, 0.025f), 0.5f, Fabric.Leather) : M(Of(rng, new Color(0.06f, 0.09f, 0.16f), new Color(0.12f, 0.12f, 0.1f)), 0.25f, Fabric.Cotton);
                    innerMat = M(Of(rng, new Color(0.45f, 0.44f, 0.4f), new Color(0.3f, 0.08f, 0.07f), new Color(0.05f, 0.05f, 0.05f)), 0.2f, Fabric.Cotton);
                    break;
                case TopKind.Sweater:
                    topMat = M(Of(rng, new Color(0.3f, 0.26f, 0.2f), new Color(0.1f, 0.14f, 0.22f), new Color(0.25f, 0.08f, 0.07f), new Color(0.16f, 0.18f, 0.13f)), 0.12f, Fabric.Knit);
                    break;
                default:
                    topMat = M(Of(rng, new Color(0.35f, 0.33f, 0.3f), new Color(0.08f, 0.15f, 0.3f), new Color(0.3f, 0.12f, 0.1f), new Color(0.2f, 0.22f, 0.15f)), 0.2f, Fabric.Cotton);
                    break;
            }

            switch (pants)
            {
                case PantsKind.Suit: pantsMat = topMat; break;
                case PantsKind.Jeans: pantsMat = M(Of(rng, new Color(0.04f, 0.06f, 0.12f), new Color(0.07f, 0.1f, 0.17f), new Color(0.03f, 0.03f, 0.035f)), 0.2f, Fabric.Denim); break;
                case PantsKind.Cargo: pantsMat = M(Of(rng, new Color(0.08f, 0.09f, 0.06f), new Color(0.05f, 0.05f, 0.05f), new Color(0.16f, 0.13f, 0.09f)), 0.2f, Fabric.Cotton); break;
                case PantsKind.Sweatpants: pantsMat = M(Of(rng, new Color(0.15f, 0.15f, 0.16f), new Color(0.03f, 0.03f, 0.035f)), 0.12f, Fabric.Knit); break;
                default: pantsMat = M(Of(rng, new Color(0.2f, 0.16f, 0.1f), new Color(0.12f, 0.1f, 0.08f)), 0.22f, Fabric.Cotton); break;
            }
            beltMat = M(new Color(0.03f, 0.02f, 0.015f), 0.5f, Fabric.Leather);

            switch (shoes)
            {
                case ShoeKind.Dress:
                    shoeMat = M(new Color(0.012f, 0.01f, 0.01f), 0.75f, Fabric.Leather);
                    soleMat = RuntimeMaterials.Lit(new Color(0.02f, 0.015f, 0.01f), 0.3f);
                    break;
                case ShoeKind.Boot:
                    shoeMat = M(Of(rng, new Color(0.14f, 0.08f, 0.04f), new Color(0.03f, 0.03f, 0.03f)), 0.4f, Fabric.Leather);
                    soleMat = RuntimeMaterials.Lit(new Color(0.02f, 0.02f, 0.02f), 0.2f);
                    break;
                default:
                    shoeMat = M(Of(rng, new Color(0.5f, 0.5f, 0.5f), new Color(0.04f, 0.04f, 0.045f), new Color(0.25f, 0.05f, 0.04f), new Color(0.1f, 0.12f, 0.2f)), 0.35f, Fabric.Cotton);
                    soleMat = RuntimeMaterials.Lit(new Color(0.6f, 0.6f, 0.58f), 0.3f);
                    break;
            }

            gloveMat = faction == Faction.Smiler ? M(new Color(0.7f, 0.7f, 0.68f), 0.3f, Fabric.Cotton)
                     : gloves ? M(new Color(0.03f, 0.03f, 0.03f), 0.4f, Fabric.Leather) : skin;
            Color[] hairs = { new Color(0.02f, 0.015f, 0.01f), new Color(0.12f, 0.07f, 0.03f), new Color(0.3f, 0.25f, 0.18f), new Color(0.04f, 0.03f, 0.025f) };
            hairMat = hair == HairKind.Beanie || hair == HairKind.Cap
                ? M(Of(rng, new Color(0.04f, 0.04f, 0.04f), new Color(0.2f, 0.05f, 0.04f), new Color(0.07f, 0.09f, 0.05f)), 0.15f, hair == HairKind.Beanie ? Fabric.Knit : Fabric.Cotton)
                : M(Of(rng, hairs), 0.35f, Fabric.Hair);
        }
    }
}
