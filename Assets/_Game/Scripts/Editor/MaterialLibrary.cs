using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Echoes.Rendering;
using Echoes.World;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Synthesizes every texture with TextureSynth, saves them as PNG assets with correct import settings,
    /// and builds the URP/Lit materials the generators use. Textures are cached on disk; pass force to regenerate.
    /// </summary>
    public static class MaterialLibrary
    {
        const string TexDir = "Assets/_Game/Generated/Textures";
        const string MatDir = "Assets/_Game/Generated/Materials";

        enum Kind { Albedo, Normal, Mask, Emission, Sprite }

        static bool force;
        static int step, totalSteps;

        [MenuItem("Tools/Echoes/Regenerate Textures", priority = 20)]
        static void RegenerateMenu()
        {
            Build(true, out _);
            Debug.Log("[Echoes] Textures regenerated.");
        }

        public static Palette Build(bool forceTextures, out FireFX.Materials fx)
        {
            force = forceTextures;
            step = 0;
            totalSteps = 22;
            Directory.CreateDirectory(TexDir);
            Directory.CreateDirectory(MatDir);
            var pal = new Palette();
            try
            {
                pal.asphalt = Pbr("Asphalt", 4f, 1024, s => TextureSynth.Asphalt(s, 11));
                pal.sidewalk = Pbr("Sidewalk", 3f, 512, s => TextureSynth.Sidewalk(s, 21));
                var concreteLight = Pbr("ConcreteLight", 3f, 512, s => TextureSynth.Concrete(s, 52, 0.42f));
                pal.curb = concreteLight;
                pal.trimStone = Pbr("TrimStone", 3f, 512, s => TextureSynth.Concrete(s, 53, 0.34f));
                pal.parapetCap = concreteLight;
                pal.facadeWalls = new[]
                {
                    Pbr("BrickRed", 2f, 1024, s => TextureSynth.Brick(s, 31, new Color(0.2f, 0.075f, 0.05f), new Color(0.3f, 0.29f, 0.27f))),
                    Pbr("BrickBrown", 2f, 1024, s => TextureSynth.Brick(s, 32, new Color(0.13f, 0.08f, 0.055f), new Color(0.2f, 0.19f, 0.17f))),
                    Pbr("BrickBuff", 2f, 1024, s => TextureSynth.Brick(s, 33, new Color(0.3f, 0.22f, 0.13f), new Color(0.26f, 0.25f, 0.23f))),
                    Pbr("StuccoBeige", 3f, 512, s => TextureSynth.Stucco(s, 41, new Color(0.36f, 0.3f, 0.23f))),
                    Pbr("StuccoGreen", 3f, 512, s => TextureSynth.Stucco(s, 42, new Color(0.15f, 0.21f, 0.19f))),
                    Pbr("Concrete", 3f, 512, s => TextureSynth.Concrete(s, 51, 0.26f)),
                };
                pal.roof = Pbr("Roof", 4f, 512, s => TextureSynth.Roof(s, 91));
                pal.boards = Pbr("Plywood", 2f, 512, s => TextureSynth.Planks(s, 61, new Color(0.42f, 0.3f, 0.17f)));
                pal.doorWood = Pbr("DoorWood", 2f, 512, s => TextureSynth.Planks(s, 62, new Color(0.12f, 0.07f, 0.04f)));
                pal.shutter = Pbr("Shutter", 2f, 512, s => TextureSynth.Shutter(s, 71, new Color(0.33f, 0.34f, 0.35f)));
                pal.metalDark = Pbr("MetalDark", 2f, 512, s => TextureSynth.PaintedMetal(s, 81, new Color(0.03f, 0.032f, 0.035f), 0.2f));
                pal.signBand = pal.metalDark;
                pal.signalHousing = Pbr("SignalHousing", 2f, 512, s => TextureSynth.PaintedMetal(s, 88, new Color(0.03f, 0.05f, 0.035f), 0.15f));
                pal.rustyMetal = Pbr("Rust", 2f, 512, s => TextureSynth.PaintedMetal(s, 82, new Color(0.1f, 0.06f, 0.04f), 0.9f));
                pal.dumpsterGreen = Pbr("DumpsterGreen", 2f, 512, s => TextureSynth.PaintedMetal(s, 83, new Color(0.04f, 0.12f, 0.06f), 0.5f));
                pal.hydrantRed = Pbr("HydrantRed", 1f, 512, s => TextureSynth.PaintedMetal(s, 84, new Color(0.45f, 0.04f, 0.03f), 0.35f));
                pal.mailboxBlue = Pbr("MailboxBlue", 1f, 512, s => TextureSynth.PaintedMetal(s, 85, new Color(0.03f, 0.07f, 0.25f), 0.15f));
                pal.burnt = Pbr("Burnt", 2f, 512, s => TextureSynth.PaintedMetal(s, 86, new Color(0.02f, 0.018f, 0.016f), 1f));
                pal.galvanized = Pbr("Galvanized", 2f, 512, s => TextureSynth.PaintedMetal(s, 87, new Color(0.34f, 0.35f, 0.36f), 0.1f));
                pal.distantWindows = Pbr("DistantWindows", 16f, 512, s => TextureSynth.DistantWindows(s, 101), 1.4f);

                // Flat-colour materials.
                pal.roadPaint = Flat("RoadPaintYellow", new Color(0.55f, 0.4f, 0.08f), 0.45f);
                pal.roadPaintWhite = Flat("RoadPaintWhite", new Color(0.6f, 0.6f, 0.58f), 0.45f);
                pal.windowFrame = Flat("WindowFrame", new Color(0.32f, 0.31f, 0.29f), 0.4f);
                pal.glassDark = Flat("GlassDark", new Color(0.012f, 0.014f, 0.018f), 0.96f);
                pal.glassLit = new[]
                {
                    Flat("GlassLitWarm", new Color(0.05f, 0.04f, 0.03f), 0.9f, 0f, new Color(1f, 0.66f, 0.36f) * 1.5f),
                    Flat("GlassLitCool", new Color(0.03f, 0.04f, 0.05f), 0.9f, 0f, new Color(0.62f, 0.75f, 1f) * 0.9f),
                    Flat("GlassLitTV", new Color(0.02f, 0.03f, 0.06f), 0.9f, 0f, new Color(0.3f, 0.42f, 1f) * 1.8f),
                };
                pal.carPaint = new[]
                {
                    Flat("CarMaroon", new Color(0.16f, 0.015f, 0.02f), 0.88f, 0.3f),
                    Flat("CarNavy", new Color(0.015f, 0.03f, 0.08f), 0.88f, 0.3f),
                    Flat("CarSilver", new Color(0.4f, 0.41f, 0.42f), 0.8f, 0.7f),
                    Flat("CarBlack", new Color(0.012f, 0.012f, 0.013f), 0.9f, 0.2f),
                    Flat("CarWhite", new Color(0.62f, 0.62f, 0.6f), 0.85f, 0.1f),
                    Flat("CarTaxi", new Color(0.62f, 0.42f, 0.02f), 0.85f, 0.1f),
                };
                pal.carGlass = Flat("CarGlass", new Color(0.01f, 0.012f, 0.014f), 0.97f);
                pal.tire = Flat("Rubber", new Color(0.018f, 0.018f, 0.018f), 0.3f);
                pal.chrome = Flat("Chrome", new Color(0.7f, 0.7f, 0.7f), 0.9f, 1f);
                pal.headlight = Flat("Headlight", new Color(0.5f, 0.5f, 0.5f), 0.95f, 0.4f);
                pal.taillight = Flat("Taillight", new Color(0.3f, 0.01f, 0.01f), 0.9f, 0f, new Color(0.4f, 0f, 0f));
                pal.signalAmber = Flat("SignalAmber", new Color(0.3f, 0.18f, 0.02f), 0.9f, 0f, new Color(1f, 0.55f, 0.08f) * 12f);
                pal.signalOff = Flat("SignalOff", new Color(0.04f, 0.03f, 0.03f), 0.9f);
                pal.lampGlass = Flat("LampGlass", new Color(0.8f, 0.6f, 0.4f), 0.9f, 0f, new Color(1f, 0.6f, 0.28f) * 8f);
                pal.signRed = Flat("SignRed", new Color(0.4f, 0.03f, 0.02f), 0.8f, 0f, new Color(1f, 0.08f, 0.04f) * 5f);
                pal.signWhite = Flat("SignWhite", new Color(0.7f, 0.7f, 0.68f), 0.8f, 0f, new Color(1f, 0.97f, 0.9f) * 3.5f);
                pal.dirt = Flat("Dirt", new Color(0.035f, 0.03f, 0.026f), 0.15f);

                fx = new FireFX.Materials
                {
                    flame = ParticleMat("FX_Flame", Sprite("FX_Flame", 128, TextureSynth.Flame(128, 7)), true, 3.2f, 0.4f),
                    ember = ParticleMat("FX_Ember", Sprite("FX_Dot", 32, TextureSynth.SoftDot(32)), true, 8f, 0.1f),
                    smoke = ParticleMat("FX_Smoke", Sprite("FX_Smoke", 128, TextureSynth.SmokePuff(128, 3)), false, 1f, 1.2f),
                    steam = ParticleMat("FX_Steam", Sprite("FX_Smoke", 128, null), false, 1f, 1.2f),
                };
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();
            return pal;
        }

        public static Material NightSky(Light moon)
        {
            var shader = Shader.Find("Echoes/NightSky");
            var mat = LoadOrCreate("Sky_Night", shader);
            if (moon != null) mat.SetVector("_MoonDirection", -moon.transform.forward);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ builders

        static Material Pbr(string name, float tileMetres, int size, Func<int, PBRSet> recipe, float emissionStrength = 0f)
        {
            Progress(name);
            string albedoPath = $"{TexDir}/T_{name}_Albedo.png";
            Texture2D albedo, normal, mask, emission = null;
            bool hasEmissionFile = File.Exists($"{TexDir}/T_{name}_Emission.png");
            if (force || !File.Exists(albedoPath))
            {
                PBRSet set = recipe(size);
                albedo = Save($"T_{name}_Albedo", size, set.albedo, Kind.Albedo);
                normal = Save($"T_{name}_Normal", size, set.BuildNormalMap(), Kind.Normal);
                mask = Save($"T_{name}_Mask", size, set.mask, Kind.Mask);
                if (set.emission != null) emission = Save($"T_{name}_Emission", size, set.emission, Kind.Emission);
            }
            else
            {
                albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/T_{name}_Normal.png");
                mask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/T_{name}_Mask.png");
                if (hasEmissionFile) emission = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/T_{name}_Emission.png");
            }

            var mat = LoadOrCreate("M_" + name, Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_MainTex", albedo);
            mat.SetTextureScale("_BaseMap", Vector2.one / tileMetres);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap", mask);
            mat.SetFloat("_Smoothness", 1f);
            mat.SetFloat("_SmoothnessTextureChannel", 0f);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetTexture("_OcclusionMap", mask);
            mat.SetFloat("_OcclusionStrength", 1f);
            mat.EnableKeyword("_OCCLUSIONMAP");
            if (emission != null)
            {
                mat.SetTexture("_EmissionMap", emission);
                mat.SetColor("_EmissionColor", Color.white * emissionStrength);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Flat(string name, Color baseColor, float smoothness, float metallic = 0f, Color? emission = null)
        {
            var mat = LoadOrCreate("M_" + name, Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D Sprite(string name, int size, Color[] px)
        {
            string path = $"{TexDir}/{name}.png";
            if (px == null || (!force && File.Exists(path))) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return Save(name, size, px, Kind.Sprite);
        }

        static Material ParticleMat(string name, Texture2D tex, bool additive, float intensity, float soft)
        {
            var mat = LoadOrCreate(name, Shader.Find("Echoes/Particle"));
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Intensity", intensity);
            mat.SetFloat("_SoftDistance", soft);
            mat.SetFloat("_SrcBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_Additive", additive ? 1f : 0f);
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material LoadOrCreate(string name, Shader shader)
        {
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            return mat;
        }

        static Texture2D Save(string name, int size, Color[] px, Kind kind)
        {
            string path = $"{TexDir}/{name}.png";
            // Recipes author albedo/emission in linear values; PNGs for sRGB import must be gamma-encoded.
            if (kind == Kind.Albedo || kind == Kind.Emission)
            {
                var g = new Color[px.Length];
                for (int i = 0; i < px.Length; i++) { var c = px[i]; float a = c.a; c = c.gamma; c.a = a; g[i] = c; }
                px = g;
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.SetPixels(px);
            tex.Apply(false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = kind == Kind.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = kind == Kind.Albedo || kind == Kind.Emission || kind == Kind.Sprite;
            imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency = kind == Kind.Sprite;
            imp.mipmapEnabled = true;
            imp.wrapMode = kind == Kind.Sprite ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            imp.anisoLevel = kind == Kind.Sprite ? 1 : 8;
            imp.filterMode = FilterMode.Trilinear;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void Progress(string what)
        {
            step++;
            EditorUtility.DisplayProgressBar("Echoes — synthesizing materials", what, Mathf.Clamp01(step / (float)totalSteps));
        }
    }
}
