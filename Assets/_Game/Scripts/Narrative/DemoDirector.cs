using System.Collections;
using UnityEngine;
using Echoes.Combat;
using Echoes.Core;
using Echoes.Rendering;
using Echoes.UI;
using Echoes.World;

namespace Echoes.Narrative
{
    /// <summary>
    /// Runs the game: sequence 1 (cold open in the ambulance), then sequence 2 (the crash site); the gas station, Daniel's
    /// house (8b) and the school door are set up from the start and play out when Daniel gets there. `startAt` can begin
    /// at the crash site, the gas station, behind it (Alleys: the house call comes next) or the school yard (for testing),
    /// or skip the story entirely; F4 skips the ride while it plays. It also keeps the checkpoints: a death restarts at the
    /// last one (Core/Checkpoint) instead of at the ambulance ride.
    /// </summary>
    [RequireComponent(typeof(ColdOpenSequence), typeof(CrashSiteSequence), typeof(GasStationSequence))]
    [RequireComponent(typeof(AlleySequence), typeof(SchoolSequence), typeof(HouseSequence))] // RequireComponent takes at most three types
    public class DemoDirector : MonoBehaviour
    {
        // New entries go at the end: the field is serialized as an int.
        public enum StartAt { ColdOpen, CrashSite, GasStation, Sandbox, Alleys, School }

        public static DemoDirector Instance { get; private set; }

        public StartAt startAt = StartAt.ColdOpen;
        [Tooltip("Smoke and steam materials from the scene builder (the truck's steaming radiator).")]
        public FireFX.Materials fx;

        ColdOpenSequence coldOpen;
        CrashSiteSequence crashSite;
        GasStationSequence gasStation;
        AlleySequence alleys;
        HouseSequence house;
        SchoolSequence school;
        Coroutine running;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            coldOpen = GetComponent<ColdOpenSequence>();
            crashSite = GetComponent<CrashSiteSequence>();
            gasStation = GetComponent<GasStationSequence>();
            alleys = GetComponent<AlleySequence>();
            house = GetComponent<HouseSequence>();
            school = GetComponent<SchoolSequence>();
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
            alleys.Setup();   // the alleys' pickups; their chase is gone (8b: the house replaces it)
            house.Setup();
            StartCoroutine(house.Play(gasStation));
            school.Setup();
            StartCoroutine(school.Play());
            StartCoroutine(SaveCheckpoints());

            // Back from a death: the last checkpoint, as things stood then.
            if (Checkpoint.TakeRestore(out var stage))
            {
                yield return Restore(stage);
                yield break;
            }
            if (startAt == StartAt.GasStation) { StartFree(CheckpointStage.GasStation); Save(CheckpointStage.GasStation); yield break; }
            if (startAt == StartAt.Alleys) { StartFree(CheckpointStage.Alleys); Save(CheckpointStage.Alleys); yield break; }
            if (startAt == StartAt.School) { StartFree(CheckpointStage.School); Save(CheckpointStage.School); yield break; }
            if (startAt == StartAt.CrashSite) coldOpen.Skip();
            running = StartCoroutine(Flow(startAt == StartAt.ColdOpen));
        }

        IEnumerator Flow(bool ride)
        {
            if (ride) yield return coldOpen.Play();
            yield return crashSite.Play();
            running = null;
        }

        // ------------------------------------------------------------------ checkpoints

        /// <summary>
        /// Saves as the story moves on: the crash; Luis's part over and Daniel heading down Harlow toward the station;
        /// through the station's back gate (before the call and the chase); into the school yard.
        /// </summary>
        IEnumerator SaveCheckpoints()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) yield break;
            var health = p.GetComponent<Health>();
            while (Checkpoint.Stage < CheckpointStage.School)
            {
                if (health == null || !health.IsDead)
                {
                    if (coldOpen.Crashed) Save(CheckpointStage.CrashSite);
                    if (crashSite.Resolved && !crashSite.Busy && p.transform.position.z < -50f) Save(CheckpointStage.GasStation);
                    if (gasStation.Done) Save(CheckpointStage.Alleys);
                    if (house.Done) Save(CheckpointStage.House);
                    if (school.Reached) Save(CheckpointStage.School);
                }
                yield return null;
            }
        }

        void Save(CheckpointStage stage)
        {
            if (stage <= Checkpoint.Stage) return;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            var kit = new Checkpoint.Kit();
            if (p.TryGetComponent(out Health h)) kit.health = h.Current;
            if (p.TryGetComponent(out PlayerHealth ph)) kit.medKits = ph.medKits;
            if (p.TryGetComponent(out WeaponPistol w)) { kit.ammoInMag = w.ammoInMag; kit.reserve = w.reserve; }
            if (p.TryGetComponent(out ThrowController t)) kit.bottles = t.bottles;
            Checkpoint.Save(stage, kit);
        }

        /// <summary>
        /// Restarts at a checkpoint. The crash site plays again from Daniel waking up. Later stages put the night back
        /// as it was (the Purge under way, Luis treated or dead, the looters gone) and give Daniel his kit back, with at
        /// least 60 health so a restart is never a death trap.
        /// </summary>
        IEnumerator Restore(CheckpointStage stage)
        {
            if (stage == CheckpointStage.CrashSite)
            {
                coldOpen.Skip();
                running = StartCoroutine(Flow(false));
                yield break;
            }
            if (PurgeClock.Instance != null) PurgeClock.Instance.AlreadyStarted(); // before the skip, so the clock isn't rewound to 18:59
            StartFree(stage);
            var clock = GameClock.Instance;
            var t = Checkpoint.SavedTime;
            if (clock != null && t != System.DateTime.MinValue) clock.JumpTo(t.Hour, t.Minute, t.Second);
            crashSite.RestoreAfterCheckpoint();
            if (stage >= CheckpointStage.Alleys) gasStation.RestoreAfterCheckpoint();
            if (stage >= CheckpointStage.House) house.RestoreAfterCheckpoint();

            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                var kit = Checkpoint.SavedKit;
                if (p.TryGetComponent(out Health h)) h.SetHealth(Mathf.Max(kit.health, 60f));
                if (p.TryGetComponent(out PlayerHealth ph)) ph.medKits = kit.medKits;
                if (p.TryGetComponent(out WeaponPistol w)) { w.ammoInMag = kit.ammoInMag; w.reserve = kit.reserve; }
                if (p.TryGetComponent(out ThrowController tc)) tc.bottles = kit.bottles;
            }
            // The tape picks up again: black while the lamps settle, then a fade in.
            var hud = BodycamHUD.Instance;
            if (hud != null) hud.SetFade(1f);
            yield return new WaitForSeconds(1.6f);
            if (hud != null) hud.ShowHint(GameSettings.Pick("Checkpoint", "Kontrol noktası"), 2.5f);
            yield return ColdOpenSequence.Fade(1f, 0f, 1.2f);
        }

        /// <summary>Starts at a stage without its lead-up (checkpoint restarts and test starts): the crash has happened, Luis sits by the wreck.</summary>
        void StartFree(CheckpointStage stage)
        {
            Vector3 pos;
            float yaw;
            string objective;
            switch (stage)
            {
                case CheckpointStage.Alleys: pos = new Vector3(-43.5f, DistrictGenerator.Curb + 0.05f, -74.5f); yaw = 270f; objective = "Get home to Mia"; break;
                case CheckpointStage.House: pos = new Vector3(-82f, DistrictGenerator.Curb + 0.05f, -58f); yaw = 0f; objective = "The school - north up the back alley"; break;
                case CheckpointStage.School: pos = new Vector3(-82f, DistrictGenerator.Curb + 0.05f, -20f); yaw = 0f; objective = "Find Mia - the school"; break;
                default: pos = new Vector3(-2f, 0.05f, -45f); yaw = 180f; objective = "Get home to Mia - south on Harlow"; break;
            }
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
