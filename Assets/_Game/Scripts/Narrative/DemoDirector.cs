using System.Collections;
using UnityEngine;
using Echoes.Rendering;
using Echoes.UI;
using Echoes.World;

namespace Echoes.Narrative
{
    /// <summary>
    /// Runs the demo (Act 1): sequence 1 (cold open in the ambulance), then sequence 2 (the crash site); sequences 3
    /// (the gas station) and 4 (the alleys) are set up from the start and play out when Daniel gets there. `startAt`
    /// can begin at the crash site, the gas station or the alleys (for testing), or skip the story entirely; F4 skips
    /// the ride while it plays.
    /// </summary>
    [RequireComponent(typeof(ColdOpenSequence), typeof(CrashSiteSequence), typeof(GasStationSequence))]
    [RequireComponent(typeof(AlleySequence))]
    public class DemoDirector : MonoBehaviour
    {
        // New entries go at the end: the field is serialized as an int.
        public enum StartAt { ColdOpen, CrashSite, GasStation, Sandbox, Alleys }

        public static DemoDirector Instance { get; private set; }

        public StartAt startAt = StartAt.ColdOpen;
        [Tooltip("Smoke and steam materials from the scene builder (the truck's steaming radiator).")]
        public FireFX.Materials fx;

        ColdOpenSequence coldOpen;
        CrashSiteSequence crashSite;
        GasStationSequence gasStation;
        AlleySequence alleys;
        Coroutine running;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            coldOpen = GetComponent<ColdOpenSequence>();
            crashSite = GetComponent<CrashSiteSequence>();
            gasStation = GetComponent<GasStationSequence>();
            alleys = GetComponent<AlleySequence>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            if (startAt == StartAt.Sandbox) yield break;
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.SetFade(1f); // nothing of the spawn point shows
            yield return null; // the NavMesh bakes in Awake; characters are placed on it from here on
            coldOpen.Build(fx);
            gasStation.Setup();
            StartCoroutine(gasStation.Play());
            alleys.Setup();
            StartCoroutine(alleys.Play(gasStation));
            if (startAt == StartAt.GasStation) { StartFree(new Vector3(-2f, 0.05f, -45f), 180f, "Get home to Mia - south on Harlow"); yield break; }
            if (startAt == StartAt.Alleys) { StartFree(new Vector3(-43.5f, DistrictGenerator.Curb + 0.05f, -74.5f), 270f, "Get home to Mia"); yield break; }
            if (startAt == StartAt.CrashSite) coldOpen.Skip();
            running = StartCoroutine(Flow(startAt == StartAt.ColdOpen));
        }

        IEnumerator Flow(bool ride)
        {
            if (ride) yield return coldOpen.Play();
            yield return crashSite.Play();
            running = null;
        }

        /// <summary>Testing: the crash has happened, Luis sits by the wreck, Daniel stands somewhere later on the route.</summary>
        void StartFree(Vector3 pos, float yaw, string objective)
        {
            coldOpen.Skip();
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            var rig = p.GetComponent<Player.CameraRig>();
            var input = p.GetComponent<Player.EchoesInput>();
            rig.Unmount();
            input.LookOnly = false;
            input.InputLocked = false;
            p.GetComponent<Player.FPSController>().Teleport(pos, yaw);
            AI.Perception.PlayerConcealed = false;
            var hud = BodycamHUD.Instance;
            if (hud != null) { hud.SetFade(0f); hud.SetVitalsVisible(true); hud.SetObjective(objective); }
        }

        /// <summary>Dev shortcut: cut the ambulance ride and go straight to the crash site.</summary>
        public void SkipToCrash()
        {
            if (startAt == StartAt.Sandbox || coldOpen.Crashed || !coldOpen.Playing) return;
            if (running != null) StopCoroutine(running);
            coldOpen.Skip();
            running = StartCoroutine(Flow(false));
        }
    }
}
