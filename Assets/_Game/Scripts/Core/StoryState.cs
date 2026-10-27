using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// What happened so far that later scenes react to (the Ledger keeps the moral record; this keeps the facts).
    /// Reset on every play session.
    /// </summary>
    public static class StoryState
    {
        /// <summary>False once the Smilers have killed him (or anyone has). 6e's stream taunts Daniel with it.</summary>
        public static bool LuisAlive = true;
        /// <summary>Daniel treated Luis and took his radio: Luis warns him of what is ahead (6b, 6c...).</summary>
        public static bool HasLuisRadio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            LuisAlive = true;
            HasLuisRadio = false;
        }
    }
}
