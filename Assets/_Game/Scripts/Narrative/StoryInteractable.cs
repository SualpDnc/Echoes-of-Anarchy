using System;
using UnityEngine;
using Echoes.Combat;

namespace Echoes.Narrative
{
    /// <summary>
    /// A use-key interactable whose prompt, availability and action a story sequence supplies in code (the closet, the
    /// phone and the bed in Daniel's house), so each prop doesn't need its own class.
    /// </summary>
    public class StoryInteractable : Interactable
    {
        public string prompt = "Use";
        public float maxDistance = 2.4f;
        public Func<bool> available;
        public Action<GameObject> use;

        public override string Prompt => prompt;
        public override bool CanInteract => use != null && (available == null || available());
        public override float MaxDistance => maxDistance;

        public override void Interact(GameObject player) => use?.Invoke(player);

        public static StoryInteractable Add(GameObject target, string prompt, Func<bool> available, Action<GameObject> use, float maxDistance = 2.4f)
        {
            var it = target.AddComponent<StoryInteractable>();
            it.prompt = prompt;
            it.available = available;
            it.use = use;
            it.maxDistance = maxDistance;
            return it;
        }
    }
}
