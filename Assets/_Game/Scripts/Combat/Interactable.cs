using UnityEngine;

namespace Echoes.Combat
{
    /// <summary>Anything the player can use with the interact key. PlayerInteractor finds it and shows the prompt.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public virtual bool CanInteract => true;
        public virtual float MaxDistance => 2.4f;
        public abstract void Interact(GameObject player);
    }
}
