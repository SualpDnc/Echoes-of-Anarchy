using UnityEngine;
using Echoes.AI;
using Echoes.Combat;

namespace Echoes.Narrative
{
    /// <summary>The gas station clerk hiding behind the counter: Daniel can talk to her while she is still hiding.</summary>
    public class ClerkInteractable : Interactable
    {
        public GasStationSequence scene;

        public override string Prompt => scene.ClerkTalked ? "Talk" : "Talk to her";
        public override bool CanInteract => scene != null && scene.Clerk != null && scene.Clerk.State == NPCState.Cower && !scene.Talking;
        public override float MaxDistance => 2.8f;

        public override void Interact(GameObject player) => scene.TalkToClerk(player);
    }
}
