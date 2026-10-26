using UnityEngine;
using Echoes.Audio;
using Echoes.Player;
using Echoes.UI;

namespace Echoes.Combat
{
    /// <summary>Throws bottles from the inventory — the stealth player's best tool for pulling guards away.</summary>
    public class ThrowController : MonoBehaviour
    {
        public int bottles = 3;
        public float throwSpeed = 14f;
        public float cooldown = 0.8f;

        EchoesInput input;
        CameraRig rig;
        FPSController body;
        WeaponPistol pistol;
        float next;

        void Awake()
        {
            input = GetComponent<EchoesInput>();
            rig = GetComponent<CameraRig>();
            body = GetComponent<FPSController>();
            TryGetComponent(out pistol);
        }

        void Update()
        {
            if (!input.ThrowPressed || input.InputLocked || Time.time < next) return;
            if (pistol != null && pistol.IsBusy) return;
            if (bottles <= 0)
            {
                if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowAmmo("NO BOTTLES", 1.2f);
                return;
            }
            next = Time.time + cooldown;
            bottles--;
            Transform cam = rig.cam.transform;
            var go = WeaponModels.Bottle(null);
            go.transform.SetPositionAndRotation(cam.position + cam.forward * 0.45f + cam.right * 0.18f - cam.up * 0.05f, cam.rotation * Quaternion.Euler(80f, 0f, 0f));
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.035f;
            col.height = 0.28f;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = body.Velocity + cam.forward * throwSpeed + Vector3.up * 2.2f;
            rb.angularVelocity = cam.right * Random.Range(8f, 14f) + Random.insideUnitSphere * 3f;
            PlayerCollision.Ignore(col);
            var b = go.AddComponent<Bottle>();
            b.thrower = gameObject;
            Destroy(go, 15f);
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.Jump, 0.35f, 1.3f);
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowAmmo($"BOTTLES {bottles}", 1.2f);
        }
    }
}
