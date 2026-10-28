using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Echoes.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Core;
using Echoes.Narrative;
using Echoes.Player;
using Echoes.Rendering;
using Echoes.UI;
using Echoes.World;

namespace Echoes.EditorTools
{
    /// <summary>Generates scenes from code so the whole project stays reproducible.</summary>
    public static class EchoesSceneBuilder
    {
        const string SceneDir = "Assets/_Game/Scenes";

        static readonly Color FogColor = new Color(0.05f, 0.056f, 0.075f);

        [MenuItem("Tools/Echoes/Build Harlow District", priority = 0)]
        public static void BuildDistrict()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            float t0 = (float)EditorApplication.timeSinceStartup;

            var pal = MaterialLibrary.Build(false, out var fx);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var moon = SetupEnvironment();
            CreateSystems();

            var world = new GameObject("World");
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            world.AddComponent<RuntimeNavMesh>();

            var info = DistrictGenerator.Generate(pal, fx, world.transform, 2034);
            CreatePlayer(info.playerSpawn, info.playerYaw);
            CreateReverbZones();
            CreateTestEncounters();
            CreateDemo(fx);

            Directory.CreateDirectory(SceneDir);
            string path = SceneDir + "/HarlowDistrict.unity";
            EditorSceneManager.SaveScene(scene, path);

            BakeProbes(info.probePoints, SceneDir + "/HarlowDistrict");
            EditorSceneManager.SaveScene(scene, path);
            AddToBuildSettings(path);
            Debug.Log($"[Echoes] Harlow District built in {EditorApplication.timeSinceStartup - t0:0.0}s → {path}");
        }

        // ------------------------------------------------------------------ environment

        static Light SetupEnvironment()
        {
            var moonGo = new GameObject("Moon");
            var moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.58f, 0.68f, 1f);
            moon.intensity = 0.3f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.8f;
            moonGo.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            RenderSettings.sun = moon;

            RenderSettings.skybox = MaterialLibrary.NightSky(moon);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.1f, 0.115f, 0.16f);
            RenderSettings.ambientEquatorColor = new Color(0.07f, 0.065f, 0.065f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.027f, 0.025f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.011f;
            RenderSettings.fogColor = FogColor;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 1f;

            var volGo = new GameObject("Bodycam PostFX", typeof(Volume), typeof(BodycamVolume));
            volGo.GetComponent<Volume>().isGlobal = true;
            return moon;
        }

        static void CreateSystems()
        {
            var sys = new GameObject("Systems");
            sys.AddComponent<GameClock>();
            sys.AddComponent<CharacterLineup>();
            sys.AddComponent<DialogueDirector>();
            sys.AddComponent<PurgeClock>();
            sys.AddComponent<StoryDevHotkeys>();
            new GameObject("Bodycam HUD").AddComponent<BodycamHUD>();

            var audio = new GameObject("Audio");
            audio.AddComponent<AudioManager>();
            audio.AddComponent<DistantEvents>();
            audio.AddComponent<PurgeSiren>();
            audio.AddComponent<AudioDevHotkeys>();
        }

        /// <summary>Demo story flow (phase 6): the ambulance cold open, then the crash site. Vehicles are built at runtime.</summary>
        static void CreateDemo(FireFX.Materials fx)
        {
            var go = new GameObject("Demo");
            go.AddComponent<ColdOpenSequence>();
            go.AddComponent<CrashSiteSequence>();
            go.AddComponent<GasStationSequence>();
            go.AddComponent<AlleySequence>();
            go.AddComponent<HouseSequence>();
            go.AddComponent<MetroSequence>();
            go.AddComponent<PartySequence>();
            go.AddComponent<GymSequence>();
            go.AddComponent<SchoolSequence>();
            go.AddComponent<DemoDirector>().fx = fx;
        }

        /// <summary>Phase 4 sandbox fights until the demo sequences (phase 6) place the real encounters.</summary>
        static void CreateTestEncounters()
        {
            var root = new GameObject("TestEncounters").transform;
            void Spawner(string name, Vector3 pos, Faction f, int count, float spread, float pistols, bool stationary, float patrol, int seed)
            {
                var s = new GameObject(name).AddComponent<NPCSpawner>();
                s.transform.SetParent(root, false);
                s.transform.position = pos;
                s.faction = f; s.count = count; s.spread = spread; s.pistolChance = pistols;
                s.stationary = stationary; s.patrolRadius = patrol; s.seed = seed;
            }
            // The barrel looters are now the gas station's (GasStationSequence).
            Spawner("Civilian_Doorway", new Vector3(9.3f, 0.15f, -31f), Faction.Civilian, 1, 0.5f, 0f, true, 2f, 12);
            Spawner("Looters_Alley", new Vector3(27f, 0.15f, -30f), Faction.Looter, 2, 2f, 0.5f, false, 10f, 13);
            // The intersection Smilers are now the scripted watchers of sequence 2 (CrashSiteSequence).
        }

        /// <summary>Street-canyon reverb everywhere; tighter, slappier reverb in the alley.</summary>
        static void CreateReverbZones()
        {
            var root = new GameObject("ReverbZones").transform;
            var city = new GameObject("City").AddComponent<AudioReverbZone>();
            city.transform.SetParent(root, false);
            city.reverbPreset = AudioReverbPreset.City;
            city.minDistance = 200f;
            city.maxDistance = 260f;
            for (float z = -95f; z <= 5f; z += 10f)
            {
                var alley = new GameObject("Alley").AddComponent<AudioReverbZone>();
                alley.transform.SetParent(root, false);
                alley.transform.position = new Vector3(27f, 1.5f, z);
                alley.reverbPreset = AudioReverbPreset.Alley;
                alley.minDistance = 3f;
                alley.maxDistance = 6f;
            }
        }

        /// <summary>Box-projected probes so wet asphalt and glass reflect the street lights they sit under.</summary>
        static void BakeProbes(List<Vector3> points, string dir)
        {
            Directory.CreateDirectory(dir);
            var parent = new GameObject("ReflectionProbes").transform;
            var probes = new List<ReflectionProbe>();
            foreach (var p in points)
            {
                var go = new GameObject("Probe");
                go.transform.SetParent(parent, false);
                go.transform.position = p;
                var rp = go.AddComponent<ReflectionProbe>();
                rp.mode = ReflectionProbeMode.Custom;
                rp.size = new Vector3(24f, 16f, 32f);
                rp.center = new Vector3(0f, 5.5f, 0f);
                rp.boxProjection = true;
                rp.resolution = 256;
                rp.hdr = true;
                rp.blendDistance = 4f;
                rp.farClipPlane = 250f;
                probes.Add(rp);
            }
            for (int i = 0; i < probes.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Echoes — baking reflections", $"Probe {i + 1}/{probes.Count}", i / (float)probes.Count);
                string file = $"{dir}/Probe_{i}.exr";
                if (Lightmapping.BakeReflectionProbe(probes[i], file))
                {
                    AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceSynchronousImport);
                    probes[i].customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(file);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        // ------------------------------------------------------------------ player

        public static GameObject CreatePlayer(Vector3 pos, float yaw)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            player.AddComponent<CharacterController>();
            player.AddComponent<EchoesInput>();
            player.AddComponent<FPSController>();
            var rig = player.AddComponent<CameraRig>();
            player.AddComponent<PlayerAudio>();
            player.AddComponent<Health>().maxHealth = 100f;
            player.AddComponent<PlayerHealth>();
            player.AddComponent<WeaponPistol>();
            player.AddComponent<ThrowController>();
            player.AddComponent<PlayerMelee>();
            player.AddComponent<PlayerInteractor>();

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0f, 1.46f, 0.12f);

            var camGo = new GameObject("BodycamCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            cam.fieldOfView = 76f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = FogColor;
            cam.allowHDR = true;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.stopNaN = true;
            data.renderShadows = true;
            data.requiresDepthTexture = true; // soft particles

            rig.pivot = pivot;
            rig.cam = cam;

            var lightGo = new GameObject("Flashlight", typeof(Light));
            lightGo.transform.SetParent(player.transform, false);
            var fl = lightGo.AddComponent<Flashlight>();
            fl.rig = rig;
            return player;
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path || s.path.EndsWith("/TestStreet.unity"));
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
