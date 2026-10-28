using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>The places the demo restarts from after a death, in story order (Alleys = behind the gas station, House = out of the back gate after the raid, Metro = down in the station, Party = up in the school yard behind the hoarding, Turret = on the pickup's gun).</summary>
    /// <remarks>Not serialized anywhere (only compared in order), so stages can be inserted.</remarks>
    public enum CheckpointStage { None, CrashSite, GasStation, Alleys, House, Metro, Party, Turret, School }

    /// <summary>
    /// Where Daniel comes back after a death (owner: dying sent him back to the ambulance ride). DemoDirector saves at
    /// each stage; on death the scene reloads and DemoDirector restarts at the last stage with what was true then: the
    /// Ledger, StoryState, the in-game time and his kit (health, med kits, rounds, bottles). Static, so it outlives the
    /// scene reload; reset on every play session.
    /// </summary>
    public static class Checkpoint
    {
        public struct Kit
        {
            public float health;
            public int medKits, ammoInMag, reserve, bottles;
        }

        public static CheckpointStage Stage { get; private set; }
        public static Kit SavedKit { get; private set; }
        public static DateTime SavedTime { get; private set; }

        static List<Ledger.Entry> ledger = new List<Ledger.Entry>();
        static StoryState.Snapshot story;
        static bool restoreOnLoad;

        /// <summary>Records a stage. Never goes backwards (the alleys can't save over the school).</summary>
        public static void Save(CheckpointStage stage, Kit kit)
        {
            if (stage <= Stage) return;
            Stage = stage;
            SavedKit = kit;
            SavedTime = GameClock.Instance != null ? GameClock.Instance.Now : DateTime.MinValue;
            ledger = new List<Ledger.Entry>(Ledger.Entries);
            story = StoryState.Take();
#if UNITY_EDITOR
            Debug.Log($"[Checkpoint] {stage} saved (health {kit.health:0}, kits {kit.medKits}, rounds {kit.ammoInMag}+{kit.reserve})");
#endif
        }

        /// <summary>Called by the death screen just before the scene reloads.</summary>
        public static void ReloadAfterDeath() => restoreOnLoad = Stage != CheckpointStage.None;

        /// <summary>
        /// On scene start: true (once) if this load is a restart after a death. The Ledger and StoryState are put back as
        /// they were at the checkpoint; whatever Daniel did after it never happened.
        /// </summary>
        public static bool TakeRestore(out CheckpointStage stage)
        {
            stage = Stage;
            if (!restoreOnLoad) return false;
            restoreOnLoad = false;
            Ledger.Restore(ledger);
            StoryState.Restore(story);
            return true;
        }

        /// <summary>A fresh start in the same launch (after the end, or a restart before any checkpoint): no checkpoint, an empty Ledger, the story as at the start.</summary>
        public static void NewGame()
        {
            ResetOnPlay();
            Ledger.ResetOnPlay();
            StoryState.ResetOnPlay();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Stage = CheckpointStage.None;
            SavedKit = default;
            SavedTime = DateTime.MinValue;
            ledger = new List<Ledger.Entry>();
            story = default;
            restoreOnLoad = false;
        }
    }
}
