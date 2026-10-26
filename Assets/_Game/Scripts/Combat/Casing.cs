using UnityEngine;
using Echoes.Audio;

namespace Echoes.Combat
{
    /// <summary>Ejected brass: tinkles on its first couple of bounces.</summary>
    public class Casing : MonoBehaviour
    {
        int bounces;

        void OnCollisionEnter(Collision c)
        {
            if (bounces++ >= 2 || c.relativeVelocity.magnitude < 0.6f || AudioManager.Instance == null) return;
            AudioManager.Instance.PlayAt(SoundId.ShellCasing, transform.position, bounces == 1 ? 0.35f : 0.18f, Random.Range(0.9f, 1.15f), 0.6f, 15f, false);
        }
    }
}
