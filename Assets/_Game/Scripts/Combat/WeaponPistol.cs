using System.Collections;
using UnityEngine;
using Echoes.Audio;
using Echoes.Player;
using Echoes.Rendering;
using Echoes.UI;

namespace Echoes.Combat
{
    /// <summary>
    /// Daniel's pistol as seen through the bodycam: procedural viewmodel with nitrile-gloved hands, spring-driven recoil,
    /// slide cycling/lock-back, tactical vs empty reloads, casing ejection, muzzle flash that blooms the camera sensor.
    /// Bullets are hitscan from the camera centre with movement- and bloom-dependent spread.
    /// </summary>
    [DefaultExecutionOrder(10)] // after CameraRig
    public class WeaponPistol : MonoBehaviour
    {
        [Header("Ammo")]
        public int magSize = 7;
        public int ammoInMag = 7;
        public int reserve = 14;
        public bool startDrawn = false;

        [Header("Ballistics")]
        public float damage = 34f;
        public float range = 150f;
        public float fireInterval = 0.14f;
        public float hipSpread = 2.2f, aimSpread = 0.35f, moveSpread = 2.5f, bloomPerShot = 0.9f;

        enum State { Holstered, Drawing, Ready, Reloading, Holstering }

        CameraRig rig;
        EchoesInput input;
        FPSController body;
        Transform viewRoot, gunPivot;
        WeaponModels.PistolRig pistol;
        Light flashLight;
        Transform flashQuad;
        State state = State.Holstered;

        float raise;          // 0 holstered … 1 up
        float aim;            // 0 hip … 1 sights
        float lower;          // sprint / wall lowering
        float lastShot = -1f;
        float bloom;
        float slideBack;
        bool slideLocked;
        Vector3 kickPos, kickPosVel;
        Vector3 kickRot, kickRotVel;
        Vector2 sway;
        Vector3 reloadPos, reloadRot;
        Transform supportHand;
        Vector3 supportRest;
        float flashTimer;

        public bool IsDrawn => state == State.Ready || state == State.Reloading;
        public bool IsBusy => state == State.Reloading || state == State.Drawing || state == State.Holstering;
        public bool IsAimingAt(Vector3 point, float coneDegrees)
        {
            if (!IsDrawn) return false;
            Vector3 to = point - rig.cam.transform.position;
            return Vector3.Angle(rig.cam.transform.forward, to) < coneDegrees;
        }

        void Awake()
        {
            rig = GetComponent<CameraRig>();
            input = GetComponent<EchoesInput>();
            body = GetComponent<FPSController>();
        }

        void Start()
        {
            viewRoot = new GameObject("ViewModel").transform;
            viewRoot.SetParent(rig.cam.transform, false);
            gunPivot = new GameObject("GunPivot").transform;
            gunPivot.SetParent(viewRoot, false);
            pistol = WeaponModels.Pistol(gunPivot, true);
            WeaponModels.Hands(pistol.root, out supportHand);
            supportRest = supportHand.localPosition;

            var lg = new GameObject("MuzzleLight", typeof(Light));
            lg.transform.SetParent(pistol.muzzle, false);
            lg.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);
            flashLight = lg.GetComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.72f, 0.42f);
            flashLight.range = 12f;
            flashLight.intensity = 0f;
            flashLight.shadows = LightShadows.None;
            flashLight.enabled = false;

            var fq = PrimitiveMeshes.Part(pistol.muzzle, "MuzzleFlash", PrimitiveType.Quad, new Vector3(0f, 0f, 0.03f), Vector3.one * 0.12f,
                                          RuntimeMaterials.Particle("MuzzleFlash", ProcTextures.MuzzleStar(), true, 6f, 0.001f), null, false);
            flashQuad = fq.transform;
            fq.SetActive(false);

            if (startDrawn) { state = State.Ready; raise = 1f; }
            viewRoot.gameObject.SetActive(raise > 0.01f);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || viewRoot == null) return;
            HandleInput();

            // --- Pose blending ---
            float targetRaise = state == State.Holstered || state == State.Holstering ? 0f : 1f;
            raise = Mathf.MoveTowards(raise, targetRaise, dt / 0.45f);
            if (state == State.Drawing && raise >= 1f) state = State.Ready;
            if (state == State.Holstering && raise <= 0f) state = State.Holstered;
            viewRoot.gameObject.SetActive(raise > 0.01f);
            if (raise <= 0.01f) { rig.IsAiming = false; return; }

            bool wallClose = Physics.Raycast(rig.cam.transform.position, rig.cam.transform.forward, out RaycastHit wh, 0.55f, ~0, QueryTriggerInteraction.Ignore)
                             && !wh.collider.transform.IsChildOf(transform);
            float targetLower = body.IsSprinting || wallClose ? 1f : 0f;
            lower = Mathf.Lerp(lower, targetLower, 1f - Mathf.Exp(-10f * dt));
            bool wantsAim = input.AimHeld && state == State.Ready && lower < 0.3f;
            aim = Mathf.Lerp(aim, wantsAim ? 1f : 0f, 1f - Mathf.Exp(-12f * dt));
            rig.IsAiming = aim > 0.5f;

            // --- Springs: recoil kick and slide ---
            kickPosVel += (-kickPos * 260f - kickPosVel * 22f) * dt;
            kickPos += kickPosVel * dt;
            kickRotVel += (-kickRot * 220f - kickRotVel * 20f) * dt;
            kickRot += kickRotVel * dt;
            if (!slideLocked) slideBack = Mathf.MoveTowards(slideBack, 0f, dt / 0.05f);
            pistol.slide.localPosition = new Vector3(0f, 0f, -0.032f * slideBack);
            bloom = Mathf.MoveTowards(bloom, 0f, dt * 2.5f);

            // --- Sway lags behind the look; bob follows the real step cycle ---
            Vector2 look = input.LookDelta * 0.02f * (1f - aim * 0.7f);
            sway = Vector2.Lerp(sway, -look, 1f - Mathf.Exp(-8f * dt));
            sway = Vector2.ClampMagnitude(sway, 3f);
            float stepT = (body.StepIndex + body.StepPhase) * Mathf.PI;
            float bobAmt = Mathf.Clamp01(body.HorizontalSpeed / body.walkSpeed) * (1f - aim * 0.8f) * (body.IsSprinting ? 1.3f : 1f);
            float sn = Mathf.Sin(stepT);
            Vector3 bob = new Vector3(Mathf.Sin(stepT * 0.5f) * 0.008f, -sn * sn * 0.01f, 0f) * bobAmt; // sin²: no jolt at the footfall

            Vector3 hipPos = new Vector3(0.105f, -0.19f, 0.36f), aimPos = new Vector3(0f, -0.018f, 0.31f);
            Vector3 lowPos = new Vector3(0.1f, -0.3f, 0.26f), holsterPos = new Vector3(0.18f, -0.55f, 0.15f);
            Vector3 hipRot = new Vector3(4f, -3f, 0f), lowRot = new Vector3(38f, -22f, 8f), holsterRot = new Vector3(70f, -10f, 0f);

            Vector3 pos = Vector3.Lerp(hipPos, aimPos, aim);
            Vector3 rot = Vector3.Lerp(hipRot, Vector3.zero, aim);
            pos = Vector3.Lerp(pos, lowPos, lower);
            rot = Vector3.Lerp(rot, lowRot, lower);
            float hide = 1f - Mathf.SmoothStep(0f, 1f, raise);
            pos = Vector3.Lerp(pos, holsterPos, hide);
            rot = Vector3.Lerp(rot, holsterRot, hide);

            viewRoot.localPosition = pos + bob + kickPos + reloadPos;
            viewRoot.localRotation = Quaternion.Euler(rot + new Vector3(sway.y, sway.x, -sway.x * 0.6f) + kickRot + reloadRot);

            // --- Muzzle flash ---
            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f)
                {
                    flashLight.enabled = false;
                    flashQuad.gameObject.SetActive(false);
                }
            }
        }

        void HandleInput()
        {
            if (input.InputLocked) return;
            if (input.HolsterPressed)
            {
                if (state == State.Holstered) state = State.Drawing;
                else if (state == State.Ready) state = State.Holstering;
            }
            if (state != State.Ready) return;
            if (input.ReloadPressed)
            {
                if (ammoInMag < magSize && reserve > 0) StartCoroutine(Reload());
                else ShowAmmo();
            }
            if (input.FirePressed && Time.time - lastShot >= fireInterval && lower < 0.5f) Fire();
        }

        void Fire()
        {
            lastShot = Time.time;
            var am = AudioManager.Instance;
            if (ammoInMag <= 0)
            {
                if (am != null) am.Play2D(SoundId.DryFire, 0.6f);
                ShowAmmo();
                return;
            }
            ammoInMag--;

            Transform cam = rig.cam.transform;
            float spread = Mathf.Lerp(hipSpread, aimSpread, aim) + body.Speed01 * moveSpread + bloom;
            Vector3 dir = Quaternion.AngleAxis(Random.Range(0f, 360f), cam.forward) * (Quaternion.AngleAxis(Random.Range(0f, spread), cam.right) * cam.forward);
            bloom = Mathf.Min(bloom + bloomPerShot, 4f);

            if (SurfaceUtil.RaycastIgnoring(cam.position, dir, range, transform, out RaycastHit hit))
            {
                var info = new DamageInfo { amount = damage, point = hit.point, direction = dir, force = 320f, source = gameObject, fromPlayer = true };
                Hitbox.Damage(hit.collider, info);
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic) hit.rigidbody.AddForceAtPosition(dir * 6f, hit.point, ForceMode.Impulse);
                ImpactFX.Spawn(hit, dir);
            }

            if (am != null) am.Play2D(SoundId.GunPistolNear, 1f, Random.Range(0.97f, 1.03f));
            NoiseSystem.Emit(cam.position, 95f, NoiseKind.Gunshot, gameObject, true);

            // Feel.
            rig.AddRecoil(new Vector2(Random.Range(2.0f, 2.8f) * (1f - aim * 0.25f), Random.Range(-0.7f, 0.7f)));
            rig.AddTrauma(0.07f);
            kickPosVel += new Vector3(0f, 0.12f, -1.8f);
            kickRotVel += new Vector3(-Random.Range(170f, 230f), Random.Range(-30f, 30f), Random.Range(-40f, 40f));
            slideBack = 1f;
            slideLocked = ammoInMag == 0;

            flashTimer = 0.045f;
            flashLight.enabled = true;
            flashLight.intensity = Random.Range(6f, 9f);
            flashQuad.gameObject.SetActive(true);
            flashQuad.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            flashQuad.localScale = Vector3.one * Random.Range(0.09f, 0.15f);
            if (BodycamVolume.Instance != null) StartCoroutine(SensorBloom());

            EjectCasing();
        }

        IEnumerator SensorBloom()
        {
            // Cheap bodycam sensors over-expose for a frame or two on a muzzle flash, then recover.
            var v = BodycamVolume.Instance;
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                v.SetExposureOffset(Mathf.Lerp(0.9f, 0f, t / 0.12f));
                yield return null;
            }
            v.SetExposureOffset(0f);
        }

        void EjectCasing()
        {
            var go = new GameObject("Casing", typeof(MeshFilter), typeof(MeshRenderer), typeof(CapsuleCollider), typeof(Rigidbody), typeof(Casing));
            go.transform.SetPositionAndRotation(pistol.ejectPort.position, pistol.ejectPort.rotation * Quaternion.Euler(0f, 0f, 90f));
            go.transform.localScale = new Vector3(0.009f, 0.0095f, 0.009f);
            go.GetComponent<MeshFilter>().sharedMesh = PrimitiveMeshes.Get(PrimitiveType.Cylinder);
            go.GetComponent<MeshRenderer>().sharedMaterial = WeaponModels.Brass;
            var rb = go.GetComponent<Rigidbody>();
            rb.mass = 0.012f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Transform cam = rig.cam.transform;
            rb.linearVelocity = body.Velocity + cam.right * Random.Range(1.6f, 2.6f) + cam.up * Random.Range(1.4f, 2.2f) - cam.forward * 0.3f;
            rb.angularVelocity = Random.insideUnitSphere * 30f;
            PlayerCollision.Ignore(go.GetComponent<Collider>());
            Destroy(go, 10f);
        }

        /// <summary>
        /// A visible magazine change (owner: the reload has to be seen and heard). The gun comes up with the magwell
        /// toward the camera, the old magazine drops out and falls to the ground, the support hand brings a fresh one
        /// from the belt and slaps it home; on an empty gun it then racks the slide. Tactical 1.3 s, empty 1.7 s.
        /// </summary>
        IEnumerator Reload()
        {
            state = State.Reloading;
            bool empty = ammoInMag == 0;
            var am = AudioManager.Instance;
            float total = empty ? 1.7f : 1.3f;
            const float release = 0.12f, fetch = 0.3f, align = 0.5f, seat = 0.62f, back = 0.74f;
            const float rackGrab = 0.78f, rackPull = 0.86f, rackDone = 0.96f;
            Vector3 axis = Quaternion.Euler(16f, 0f, 0f) * Vector3.down; // down the grip, along the magwell
            Vector3 away = new Vector3(-0.07f, -0.26f, -0.1f);            // the belt pouch, out of view
            Vector3 under = axis * 0.09f;                                  // fresh mag lined up under the grip
            Vector3 cup = axis * 0.03f;                                    // the palm sits just under the base plate
            Vector3 overSlide = new Vector3(0.02f, 0.085f, -0.035f), pulled = overSlide + new Vector3(0f, 0f, -0.035f);
            GameObject fresh = null;
            bool released = false, dropped = false, pouched = false, scraped = false, seated = false, racked = !empty;

            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                float p = t / total;
                // Up and turned: muzzle tipped back so the bottom of the grip faces the camera.
                float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p / 0.14f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - p) / 0.16f));
                reloadPos = new Vector3(-0.06f, 0.14f, -0.06f) * w;
                reloadRot = new Vector3(-35f, 20f, 28f) * w;

                // The old magazine slides out of the grip, then falls free.
                if (!released && p >= release)
                {
                    released = true;
                    if (am != null) am.Play2D(SoundId.MagOut, 0.85f);
                }
                if (released && !dropped)
                {
                    float k = Mathf.Clamp01((p - release) / 0.06f);
                    pistol.mag.localPosition = axis * 0.08f * k;
                    if (k >= 1f) { dropped = true; DropMagazine(); }
                }

                // Support hand: off to the pouch, back with a fresh magazine, up into the grip, back on the gun.
                Vector3 hand;
                Vector3? magAt = null;
                if (p < 0.06f) hand = Vector3.zero;
                else if (p < fetch) hand = Vector3.Lerp(Vector3.zero, away, Smooth(p, 0.06f, fetch));
                else if (p < align) { magAt = Vector3.Lerp(away - cup, under, Smooth(p, fetch, align)); hand = magAt.Value + cup; }
                else if (p < seat) { magAt = Vector3.Lerp(under, Vector3.zero, Smooth(p, align, seat)); hand = magAt.Value + cup; }
                else if (p < back || !empty) hand = Vector3.Lerp(cup, Vector3.zero, Smooth(p, seat, back));
                else if (p < rackGrab) hand = Vector3.zero;
                else if (p < rackPull) hand = Vector3.Lerp(Vector3.zero, overSlide, Smooth(p, rackGrab, rackPull - 0.03f));
                else if (p < rackDone) hand = Vector3.Lerp(overSlide, pulled, Smooth(p, rackPull - 0.03f, rackPull));
                else hand = Vector3.Lerp(pulled, Vector3.zero, Smooth(p, rackDone - 0.04f, 1f));
                supportHand.localPosition = supportRest + hand;

                if (!pouched && p >= fetch - 0.05f)
                {
                    pouched = true;
                    if (am != null) am.Play2D(SoundId.MagPouch, 0.6f);
                    fresh = Instantiate(pistol.mag.gameObject, pistol.root);
                    fresh.transform.localPosition = away - cup; // in the hand, down at the belt
                    fresh.SetActive(true);
                }
                if (fresh != null && magAt.HasValue) fresh.transform.localPosition = magAt.Value;
                if (!scraped && p >= align)
                {
                    scraped = true;
                    if (am != null) am.Play2D(SoundId.MagIn, 0.7f);
                }
                if (!seated && p >= seat)
                {
                    seated = true;
                    if (fresh != null) Destroy(fresh);
                    pistol.mag.localPosition = Vector3.zero;
                    pistol.mag.gameObject.SetActive(true);
                    if (am != null) am.Play2D(SoundId.MagSeat, 0.9f);
                    kickPosVel += new Vector3(0f, 0.3f, 0f);
                }
                if (!racked && p >= rackPull)
                {
                    // Slide pulled back and let go: it slams forward on a round.
                    racked = true;
                    slideLocked = false;
                    slideBack = 1f;
                    if (am != null) am.Play2D(SoundId.SlideRack, 0.85f);
                    kickPosVel += new Vector3(0f, 0f, -0.5f);
                }
                yield return null;
            }
            if (fresh != null) Destroy(fresh);
            pistol.mag.localPosition = Vector3.zero;
            pistol.mag.gameObject.SetActive(true);
            supportHand.localPosition = supportRest;
            reloadPos = Vector3.zero;
            reloadRot = Vector3.zero;
            int need = magSize - ammoInMag;
            int take = Mathf.Min(need, reserve);
            ammoInMag += take;
            reserve -= take;
            slideLocked = false;
            state = State.Ready;
            ShowAmmo();
        }

        static float Smooth(float p, float a, float b) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, p));

        /// <summary>The spent magazine leaves the gun as a real object: it falls, bounces and clatters on the ground.</summary>
        void DropMagazine()
        {
            var old = Instantiate(pistol.mag.gameObject, pistol.mag.position, pistol.mag.rotation);
            old.name = "DroppedMag";
            pistol.mag.gameObject.SetActive(false);
            var col = old.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.07f, -0.062f);
            col.size = new Vector3(0.03f, 0.12f, 0.05f);
            var rb = old.AddComponent<Rigidbody>();
            rb.mass = 0.12f;
            rb.linearVelocity = pistol.mag.TransformDirection(Quaternion.Euler(16f, 0f, 0f) * Vector3.down) * 1.2f + body.Velocity;
            rb.angularVelocity = Random.insideUnitSphere * 6f;
            PlayerCollision.Ignore(col);
            StartCoroutine(MagClatter(old.transform));
            Destroy(old, 30f);
        }

        IEnumerator MagClatter(Transform mag)
        {
            // About when it reaches the ground from chest height.
            yield return new WaitForSeconds(0.42f);
            if (mag != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayAt(SoundId.MagDrop, mag.position, 0.75f, Random.Range(0.95f, 1.05f), 1f, 20f, false);
        }

        /// <summary>Pistol-whip swing on the viewmodel. Returns false if the gun can't swing right now.</summary>
        public bool TryMeleeSwing()
        {
            if (state != State.Ready) return false;
            StartCoroutine(MeleeSwing());
            return true;
        }

        IEnumerator MeleeSwing()
        {
            state = State.Reloading; // blocks firing and throwing during the swing
            const float dur = PlayerMelee.SwingTime;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p = t / dur;
                // Wind up (gun pulled back and up to the right), snap the butt forward-left, recover.
                Vector3 windPos = new Vector3(0.06f, 0.08f, -0.12f), hitPos = new Vector3(-0.12f, 0.1f, 0.22f);
                Vector3 windRot = new Vector3(-30f, 25f, 30f), hitRot = new Vector3(-75f, -35f, -55f);
                if (p < 0.3f)
                {
                    float k = Mathf.SmoothStep(0f, 1f, p / 0.3f);
                    reloadPos = windPos * k; reloadRot = windRot * k;
                }
                else if (p < 0.45f)
                {
                    float k = Mathf.SmoothStep(0f, 1f, (p - 0.3f) / 0.15f);
                    reloadPos = Vector3.Lerp(windPos, hitPos, k); reloadRot = Vector3.Lerp(windRot, hitRot, k);
                }
                else
                {
                    float k = Mathf.SmoothStep(0f, 1f, (p - 0.45f) / 0.55f);
                    reloadPos = Vector3.Lerp(hitPos, Vector3.zero, k); reloadRot = Vector3.Lerp(hitRot, Vector3.zero, k);
                }
                yield return null;
            }
            reloadPos = Vector3.zero;
            reloadRot = Vector3.zero;
            state = State.Ready;
        }

        void ShowAmmo()
        {
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowAmmo($"{ammoInMag} | {reserve}");
        }

        public void AddAmmo(int rounds)
        {
            reserve += rounds;
            ShowAmmo();
        }
    }
}
