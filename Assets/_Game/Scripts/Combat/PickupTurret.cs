using System;
using UnityEngine;
using Echoes.Audio;
using Echoes.Player;
using Echoes.Rendering;

namespace Echoes.Combat
{
    /// <summary>
    /// 8d: the belt-fed machine gun on a pintle in the bed of the Smilers' pickup. Daniel mans it (Mount): he stands in
    /// the bed behind it, the gun follows his view, left click fires a steady stream (infinite ammo: the owner's call),
    /// right click steadies the aim. Rounds are hitscan with tracers, a muzzle flash, recoil and the rattle; they hurt
    /// people and vehicles alike (anything with a Health). Runs before the camera rig so Daniel's place behind the gun
    /// is set for the frame the camera is placed.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PickupTurret : MonoBehaviour
    {
        [Header("Fire")]
        public float interval = 0.085f;
        public float damage = 45f;
        public float range = 300f;
        public float spread = 0.9f, aimedSpread = 0.35f;

        public bool Mounted { get; private set; }
        /// <summary>Raised on every round (the sequence counts them and keeps the noise going).</summary>
        public event Action Fired;
        public Transform Gun => gunYaw;

        const float BedY = 1.3f;
        static readonly Vector3 PivotLocal = new Vector3(0f, BedY + 1.12f, -1.9f);

        Transform gunYaw, gunPitch, recoilT, muzzle, mount, flashQuad;
        Light flashLight;
        GameObject player;
        CameraRig rig;
        EchoesInput input;
        WeaponPistol pistol;
        float next, flashTimer, kick;
        readonly Transform[] tracers = new Transform[8];
        readonly float[] tracerUntil = new float[8];
        int tracerNext;

        /// <summary>Builds the gun on its pintle (called once, after the pickup is made).</summary>
        public void Build()
        {
            var gunmetal = RuntimeMaterials.Lit(new Color(0.05f, 0.05f, 0.055f), 0.45f, 0.7f);
            var olive = RuntimeMaterials.Lit(new Color(0.16f, 0.17f, 0.11f), 0.3f, 0.2f);
            var brass = RuntimeMaterials.Lit(new Color(0.55f, 0.42f, 0.18f), 0.6f, 0.9f);
            // The pintle: a post bolted to the bed.
            PrimitiveMeshes.Part(transform, "Pintle", PrimitiveType.Cylinder, new Vector3(0f, BedY + 0.55f, PivotLocal.z), new Vector3(0.09f, 0.55f, 0.09f), gunmetal);
            PrimitiveMeshes.Part(transform, "PintleFoot", PrimitiveType.Cube, new Vector3(0f, BedY + 0.03f, PivotLocal.z), new Vector3(0.4f, 0.06f, 0.4f), gunmetal);

            gunYaw = new GameObject("GunYaw").transform;
            gunYaw.SetParent(transform, false);
            gunYaw.localPosition = PivotLocal;
            gunPitch = new GameObject("GunPitch").transform;
            gunPitch.SetParent(gunYaw, false);
            recoilT = new GameObject("Recoil").transform;
            recoilT.SetParent(gunPitch, false);
            var g = recoilT;
            PrimitiveMeshes.Part(g, "Receiver", PrimitiveType.Cube, new Vector3(0f, 0.04f, -0.12f), new Vector3(0.15f, 0.17f, 0.62f), gunmetal);
            PrimitiveMeshes.Part(g, "FeedCover", PrimitiveType.Cube, new Vector3(0f, 0.14f, -0.05f), new Vector3(0.13f, 0.04f, 0.3f), gunmetal);
            PrimitiveMeshes.Part(g, "Jacket", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0.42f), new Vector3(0.075f, 0.24f, 0.075f), olive, Quaternion.Euler(90f, 0f, 0f));
            PrimitiveMeshes.Part(g, "Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0.75f), new Vector3(0.035f, 0.38f, 0.035f), gunmetal, Quaternion.Euler(90f, 0f, 0f));
            PrimitiveMeshes.Part(g, "FlashHider", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 1.15f), new Vector3(0.05f, 0.04f, 0.05f), gunmetal, Quaternion.Euler(90f, 0f, 0f));
            PrimitiveMeshes.Part(g, "Cradle", PrimitiveType.Cube, new Vector3(0f, -0.08f, -0.05f), new Vector3(0.1f, 0.1f, 0.25f), gunmetal);
            // Spade grips and the butterfly trigger at the back.
            foreach (float s in new[] { -1f, 1f })
                PrimitiveMeshes.Part(g, "Grip", PrimitiveType.Cylinder, new Vector3(s * 0.11f, 0.02f, -0.5f), new Vector3(0.035f, 0.08f, 0.035f), olive);
            PrimitiveMeshes.Part(g, "GripBar", PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.46f), new Vector3(0.26f, 0.03f, 0.03f), gunmetal);
            // The ammo can on the left and the belt into the feed tray.
            PrimitiveMeshes.Part(g, "AmmoCan", PrimitiveType.Cube, new Vector3(-0.2f, -0.02f, -0.05f), new Vector3(0.14f, 0.2f, 0.3f), olive);
            for (int i = 0; i < 5; i++)
                PrimitiveMeshes.Part(g, "Round", PrimitiveType.Cube, new Vector3(-0.12f + i * 0.022f, 0.11f + i * 0.006f, -0.02f), new Vector3(0.012f, 0.012f, 0.07f), brass, null, false);
            // A gun shield: a plate with a sighting notch.
            foreach (float s in new[] { -1f, 1f })
                PrimitiveMeshes.Part(g, "Shield", PrimitiveType.Cube, new Vector3(s * 0.33f, 0.1f, 0.2f), new Vector3(0.48f, 0.55f, 0.02f), olive, Quaternion.Euler(0f, s * -12f, 0f));
            PrimitiveMeshes.Part(g, "ShieldLow", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0.21f), new Vector3(0.2f, 0.15f, 0.02f), olive);

            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(g, false);
            muzzle.localPosition = new Vector3(0f, 0.05f, 1.22f);
            var lg = new GameObject("MuzzleLight", typeof(Light));
            lg.transform.SetParent(muzzle, false);
            flashLight = lg.GetComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.7f, 0.4f);
            flashLight.range = 14f;
            flashLight.intensity = 9f;
            flashLight.shadows = LightShadows.None;
            flashLight.enabled = false;
            var flashMat = RuntimeMaterials.Lit(Color.black, 0f, 0f, new Color(1f, 0.75f, 0.4f) * 14f);
            flashQuad = new GameObject("Flash").transform;
            flashQuad.SetParent(muzzle, false);
            foreach (float a in new[] { 0f, 60f, 120f })
                PrimitiveMeshes.Part(flashQuad, "Petal", PrimitiveType.Cube, new Vector3(0f, 0f, 0.12f), new Vector3(0.05f, 0.3f, 0.26f), flashMat, Quaternion.Euler(0f, 0f, a), false);
            flashQuad.gameObject.SetActive(false);

            var tracerMat = RuntimeMaterials.Lit(Color.black, 0f, 0f, new Color(1f, 0.6f, 0.25f) * 10f);
            for (int i = 0; i < tracers.Length; i++)
            {
                tracers[i] = PrimitiveMeshes.Part(null, "Tracer", PrimitiveType.Cube, Vector3.zero, Vector3.one, tracerMat, null, false).transform;
                tracers[i].SetParent(transform.parent, false);
                tracers[i].gameObject.SetActive(false);
            }

            // Where Daniel stands: moved round the pintle every frame so he is always behind the gun.
            mount = new GameObject("GunnerMount").transform;
            mount.SetParent(transform, false);
            mount.localPosition = new Vector3(0f, BedY, PivotLocal.z - 0.7f);
            mount.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        /// <summary>Daniel takes the gun. `facingYaw` (world) is where the mount looks straight ahead.</summary>
        public void Mount(GameObject who, float facingYaw)
        {
            if (Mounted || who == null) return;
            player = who;
            rig = who.GetComponent<CameraRig>();
            input = who.GetComponent<EchoesInput>();
            pistol = who.GetComponent<WeaponPistol>();
            mount.rotation = Quaternion.Euler(0f, facingYaw, 0f);
            PlaceMount(0f);
            if (pistol != null) pistol.Stowed = true;
            if (input != null) input.LookOnly = true;
            if (rig != null) rig.Mount(mount, 178f, 35f, 42f);
            Mounted = true;
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.SlideRack, 0.9f, 0.7f);
        }

        public void Dismount()
        {
            if (!Mounted) return;
            Mounted = false;
            if (rig != null) { rig.IsAiming = false; rig.Unmount(); }
            if (input != null) input.LookOnly = false;
            if (pistol != null) pistol.Stowed = false;
        }

        /// <summary>Keeps Daniel 0.7 m behind the pintle along his view (the mount itself stays the same heading).</summary>
        void PlaceMount(float relYaw)
        {
            Vector3 pivot = transform.TransformPoint(new Vector3(PivotLocal.x, BedY, PivotLocal.z));
            Vector3 fwd = Quaternion.Euler(0f, mount.eulerAngles.y + relYaw, 0f) * Vector3.forward;
            mount.position = pivot - fwd * 0.7f;
        }

        void LateUpdate()
        {
            if (gunYaw == null) return;
            float dt = Time.deltaTime;
            if (Mounted && rig != null)
            {
                PlaceMount(rig.Yaw);
                // The gun points where Daniel looks.
                Transform cam = rig.cam.transform;
                Vector3 target = Physics.Raycast(cam.position + cam.forward * 1.6f, cam.forward, out RaycastHit h, range, ~0, QueryTriggerInteraction.Ignore)
                                 ? h.point : cam.position + cam.forward * range;
                Vector3 d = target - gunYaw.position;
                Vector3 flat = Vector3.ProjectOnPlane(d, transform.up);
                if (flat.sqrMagnitude > 0.001f)
                {
                    gunYaw.rotation = Quaternion.Slerp(gunYaw.rotation, Quaternion.LookRotation(flat, transform.up), 1f - Mathf.Exp(-30f * dt));
                    float pitch = -Mathf.Atan2(Vector3.Dot(d, transform.up), flat.magnitude) * Mathf.Rad2Deg;
                    gunPitch.localRotation = Quaternion.Slerp(gunPitch.localRotation, Quaternion.Euler(Mathf.Clamp(pitch, -35f, 45f), 0f, 0f), 1f - Mathf.Exp(-30f * dt));
                }
                bool aiming = input != null && input.GunnerAimHeld;
                rig.IsAiming = aiming;
                if (input != null && input.GunnerFireHeld && Time.time >= next)
                {
                    next = Time.time + interval;
                    Fire(cam, aiming);
                }
            }
            kick = Mathf.MoveTowards(kick, 0f, dt * 1.2f);
            recoilT.localPosition = new Vector3(0f, 0f, -kick);
            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f) { flashLight.enabled = false; flashQuad.gameObject.SetActive(false); }
            }
            for (int i = 0; i < tracers.Length; i++)
                if (tracers[i] != null && tracers[i].gameObject.activeSelf && Time.time > tracerUntil[i]) tracers[i].gameObject.SetActive(false);
        }

        void Fire(Transform cam, bool aiming)
        {
            float s = aiming ? aimedSpread : spread;
            Vector3 dir = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), cam.forward) * (Quaternion.AngleAxis(UnityEngine.Random.Range(0f, s), cam.right) * cam.forward);
            Vector3 end = cam.position + dir * range;
            if (Raycast(cam.position, dir, out RaycastHit hit))
            {
                end = hit.point;
                var info = new DamageInfo { amount = damage, point = hit.point, direction = dir, force = 500f, source = player, fromPlayer = true };
                Hitbox.Damage(hit.collider, info);
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic) hit.rigidbody.AddForceAtPosition(dir * 9f, hit.point, ForceMode.Impulse);
                ImpactFX.Spawn(hit, dir);
            }

            // A tracer from the muzzle to the impact, for a few frames.
            var tr = tracers[tracerNext];
            tracerUntil[tracerNext] = Time.time + 0.035f;
            tracerNext = (tracerNext + 1) % tracers.Length;
            Vector3 from = muzzle.position, span = end - from;
            float len = Mathf.Min(span.magnitude, 40f);
            tr.SetPositionAndRotation(from + span.normalized * (len * 0.5f), Quaternion.LookRotation(span));
            tr.localScale = new Vector3(0.025f, 0.025f, Mathf.Max(0.1f, len - 3f));
            tr.gameObject.SetActive(len > 3.5f);

            var am = AudioManager.Instance;
            if (am != null) am.Play2D(SoundId.MachineGun, 0.85f, UnityEngine.Random.Range(0.95f, 1.05f));
            NoiseSystem.Emit(cam.position, 120f, NoiseKind.Gunshot, player, true);
            if (rig != null)
            {
                rig.AddRecoil(new Vector2(UnityEngine.Random.Range(0.25f, 0.45f) * (aiming ? 0.6f : 1f), UnityEngine.Random.Range(-0.3f, 0.3f)));
                rig.AddTrauma(0.035f);
            }
            kick = 0.035f;
            flashTimer = 0.03f;
            flashLight.enabled = true;
            flashLight.intensity = UnityEngine.Random.Range(7f, 11f);
            flashQuad.gameObject.SetActive(true);
            flashQuad.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            Fired?.Invoke();
        }

        /// <summary>The nearest hit that is neither Daniel nor this pickup.</summary>
        bool Raycast(Vector3 origin, Vector3 dir, out RaycastHit hit)
        {
            hit = default;
            float best = float.MaxValue;
            bool found = false;
            foreach (var h in Physics.RaycastAll(origin, dir, range, ~0, QueryTriggerInteraction.Ignore))
            {
                var t = h.collider.transform;
                if (t.IsChildOf(transform) || (player != null && t.IsChildOf(player.transform))) continue;
                if (h.distance < best) { best = h.distance; hit = h; found = true; }
            }
            return found;
        }

        void OnDestroy()
        {
            foreach (var t in tracers) if (t != null) Destroy(t.gameObject);
        }
    }
}
