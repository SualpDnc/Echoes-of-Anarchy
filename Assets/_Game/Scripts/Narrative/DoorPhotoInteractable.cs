using UnityEngine;
using Echoes.Combat;

namespace Echoes.Narrative
{
    /// <summary>The school door with Daniel's photo taped to it: looking at it starts the end of the demo.</summary>
    public class DoorPhotoInteractable : Interactable
    {
        public SchoolSequence scene;

        public override string Prompt => "Look at the photo";
        public override bool CanInteract => scene != null && scene.CanLook;
        public override float MaxDistance => 3.2f;

        public override void Interact(GameObject player) => scene.LookAtPhoto();
    }
}
