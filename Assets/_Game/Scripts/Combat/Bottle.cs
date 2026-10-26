using UnityEngine;
using Echoes.Audio;

namespace Echoes.Combat
{
    /// <summary>A thrown glass bottle. Shatters on a hard hit, making a noise the AI will investigate.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Bottle : MonoBehaviour
    {
        public float noiseRadius = 24f;
        public GameObject thrower;
        bool broken;
        float armedAt;

        void Start() => armedAt = Time.time + 0.05f;

        void OnCollisionEnter(Collision c)
        {
            if (broken || Time.time < armedAt) return;
            if (thrower != null && c.collider.transform.IsChildOf(thrower.transform)) return;
            if (c.relativeVelocity.magnitude < 3f) return;
            broken = true;
            Vector3 p = c.GetContact(0).point;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.GlassBreak, p, 1f, Random.Range(0.9f, 1.1f), 3f, 80f);
            ImpactFX.Glass(p, c.GetContact(0).normal);
            NoiseSystem.Emit(p, noiseRadius, NoiseKind.Impact, gameObject, true);
            Hitbox.Damage(c.collider, new DamageInfo { amount = 8f, point = p, direction = c.relativeVelocity.normalized, source = thrower, fromPlayer = true });
            Destroy(gameObject);
        }
    }
}
