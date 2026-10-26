using UnityEngine;
using Echoes.Combat;

namespace Echoes.AI
{
    /// <summary>Added to a surrendered NPC: the player can let him go. Shooting him instead is recorded too.</summary>
    public class SpareInteractable : Interactable
    {
        public NPCBrain brain;

        public override string Prompt => "Let him go";
        public override float MaxDistance => 3.5f;
        public override bool CanInteract => brain != null && brain.State == NPCState.Surrender;

        public override void Interact(GameObject player) => brain.Spare(true);
    }
}
