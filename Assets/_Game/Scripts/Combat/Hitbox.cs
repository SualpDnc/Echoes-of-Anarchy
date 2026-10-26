using UnityEngine;

namespace Echoes.Combat
{
    /// <summary>Collider on a body part. Scales damage and forwards it to the owning Health.</summary>
    public class Hitbox : MonoBehaviour
    {
        public Health owner;
        public BodyPart part = BodyPart.Torso;
        public float multiplier = 1f;

        public void Hit(DamageInfo info)
        {
            if (owner == null) return;
            info.amount *= multiplier;
            info.part = part;
            if (info.hitBody == null) TryGetComponent(out info.hitBody);
            owner.ApplyDamage(info);
        }

        /// <summary>Applies damage to whatever a ray hit: a hitbox, or any Health up the hierarchy.</summary>
        public static bool Damage(Collider c, DamageInfo info)
        {
            if (c.TryGetComponent(out Hitbox hb)) { info.hitBody = c.attachedRigidbody; hb.Hit(info); return true; }
            var h = c.GetComponentInParent<Health>();
            if (h == null) return false;
            h.ApplyDamage(info);
            return true;
        }
    }
}
