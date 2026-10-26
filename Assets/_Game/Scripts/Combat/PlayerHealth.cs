using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Echoes.Audio;
using Echoes.Player;
using Echoes.Rendering;
using Echoes.UI;

namespace Echoes.Combat
{
    /// <summary>
    /// Player-side damage feedback and survival: hit flinch, red-edged stress on the sensor, heartbeat and limp when
    /// badly hurt, med-kit healing, and the bodycam "NO SIGNAL" death.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        public int medKits = 2;
        public float medKitHeal = 45f;
        public float healTime = 2.6f;

        Health health;
        CameraRig rig;
        EchoesInput input;
        FPSController body;
        AudioSource heartbeat;
        float hitPulse;
        float baseWalk, baseSprint;
        bool healing;

        void Awake()
        {
            health = GetComponent<Health>();
            rig = GetComponent<CameraRig>();
            input = GetComponent<EchoesInput>();
            body = GetComponent<FPSController>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            baseWalk = body.walkSpeed;
            baseSprint = body.sprintSpeed;
        }

        void OnDestroy()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Start()
        {
            if (AudioManager.Instance == null) return;
            heartbeat = gameObject.AddComponent<AudioSource>();
            heartbeat.clip = AudioManager.Instance.Clip(SoundId.Heartbeat);
            heartbeat.loop = true;
            heartbeat.spatialBlend = 0f;
            heartbeat.volume = 0f;
            heartbeat.Play();
        }

        void Update()
        {
            float hurt = 1f - health.Fraction;
            float critical = Mathf.Clamp01((hurt - 0.55f) / 0.45f);
            hitPulse = Mathf.MoveTowards(hitPulse, 0f, Time.deltaTime * 1.6f);
            if (BodycamVolume.Instance != null && !health.IsDead)
                BodycamVolume.Instance.SetStress(Mathf.Clamp01(Mathf.Max(hitPulse, critical * 0.55f)));

            if (heartbeat != null)
            {
                heartbeat.volume = Mathf.Lerp(heartbeat.volume, critical * 0.7f, 1f - Mathf.Exp(-2f * Time.deltaTime));
                heartbeat.pitch = Mathf.Lerp(1.1f, 1.7f, critical);
            }
            // Badly hurt: he can't run like he used to.
            body.walkSpeed = baseWalk * Mathf.Lerp(1f, 0.75f, critical);
            body.sprintSpeed = baseSprint * Mathf.Lerp(1f, 0.6f, critical);

            if (BodycamHUD.Instance != null) BodycamHUD.Instance.SetVitals(health.Fraction, medKits);
            if (input.HealPressed && !healing && !health.IsDead) StartCoroutine(UseMedKit());
        }

        void OnDamaged(DamageInfo info)
        {
            hitPulse = Mathf.Clamp01(hitPulse + 0.35f + info.amount / 60f);
            rig.AddTrauma(Mathf.Clamp(info.amount / 70f, 0.15f, 0.5f));
            rig.AddRecoil(new Vector2(Random.Range(-2f, 3f), Random.Range(-3f, 3f)));
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.ImpactFlesh, 0.8f, Random.Range(0.85f, 1f));
        }

        IEnumerator UseMedKit()
        {
            if (medKits <= 0)
            {
                if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowAmmo("NO MED KITS", 1.2f);
                yield break;
            }
            if (health.Fraction >= 0.99f) yield break;
            healing = true;
            medKits--;
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.Bandage, 0.7f);
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowAmmo("DRESSING WOUND…", healTime);
            float t = 0f;
            float startHp = health.Current;
            while (t < healTime)
            {
                // Taking more damage interrupts the dressing.
                if (health.Current < startHp - 0.1f) { healing = false; yield break; }
                t += Time.deltaTime;
                yield return null;
            }
            health.Heal(medKitHeal);
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowAmmo($"MED KITS {medKits}", 1.5f);
            healing = false;
        }

        void OnDied(DamageInfo info) => StartCoroutine(Death(info));

        IEnumerator Death(DamageInfo info)
        {
            input.InputLocked = true;
            if (TryGetComponent(out WeaponPistol w)) w.enabled = false;
            var pivot = rig.pivot;
            Vector3 startPos = pivot.localPosition;
            Quaternion startRot = pivot.localRotation;
            float side = Random.value < 0.5f ? -1f : 1f;
            rig.enabled = false;
            if (BodycamVolume.Instance != null) BodycamVolume.Instance.SetStress(1f);

            // The camera (and Daniel) goes down; the mount ends up sideways on the asphalt.
            for (float t = 0f; t < 1.1f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 1.1f);
                pivot.localPosition = Vector3.Lerp(startPos, new Vector3(0.2f * side, 0.18f, 0.3f), k);
                pivot.localRotation = Quaternion.Slerp(startRot, Quaternion.Euler(-10f, 25f * side, 80f * side), k);
                yield return null;
            }
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.BodyFall, 0.9f);
            yield return new WaitForSeconds(1.2f);
            var hud = BodycamHUD.Instance;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                if (hud != null) hud.SetFade(t / 1.5f);
                yield return null;
            }
            if (hud != null) hud.ShowCenter("NO SIGNAL", 10f);
            yield return new WaitForSeconds(2.5f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
