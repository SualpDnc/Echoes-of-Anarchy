using System;
using UnityEngine;

namespace Echoes.Combat
{
    /// <summary>
    /// A body that bullets and blows don't hurt (8c: the blind Smiler in bullet-proof armour). Hits on its hitboxes do
    /// no damage: they read as metal (sparks, a ricochet clang) and are reported through Struck, so a brain can react.
    /// </summary>
    public class Armored : MonoBehaviour
    {
        public event Action<DamageInfo> Struck;

        /// <summary>Called by Hitbox instead of applying damage. Bullets get their sparks and clang from ImpactFX.</summary>
        public void Hit(DamageInfo info) => Struck?.Invoke(info);
    }
}
