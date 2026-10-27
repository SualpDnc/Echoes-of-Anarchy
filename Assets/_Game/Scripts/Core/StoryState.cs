using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// What happened so far that later scenes react to (the Ledger keeps the moral record; this keeps the facts).
    /// Reset on every play session; Checkpoint takes and restores a copy.
    /// </summary>
    public static class StoryState
    {
        /// <summary>False once the Smilers have killed him (or anyone has). 6e's stream taunts Daniel with it.</summary>
        public static bool LuisAlive = true;
        /// <summary>Daniel treated Luis and took his radio: Luis warns him of what is ahead (6b, 6c...).</summary>
        public static bool HasLuisRadio;
        /// <summary>The gas station clerk is still alive (a checkpoint past the station leaves her body if not).</summary>
        public static bool ClerkAlive = true;

        public struct Snapshot { public bool luisAlive, hasLuisRadio, clerkAlive, taken; }

        public static Snapshot Take() => new Snapshot { luisAlive = LuisAlive, hasLuisRadio = HasLuisRadio, clerkAlive = ClerkAlive, taken = true };

        public static void Restore(Snapshot s)
        {
            if (!s.taken) { ResetOnPlay(); return; }
            LuisAlive = s.luisAlive;
            HasLuisRadio = s.hasLuisRadio;
            ClerkAlive = s.clerkAlive;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            LuisAlive = true;
            HasLuisRadio = false;
            ClerkAlive = true;
        }
    }
}
