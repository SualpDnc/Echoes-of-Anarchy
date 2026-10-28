using System.Collections;
using UnityEngine;
using Echoes.Audio;
using Echoes.Player;
using Echoes.Rendering;

namespace Echoes.Combat
{
    /// <summary>
    /// Close-quarters strike on the interact key: a pistol-whip when the gun is out, a gloved punch otherwise.
    /// Damage lands at the contact frame of the swing. Quiet compared to a gunshot; a blow to the head hurts, but it takes more than one.
    /// </summary>
    public class PlayerMelee : MonoBehaviour
    {
        public const float SwingTime = 0.55f;
        const float ContactAt = 0.4f; // fraction of the swing where the blow connects

        public float reach = 1.9f;
        [Tooltip("Halved from 40 at the owner's request: a blow staggers, it rarely ends a fight on its own.")]
        public float strikeDamage = 20f; // renamed from `damage` so scenes built with the old 40 pick up the new value
        public float cooldown = 0.75f;

        CameraRig rig;
        WeaponPistol pistol;
        Transform fist;
        float next;

        void Awake()
        {
            rig = GetComponent<CameraRig>();
            TryGetComponent(out pistol);
        }

        void Start() => BuildFist();

        /// <summary>Right fist + sleeve viewmodel for unarmed strikes (same nitrile glove and EMS sleeve as the gun hands).</summary>
        void BuildFist()
        {
            fist = new GameObject("FistViewModel").transform;
            fist.SetParent(rig.cam.transform, false);
            var glove = RuntimeMaterials.Lit(new Color(0.07f, 0.13f, 0.42f), 0.55f);
            var sleeve = RuntimeMaterials.Lit(new Color(0.025f, 0.03f, 0.055f), 0.2f);
            var band = RuntimeMaterials.Lit(new Color(0.55f, 0.56f, 0.5f), 0.85f, 0.3f);
            PrimitiveMeshes.Part(fist, "Fist", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.075f, 0.07f, 0.09f), glove, null, false);
            PrimitiveMeshes.Part(fist, "Knuckles", PrimitiveType.Capsule, new Vector3(0f, 0.012f, 0.035f), new Vector3(0.03f, 0.035f, 0.03f), glove, Quaternion.Euler(0f, 0f, 90f), false);
            PrimitiveMeshes.Part(fist, "Wrist", PrimitiveType.Capsule, new Vector3(0f, -0.01f, -0.06f), new Vector3(0.05f, 0.04f, 0.05f), glove, Quaternion.Euler(90f, 0f, 0f), false);
            PrimitiveMeshes.Part(fist, "Sleeve", PrimitiveType.Capsule, new Vector3(0.02f, -0.03f, -0.24f), new Vector3(0.08f, 0.17f, 0.08f), sleeve, Quaternion.Euler(80f, -8f, 0f), false);
            PrimitiveMeshes.Part(fist, "CuffBand", PrimitiveType.Cylinder, new Vector3(0f, -0.015f, -0.1f), new Vector3(0.082f, 0.008f, 0.082f), band, Quaternion.Euler(80f, -8f, 0f), false);
            fist.gameObject.SetActive(false);
        }

        /// <summary>The living enemy hitbox in front of the camera within reach, if any.</summary>
        public Hitbox FindTarget()
        {
            Transform cam = rig.cam.transform;
            var hits = Physics.SphereCastAll(cam.position, 0.3f, cam.forward, reach, ~0, QueryTriggerInteraction.Ignore);
            Hitbox best = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (!h.collider.TryGetComponent(out Hitbox hb) || hb.owner == null || hb.owner.IsDead || hb.owner.meleeImmune) continue;
                // Prefer the head when it's in the cast — that's where a swing lands.
                float d = h.distance - (hb.part == BodyPart.Head ? 0.3f : 0f);
                if (d < bestDist) { bestDist = d; best = hb; }
            }
            return best;
        }

        public bool Ready => Time.time >= next && (pistol == null || !pistol.IsBusy);

        public void Strike()
        {
            if (!Ready) return;
            next = Time.time + cooldown;
            bool armed = pistol != null && pistol.IsDrawn && pistol.TryMeleeSwing();
            StartCoroutine(Swing(armed));
        }

        IEnumerator Swing(bool armed)
        {
            if (!armed) fist.gameObject.SetActive(true);
            bool connected = false;
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.BatSwing, 0.3f, armed ? 1.4f : 1.7f);
            for (float t = 0f; t < SwingTime; t += Time.deltaTime)
            {
                float p = t / SwingTime;
                if (!armed) PoseFist(p);
                if (!connected && p >= ContactAt)
                {
                    connected = true;
                    Connect(armed);
                }
                yield return null;
            }
            fist.gameObject.SetActive(false);
        }

        /// <summary>Rises from below frame, cocks back, drives forward and slightly left, then drops away.</summary>
        void PoseFist(float p)
        {
            Vector3 low = new Vector3(0.2f, -0.42f, 0.12f), cocked = new Vector3(0.17f, -0.2f, 0.12f), hit = new Vector3(0.03f, -0.1f, 0.45f);
            Vector3 lowRot = new Vector3(40f, -10f, 0f), cockRot = new Vector3(10f, -15f, -20f), hitRot = new Vector3(0f, -5f, -80f);
            Vector3 pos; Vector3 rot;
            if (p < 0.3f) { float k = Mathf.SmoothStep(0f, 1f, p / 0.3f); pos = Vector3.Lerp(low, cocked, k); rot = Vector3.Lerp(lowRot, cockRot, k); }
            else if (p < ContactAt) { float k = Mathf.SmoothStep(0f, 1f, (p - 0.3f) / (ContactAt - 0.3f)); pos = Vector3.Lerp(cocked, hit, k); rot = Vector3.Lerp(cockRot, hitRot, k); }
            else { float k = Mathf.SmoothStep(0f, 1f, (p - ContactAt) / (1f - ContactAt)); pos = Vector3.Lerp(hit, low, k); rot = Vector3.Lerp(hitRot, lowRot, k); }
            fist.localPosition = pos;
            fist.localRotation = Quaternion.Euler(rot);
        }

        void Connect(bool armed)
        {
            var target = FindTarget();
            if (target == null) return; // whiffed
            Transform cam = rig.cam.transform;
            target.Hit(new DamageInfo
            {
                amount = strikeDamage * (armed ? 1f : 0.6f),
                point = target.transform.position,
                direction = cam.forward,
                force = 180f,
                source = gameObject,
                fromPlayer = true
            });
            var am = AudioManager.Instance;
            if (am != null)
            {
                am.Play2D(SoundId.BatHit, 0.8f, Random.Range(1.05f, 1.25f));
                am.Play2D(SoundId.ImpactFlesh, 0.6f);
            }
            rig.AddRecoil(new Vector2(-1.2f, Random.Range(-1.5f, 1.5f)));
            rig.AddTrauma(0.15f);
            NoiseSystem.Emit(target.transform.position, 7f, NoiseKind.Impact, gameObject, true);
        }
    }
}
