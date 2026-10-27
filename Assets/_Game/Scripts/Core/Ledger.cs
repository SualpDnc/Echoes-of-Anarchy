using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Core
{
    public enum LedgerAct
    {
        KilledHostile, KilledFleeing, KilledSurrendered, SparedSurrendered,
        KilledCivilian, HelpedWounded, AbandonedWounded, Looted
    }

    /// <summary>
    /// The hidden moral record — everything the bodycam saw. Never shown during play; read by the ending/trial.
    /// Mercy is positive, cruelty negative.
    /// </summary>
    public static class Ledger
    {
        public struct Entry
        {
            public LedgerAct act;
            public string detail;
            public DateTime time;
        }

        static readonly List<Entry> entries = new List<Entry>();
        public static IReadOnlyList<Entry> Entries => entries;
        public static event Action<Entry> Recorded;

        public static int Mercy { get; private set; }

        static int Weight(LedgerAct a)
        {
            switch (a)
            {
                case LedgerAct.KilledHostile: return 0;
                case LedgerAct.KilledFleeing: return -2;
                case LedgerAct.KilledSurrendered: return -4;
                case LedgerAct.SparedSurrendered: return 3;
                case LedgerAct.KilledCivilian: return -6;
                case LedgerAct.HelpedWounded: return 3;
                case LedgerAct.AbandonedWounded: return -2;
                case LedgerAct.Looted: return -1;
                default: return 0;
            }
        }

        public static void Record(LedgerAct act, string detail = "")
        {
            var e = new Entry { act = act, detail = detail, time = GameClock.Instance != null ? GameClock.Instance.Now : DateTime.MinValue };
            entries.Add(e);
            Mercy += Weight(act);
            Recorded?.Invoke(e);
#if UNITY_EDITOR
            Debug.Log($"[Ledger] {act} {detail} (mercy {Mercy})");
#endif
        }

        /// <summary>Back to a saved record (a checkpoint restart): listeners are not told, nothing new happened.</summary>
        public static void Restore(IEnumerable<Entry> saved)
        {
            entries.Clear();
            entries.AddRange(saved);
            Mercy = 0;
            foreach (var e in entries) Mercy += Weight(e.act);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            entries.Clear();
            Mercy = 0;
            Recorded = null;
        }
    }
}
