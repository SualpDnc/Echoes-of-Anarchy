using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Echoes.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Builds the shareable Windows demo: rebuilds the Harlow District scene, keeps the shader variants that only runtime
    /// materials use (transparent Lit for glass, blood and spray paint; they would be stripped otherwise), and makes a
    /// release (non-development) 64-bit build with only the demo scene. Developer keys are off in release builds.
    /// Menu: Tools > Echoes > Build Demo (Windows). Batchmode: -executeMethod Echoes.EditorTools.EchoesBuild.BuildDemo
    /// (optional -buildOutput "folder").
    /// </summary>
    public static class EchoesBuild
    {
        const string ScenePath = "Assets/_Game/Scenes/HarlowDistrict.unity";
        const string KeepDir = "Assets/_Game/Generated/Resources/ShaderKeep";
        const string ExeName = "EchoesOfAnarchy.exe";

        [MenuItem("Tools/Echoes/Build Demo (Windows)", priority = 20)]
        public static void BuildDemoMenu()
        {
            string dir = EditorUtility.SaveFolderPanel("Build the demo into", DefaultOutput(), "");
            if (!string.IsNullOrEmpty(dir)) Build(dir);
        }

        public static void BuildDemo()
        {
            string dir = DefaultOutput();
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-buildOutput") dir = args[i + 1];
            bool ok = Build(dir);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        static string DefaultOutput() =>
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory), "EchoesOfAnarchy_Demo");

        static bool Build(string dir)
        {
            EchoesSceneBuilder.BuildDistrict();
            KeepRuntimeVariants();

            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(dir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None, // release: no profiler hooks, no debug console, developer keys off
            };
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[Echoes] Build {s.result}: {s.outputPath}, {s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s");
            if (s.result != BuildResult.Succeeded) return false;

            // Debug symbols and IL backups are for us, not for players.
            foreach (var pattern in new[] { "*_DoNotShip", "*DontShipItWithYourGame" })
                foreach (var d in Directory.GetDirectories(dir, pattern)) Directory.Delete(d, true);
            File.WriteAllText(Path.Combine(dir, "README.txt"), Readme);
            return true;
        }

        /// <summary>
        /// Saved copies of the runtime materials whose keyword sets no scene material has. Resources content is always in
        /// the build, so URP keeps these Lit variants (the textures don't matter, only the keywords).
        /// </summary>
        public static void KeepRuntimeVariants()
        {
            Directory.CreateDirectory(KeepDir);
            void Save(string name, Material m)
            {
                var copy = new Material(m) { name = name, hideFlags = HideFlags.None };
                foreach (var prop in new[] { "_BaseMap", "_BumpMap" }) if (copy.HasProperty(prop)) copy.SetTexture(prop, null);
                string path = KeepDir + "/" + name + ".mat";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(copy, path);
                // URP re-derives keywords from the properties when the asset is first imported (it dropped _EMISSION,
                // _NORMALMAP and premultiply here). Write back the exact set the runtime material has: that is the
                // variant the build must contain.
                var saved = AssetDatabase.LoadAssetAtPath<Material>(path);
                saved.shaderKeywords = m.shaderKeywords;
                if (m.IsKeywordEnabled("_EMISSION")) saved.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                EditorUtility.SetDirty(saved);
                AssetDatabase.SaveAssetIfDirty(saved);
            }
            var tex = Texture2D.whiteTexture;
            Save("Glass", RuntimeMaterials.Glass(new Color(0.5f, 0.6f, 0.6f), 0.3f));
            Save("Decal", RuntimeMaterials.Decal("KeepDecal", tex, Color.red, 0.5f));
            Save("DecalGlow", RuntimeMaterials.Decal("KeepDecalGlow", tex, Color.red, 0.5f, Color.red * 0.2f));
            Save("Lit", RuntimeMaterials.Lit(Color.gray, 0.5f));
            Save("LitGlow", RuntimeMaterials.Lit(Color.gray, 0.5f, 0f, Color.white));
            Save("Fabric", RuntimeMaterials.Fabric(Color.gray, 0.3f, Fabric.Cotton));
            Save("FabricGlow", RuntimeMaterials.Fabric(Color.gray, 0.3f, Fabric.Cotton, 0f, Color.white));
            AssetDatabase.SaveAssets();
        }

        const string Readme =
@"ECHOES OF ANARCHY - DEMO (Act 1, ~15 min)
==========================================

Run EchoesOfAnarchy.exe. Windows may warn that the app is from an unknown publisher
(it is not signed): More info > Run anyway.

TR - Kontroller
  WASD yürü, Fare bak, Shift koş, C eğil (Ctrl basılı tut), Space zıpla
  Sol tık ateş, Sağ tık nişan, R şarjör, G şişe at, F kullan / yumruk
  Q / E eğilip bak (köşeden), X silahı kaldır/çıkar, T fener, H sağlık kiti
  F2 hasar sayıları aç/kapa, F3 altyazı dili (Türkçe / English)
  Esc imleci serbest bırakır; ESC'YE BASILI TUT = oyundan çık

EN - Controls
  WASD move, Mouse look, Shift sprint, C crouch (hold Ctrl), Space jump
  Left click fire, Right click aim, R reload, G throw a bottle, F use / punch
  Q / E lean, X holster, T flashlight, H med kit
  F2 damage numbers on/off, F3 subtitle language
  Esc frees the cursor; HOLD ESC = quit

The first start asks for the subtitle language. Use headphones.
Everything in the game (models, textures, sounds) is generated by code.
";
    }
}
