using UnityEngine;

namespace Echoes.Combat
{
    /// <summary>
    /// Small loose physics objects (casings, dropped guns, ragdolls) must not collide with the player's
    /// CharacterController: standing on a moving rigidbody makes the capsule — and the camera — jitter.
    /// </summary>
    public static class PlayerCollision
    {
        static CharacterController player;

        public static void Ignore(Collider c)
        {
            if (c == null) return;
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go == null || !go.TryGetComponent(out player)) return;
            }
            Physics.IgnoreCollision(c, player, true);
        }

        public static void IgnoreAll(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>()) Ignore(c);
        }
    }
}
