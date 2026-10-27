using System;
using UnityEngine;

namespace Echoes.Combat
{
    public enum BodyPart { Torso, Head, Limb }

    public struct DamageInfo
    {
        public float amount;
        public Vector3 point;
        public Vector3 direction;
        public float force;
        public GameObject source;
        public bool fromPlayer;
        public BodyPart part;
        public Rigidbody hitBody; // bone that was hit, for ragdoll impulses
    }

    /// <summary>Hit points for anything that can be hurt. Hitboxes route damage here with their multiplier applied.</summary>
    public class Health : MonoBehaviour
    {
        public float maxHealth = 100f;
        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public float Fraction => Current / maxHealth;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        void Awake() => Current = maxHealth;

        public void ApplyDamage(DamageInfo info)
        {
            if (IsDead || info.amount <= 0f) return;
            Current = Mathf.Max(0f, Current - info.amount);
            Damaged?.Invoke(info);
            if (Current <= 0f)
            {
                IsDead = true;
                Died?.Invoke(info);
            }
        }

        /// <summary>Sets hit points without a hit reaction (story beats, e.g. waking up hurt after the crash).</summary>
        public void SetHealth(float value)
        {
            if (IsDead) return;
            Current = Mathf.Clamp(value, 1f, maxHealth);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }
    }
}
