using UnityEngine;

namespace Echoes.Player
{
    /// <summary>
    /// Chest-mounted bodycam rig. Yaw rotates the body, pitch rotates the pivot; the camera itself gets
    /// step bob, fatigue breathing, lean, landing spring, recoil and trauma shake layered on top.
    /// </summary>
    [RequireComponent(typeof(FPSController))]
    public class CameraRig : MonoBehaviour
    {
        public Transform pivot;
        public Camera cam;

        [Header("Look")]
        public float sensitivity = 0.075f;
        [Tooltip("Higher = snappier. Low values give a heavy, body-mounted feel.")]
        public float lookSmoothing = 22f;
        public float minPitch = -80f, maxPitch = 82f;

        [Header("Mount")]
        [Tooltip("Camera height below the top of the capsule (chest mount).")]
        public float mountDrop = 0.34f;
        public float forwardOffset = 0.12f;

        [Header("FOV")]
        public float baseFov = 76f;
        public float sprintFovKick = 5f;
        public float aimFov = 62f;

        [Header("Bob")]
        public float bobVertical = 0.04f;
        public float bobLateral = 0.025f;
        public float bobRoll = 0.7f;
        public float sprintBobMultiplier = 1.3f;

        [Header("Height smoothing")]
        [Tooltip("How fast the camera catches up with sudden body height changes (curb steps, capsule contact jitter).")]
        public float heightFollow = 16f;

        [Header("Lean")]
        public float leanDistance = 0.4f;
        public float leanRoll = 11f;
        public float leanSpeed = 7f;

        [Header("Handheld noise")]
        public float idleSway = 0.12f;
        public float fatigueSway = 0.6f;
        public float breathing = 0.06f;
        public float fatigueBreathing = 0.3f;

        FPSController body;
        EchoesInput input;
        float yaw, pitch, smYaw, smPitch;
        float lean;
        float landOffset, landVel;
        float trauma;
        Vector2 recoil, recoilVel;
        float bobWeight;
        float fovCurrent;
        float noiseSeed;
        float smoothY;
        bool hasSmoothY;

        Transform mount;
        float mountYawLimit, mountPitchUp, mountPitchDown;

        public bool IsAiming { get; set; }
        public bool IsMounted => mount != null;
        /// <summary>Engine and road vibration in degrees while riding in a vehicle (set by the vehicle). Keep it small.</summary>
        public float Vibration { get; set; }
        public Vector3 Forward => cam.transform.forward;
        public float Yaw => smYaw;
        public float Pitch => smPitch;

        void Awake()
        {
            body = GetComponent<FPSController>();
            input = GetComponent<EchoesInput>();
            yaw = smYaw = transform.eulerAngles.y;
            fovCurrent = baseFov;
            noiseSeed = Random.value * 100f;
            body.Landed += OnLanded;
        }

        void OnDestroy() => body.Landed -= OnLanded;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SetYawPitch(float newYaw, float newPitch)
        {
            yaw = smYaw = newYaw;
            pitch = smPitch = newPitch;
        }

        /// <summary>
        /// Seats Daniel at a mount point (its position is the body root, its forward is straight ahead). Movement stops;
        /// look is relative to the mount and limited, so he can turn his head in the cab but not spin round.
        /// </summary>
        public void Mount(Transform seat, float yawLimit = 105f, float pitchUp = 50f, float pitchDown = 38f)
        {
            mount = seat;
            mountYawLimit = yawLimit;
            mountPitchUp = pitchUp;
            mountPitchDown = pitchDown;
            yaw = smYaw = 0f;
            pitch = smPitch = 0f;
            body.enabled = false;
            body.Controller.enabled = false;
            hasSmoothY = false;
        }

        /// <summary>Back on his feet: world yaw from the current view, body physics on again.</summary>
        public void Unmount()
        {
            if (mount == null) return;
            float worldYaw = (mount.rotation * Quaternion.Euler(0f, smYaw, 0f)).eulerAngles.y;
            mount = null;
            Vibration = 0f;
            transform.rotation = Quaternion.Euler(0f, worldYaw, 0f);
            yaw = smYaw = worldYaw;
            body.Controller.enabled = true;
            body.enabled = true;
            hasSmoothY = false;
        }

        /// <summary>Kick in degrees (x = pitch up, y = yaw).</summary>
        public void AddRecoil(Vector2 kick) => recoilVel += kick * 22f;

        /// <summary>Trauma 0..1 — shake magnitude scales with trauma².</summary>
        public void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        void OnLanded(float impact)
        {
            landVel -= Mathf.Clamp(impact * 0.09f, 0.2f, 1.4f);
            AddTrauma(Mathf.Clamp01((impact - 4f) * 0.08f));
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Temporary cursor handling until the pause menu exists (phase 7).
            if (input.PausePressed) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else if (Cursor.lockState != CursorLockMode.Locked && UnityEngine.InputSystem.Mouse.current != null
                     && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                // Still ride along when the cursor is free, or the vehicle drives off without the camera.
                if (mount != null) transform.SetPositionAndRotation(mount.position, mount.rotation * Quaternion.Euler(0f, smYaw, 0f));
                return;
            }

            // --- Look ---
            Vector2 d = input.LookDelta * sensitivity * (IsAiming ? 0.7f : 1f);
            yaw += d.x;
            pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
            if (mount != null)
            {
                yaw = Mathf.Clamp(yaw, -mountYawLimit, mountYawLimit);
                pitch = Mathf.Clamp(pitch, -mountPitchUp, mountPitchDown);
            }
            float k = 1f - Mathf.Exp(-lookSmoothing * dt);
            smYaw = Mathf.LerpAngle(smYaw, yaw, k);
            smPitch = Mathf.Lerp(smPitch, pitch, k);

            // --- Recoil spring (critically-damped-ish) ---
            recoil += recoilVel * dt;
            recoilVel += (-recoil * 160f - recoilVel * 18f) * dt;

            if (mount != null) transform.SetPositionAndRotation(mount.position, mount.rotation * Quaternion.Euler(0f, smYaw, 0f));
            else transform.rotation = Quaternion.Euler(0f, smYaw, 0f);
            pivot.localRotation = Quaternion.Euler(smPitch - recoil.x, recoil.y, 0f);

            // --- Mount height follows crouch; sudden body height changes are eased ---
            // The capsule snaps up curbs (step offset) and can jitter by millimetres on contact; a body-worn
            // camera rides those out. Jumps and falls are followed closely so the camera never lags far behind.
            float bodyY = transform.position.y;
            if (mount != null) smoothY = bodyY; // seated: the camera rides the cab exactly
            if (!hasSmoothY || Mathf.Abs(smoothY - bodyY) > 1f) { smoothY = bodyY; hasSmoothY = true; }
            smoothY = Mathf.Lerp(smoothY, bodyY, 1f - Mathf.Exp(-(body.IsGrounded ? heightFollow : heightFollow * 2f) * dt));
            smoothY = Mathf.Clamp(smoothY, bodyY - 0.4f, bodyY + 0.4f);
            float mountY = body.CurrentHeight - mountDrop + (smoothY - bodyY);
            pivot.localPosition = new Vector3(0f, mountY, forwardOffset);

            // --- Lean with wall check ---
            float targetLean = body.IsSprinting ? 0f : input.Lean;
            if (Mathf.Abs(targetLean) > 0.01f)
            {
                Vector3 side = transform.right * Mathf.Sign(targetLean);
                if (Physics.SphereCast(pivot.position, 0.18f, side, out RaycastHit hit, leanDistance + 0.1f, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.gameObject != gameObject)
                    targetLean *= Mathf.Clamp01((hit.distance - 0.1f) / leanDistance);
            }
            lean = Mathf.Lerp(lean, targetLean, 1f - Mathf.Exp(-leanSpeed * dt));

            // --- Step bob, phase-locked to real footsteps ---
            float targetBob = mount != null ? 0f : body.IsGrounded ? Mathf.Clamp01(body.HorizontalSpeed / body.walkSpeed) : 0f;
            bobWeight = Mathf.Lerp(bobWeight, targetBob, 1f - Mathf.Exp(-8f * dt));
            float stepT = (body.StepIndex + body.StepPhase) * Mathf.PI; // one PI per step
            float sprintMul = body.IsSprinting ? sprintBobMultiplier : body.IsCrouching ? 0.6f : 1f;
            float aimMul = IsAiming ? 0.35f : 1f;
            float bw = bobWeight * sprintMul * aimMul;
            // Vertical dip once per step (sin², smooth, no jolt at the footfall), lateral sway once per two steps.
            float sn = Mathf.Sin(stepT);
            float bobY = (0.5f - sn * sn) * bobVertical * bw;
            float bobX = Mathf.Sin(stepT * 0.5f) * bobLateral * bw;
            float bobR = Mathf.Sin(stepT * 0.5f) * bobRoll * bw;

            // --- Landing spring ---
            landVel += (-landOffset * 120f - landVel * 14f) * dt;
            landOffset += landVel * dt;

            // --- Handheld / breathing noise; worse when exhausted ---
            float fatigue = 1f - body.Stamina01;
            float t = Time.time;
            float swayAmp = (idleSway + fatigueSway * fatigue * fatigue) * aimMul;
            float breath = Mathf.Sin(t * Mathf.Lerp(1.4f, 3.2f, fatigue)) * (breathing + fatigue * fatigueBreathing);
            float nx = (Mathf.PerlinNoise(noiseSeed, t * 0.45f) - 0.5f) * 2f * swayAmp + breath * aimMul;
            float ny = (Mathf.PerlinNoise(noiseSeed + 13.7f, t * 0.4f) - 0.5f) * 2f * swayAmp;
            float nz = (Mathf.PerlinNoise(noiseSeed + 27.1f, t * 0.35f) - 0.5f) * 2f * swayAmp * 0.6f;

            // --- Trauma shake ---
            trauma = Mathf.MoveTowards(trauma, 0f, dt * 0.9f);
            float shake = trauma * trauma;
            float sx = (Mathf.PerlinNoise(noiseSeed + 50f, t * 22f) - 0.5f) * 2f * 6f * shake;
            float sy = (Mathf.PerlinNoise(noiseSeed + 60f, t * 22f) - 0.5f) * 2f * 6f * shake;
            float sz = (Mathf.PerlinNoise(noiseSeed + 70f, t * 22f) - 0.5f) * 2f * 8f * shake;
            // Riding: a fine engine/road buzz on top (degrees, set by the vehicle).
            sx += (Mathf.PerlinNoise(noiseSeed + 80f, t * 17f) - 0.5f) * 2f * Vibration;
            sz += (Mathf.PerlinNoise(noiseSeed + 90f, t * 13f) - 0.5f) * 2f * Vibration * 0.6f;

            Vector3 camPos = new Vector3(bobX + lean * leanDistance, bobY + landOffset * 0.12f - Mathf.Abs(lean) * 0.06f, 0f);
            cam.transform.localPosition = camPos;
            cam.transform.localRotation = Quaternion.Euler(nx + sx, ny + sy, bobR - lean * leanRoll + nz + sz);

            // --- FOV ---
            float targetFov = IsAiming ? aimFov : baseFov + (body.IsSprinting ? sprintFovKick * Mathf.Clamp01(body.Speed01 * 1.3f) : 0f);
            fovCurrent = Mathf.Lerp(fovCurrent, targetFov, 1f - Mathf.Exp(-7f * dt));
            cam.fieldOfView = fovCurrent;
        }
    }
}
