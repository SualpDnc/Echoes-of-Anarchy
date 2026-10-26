using UnityEngine;
using Echoes.Audio;

namespace Echoes.Combat
{
    /// <summary>A dropped pistol or loose magazine: take its rounds.</summary>
    public class AmmoPickup : Interactable
    {
        public int rounds = 5;

        public override string Prompt => $"Take ammo ({rounds})";

        Light glow;

        void Start()
        {
            // A faint warm glint so dropped ammo can be found on dark asphalt.
            var go = new GameObject("PickupGlow", typeof(Light));
            glow = go.GetComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.82f, 0.5f);
            glow.range = 1.4f;
            glow.shadows = LightShadows.None;
            go.transform.SetParent(transform, true);
        }

        void Update()
        {
            if (glow == null) return;
            // Kept above the object in world space as it tumbles, with a slow pulse.
            glow.transform.position = transform.position + Vector3.up * 0.2f;
            glow.intensity = 0.7f + 0.35f * Mathf.Sin(Time.time * 3f);
        }

        public override void Interact(GameObject player)
        {
            if (!player.TryGetComponent(out WeaponPistol pistol)) return;
            pistol.AddAmmo(rounds);
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.MagIn, 0.5f, 0.9f);
            Destroy(gameObject);
        }
    }
}
