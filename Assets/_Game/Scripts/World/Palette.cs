using UnityEngine;

namespace Echoes.World
{
    /// <summary>All materials the procedural world generators draw from. Filled by the editor material library.</summary>
    public class Palette
    {
        public Material asphalt, sidewalk, curb, roadPaint, roadPaintWhite;
        public Material[] facadeWalls;      // brick, stucco, concrete variants
        public Material trimStone, roof, parapetCap;
        public Material windowFrame, glassDark;
        public Material[] glassLit;
        public Material boards, shutter, doorWood, signBand;
        public Material metalDark, rustyMetal, dumpsterGreen, galvanized;
        public Material[] carPaint;
        public Material carGlass, tire, chrome, headlight, taillight, burnt;
        public Material hydrantRed, mailboxBlue, signalHousing, signalAmber, signalOff, lampGlass;
        public Material signRed, signWhite; // lit sign panels (the gas station pylon)
        public Material distantWindows, dirt;

        public Material Pick(Material[] set, System.Random rng) => set[rng.Next(set.Length)];
    }
}
