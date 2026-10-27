using UnityEngine;
using Echoes.Combat;

namespace Echoes.Narrative
{
    /// <summary>Wounded Luis at the crash site: treat him with a med kit (the choice), or talk once he is patched up.</summary>
    public class LuisInteractable : Interactable
    {
        public CrashSiteSequence scene;
        PlayerHealth playerHealth;

        public override string Prompt
        {
            get
            {
                if (scene.LuisTreated) return "Talk to Luis";
                if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
                return playerHealth != null && playerHealth.medKits > 0 ? "Treat Luis (uses a med kit)" : "Treat Luis (needs a med kit)";
            }
        }

        public override bool CanInteract => scene != null && !scene.LuisDead && !scene.Treating; // coming back for him still counts
        public override float MaxDistance => 2.6f;

        public override void Interact(GameObject player) => scene.InteractWithLuis(player);
    }
}
