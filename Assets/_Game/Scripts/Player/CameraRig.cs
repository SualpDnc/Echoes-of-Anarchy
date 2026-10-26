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
        public float bobVertical = 0.045f;
        public float bobLateral = 0.03f;
        public float bobRoll = 0.9f;

        [Header("Lean")]
        public float leanDistance = 0.4f;
        public float leanRoll = 11f;
        public float leanSpeed = 7f;

        [Header("Handheld noise")]
        public float idleSway = 0.35f;
        public float fatigueSway = 1.6f;

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

        public bool IsAiming { get; set; }
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
            if (Cursor.lockState != CursorLockMode.Locked) return;

            // --- Look ---
            Vector2 d = input.LookDelta * sensitivity * (IsAiming ? 0.7f : 1f);
            yaw += d.x;
            pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
            float k = 1f - Mathf.Exp(-lookSmoothing * dt);
            smYaw = Mathf.LerpAngle(smYaw, yaw, k);
            smPitch = Mathf.Lerp(smPitch, pitch, k);

            // --- Recoil spring (critically-damped-ish) ---
            recoil += recoilVel * dt;
            recoilVel += (-recoil * 160f - recoilVel * 18f) * dt;

            transform.rotation = Quaternion.Euler(0f, smYaw, 0f);
            pivot.localRotation = Quaternion.Euler(smPitch - recoil.x, recoil.y, 0f);

            // --- Mount height follows crouch ---
            float mountY = body.CurrentHeight - mountDrop;
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
            float targetBob = body.IsGrounded ? Mathf.Clamp01(body.HorizontalSpeed / body.walkSpeed) : 0f;
            bobWeight = Mathf.Lerp(bobWeight, targetBob, 1f - Mathf.Exp(-8f * dt));
            float stepT = (body.StepIndex + body.StepPhase) * Mathf.PI; // one PI per step
            float sprintMul = body.IsSprinting ? 1.6f : body.IsCrouching ? 0.6f : 1f;
            float aimMul = IsAiming ? 0.35f : 1f;
            float bw = bobWeight * sprintMul * aimMul;
            // Vertical dips on each footfall (|sin|), lateral sways once per two steps.
            float bobY = -Mathf.Abs(Mathf.Sin(stepT)) * bobVertical * bw + bobVertical * 0.5f * bw;
            float bobX = Mathf.Sin(stepT * 0.5f) * bobLateral * bw;
            float bobR = Mathf.Sin(stepT * 0.5f) * bobRoll * bw;

            // --- Landing spring ---
            landVel += (-landOffset * 120f - landVel * 14f) * dt;
            landOffset += landVel * dt;

            // --- Handheld / breathing noise; worse when exhausted ---
            float fatigue = 1f - body.Stamina01;
            float t = Time.time;
            float swayAmp = (idleSway + fatigueSway * fatigue * fatigue) * aimMul;
            float breath = Mathf.Sin(t * Mathf.Lerp(1.4f, 3.6f, fatigue)) * (0.15f + fatigue * 0.6f);
            float nx = (Mathf.PerlinNoise(noiseSeed, t * 0.45f) - 0.5f) * 2f * swayAmp + breath * aimMul;
            float ny = (Mathf.PerlinNoise(noiseSeed + 13.7f, t * 0.4f) - 0.5f) * 2f * swayAmp;
            float nz = (Mathf.PerlinNoise(noiseSeed + 27.1f, t * 0.35f) - 0.5f) * 2f * swayAmp * 0.6f;

            // --- Trauma shake ---
            trauma = Mathf.MoveTowards(trauma, 0f, dt * 0.9f);
            float shake = trauma * trauma;
            float sx = (Mathf.PerlinNoise(noiseSeed + 50f, t * 22f) - 0.5f) * 2f * 6f * shake;
            float sy = (Mathf.PerlinNoise(noiseSeed + 60f, t * 22f) - 0.5f) * 2f * 6f * shake;
            float sz = (Mathf.PerlinNoise(noiseSeed + 70f, t * 22f) - 0.5f) * 2f * 8f * shake;

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
