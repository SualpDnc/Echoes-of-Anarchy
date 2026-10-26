using System;
using UnityEngine;

namespace Echoes.Player
{
    /// <summary>
    /// Weighty first-person locomotion: acceleration-based movement, stamina-limited sprint,
    /// crouch with ceiling check, and a distance-driven step cycle that drives camera bob and footsteps.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(EchoesInput))]
    public class FPSController : MonoBehaviour
    {
        [Header("Speeds (m/s)")]
        public float walkSpeed = 2.4f;
        public float sprintSpeed = 5.4f;
        public float crouchSpeed = 1.3f;
        public float backwardMultiplier = 0.65f;
        public float strafeMultiplier = 0.85f;

        [Header("Acceleration (m/s²)")]
        public float groundAccel = 11f;
        public float groundDecel = 15f;
        public float airAccel = 1.5f;

        [Header("Jump / Gravity")]
        public float jumpHeight = 0.8f;
        public float gravity = -19f;

        [Header("Crouch")]
        public float standHeight = 1.8f;
        public float crouchHeight = 1.15f;
        public float crouchLerpSpeed = 9f;

        [Header("Stamina (seconds of sprint)")]
        public float staminaMax = 6f;
        public float staminaRegenRate = 1.1f;
        public float staminaRegenDelay = 1.3f;
        public float jumpStaminaCost = 0.9f;

        [Header("Step cycle")]
        public float walkStride = 0.72f;
        public float sprintStride = 1.1f;
        public float crouchStride = 0.55f;

        [Header("Ground")]
        [Tooltip("Brief contact losses shorter than this (curbs, seams, high frame rates) don't count as airborne.")]
        public float groundedGraceTime = 0.12f;

        public event Action<float> Footstep;      // arg: intensity 0..1
        public event Action<float> Landed;        // arg: impact speed (m/s)
        public event Action Jumped;

        public CharacterController Controller { get; private set; }
        public Vector3 Velocity => velocity;
        public float HorizontalSpeed => new Vector3(velocity.x, 0f, velocity.z).magnitude;
        public float Speed01 => Mathf.Clamp01(HorizontalSpeed / sprintSpeed);
        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public float CrouchBlend { get; private set; }          // 0 standing .. 1 fully crouched
        public float Stamina01 => stamina / staminaMax;
        /// <summary>0..1 progress through the current step. Continuous: it only advances with distance walked.</summary>
        public float StepPhase => stepCycle - StepIndex;
        public int StepIndex { get; private set; }
        /// <summary>How loud the player currently is, 0..1. Used by AI hearing.</summary>
        public float CurrentNoise { get; private set; }
        public float CurrentHeight => Controller.height;

        EchoesInput input;
        Vector3 velocity;
        float stamina;
        float staminaCooldown;
        bool exhausted;
        bool crouchToggled;
        float stepCycle; // steps taken, fractional
        float peakFallSpeed;
        float airTime;
        float groundedGrace;
        bool wasGrounded;

        float CurrentStride => IsCrouching ? crouchStride : Mathf.Lerp(walkStride, sprintStride, Mathf.InverseLerp(walkSpeed, sprintSpeed, HorizontalSpeed));

        void Awake()
        {
            Controller = GetComponent<CharacterController>();
            input = GetComponent<EchoesInput>();
            stamina = staminaMax;
            Controller.height = standHeight;
            Controller.center = new Vector3(0f, standHeight * 0.5f, 0f);
            Controller.radius = 0.32f;
            Controller.stepOffset = 0.35f;
            Controller.slopeLimit = 50f;
            Controller.skinWidth = 0.04f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            // The controller's contact flag flickers for single frames on seams, curbs and at high frame rates.
            // Gameplay and camera use a debounced flag so sprint, FOV and bob don't toggle on and off.
            bool rawGrounded = Controller.isGrounded;
            groundedGrace = rawGrounded ? groundedGraceTime : groundedGrace - dt;
            IsGrounded = rawGrounded || (groundedGrace > 0f && velocity.y <= 0f);

            UpdateCrouch(dt);

            Vector2 mv = input.Move;
            bool wantsSprint = input.SprintHeld && mv.y > 0.4f && !IsCrouching && IsGrounded && !exhausted;
            IsSprinting = wantsSprint && stamina > 0f;

            UpdateStamina(dt);

            float targetSpeed = IsCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);
            Vector3 wish = transform.right * (mv.x * strafeMultiplier)
                         + transform.forward * (mv.y * (mv.y < 0f ? backwardMultiplier : 1f));
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            Vector3 targetVel = wish * targetSpeed;

            Vector3 hv = new Vector3(velocity.x, 0f, velocity.z);
            float accel = IsGrounded ? (targetVel.sqrMagnitude >= hv.sqrMagnitude ? groundAccel : groundDecel) : airAccel;
            hv = Vector3.MoveTowards(hv, targetVel, accel * dt);

            bool jumped = false;
            if (IsGrounded)
            {
                if (rawGrounded && velocity.y < 0f) velocity.y = -2f;
                if (input.JumpPressed && !IsCrouching && stamina > jumpStaminaCost * 0.5f)
                {
                    jumped = true;
                    groundedGrace = 0f;
                    velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
                    stamina = Mathf.Max(0f, stamina - jumpStaminaCost);
                    staminaCooldown = staminaRegenDelay;
                    Jumped?.Invoke();
                }
            }
            velocity.y += gravity * dt;
            velocity.x = hv.x;
            velocity.z = hv.z;

            CollisionFlags flags = Controller.Move(velocity * dt);
            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;

            // Stepping off a curb: snap down onto ground within step height instead of falling (and bouncing the camera).
            bool groundedNow = Controller.isGrounded;
            if (wasGrounded && !groundedNow && !jumped && velocity.y <= 0f
                && Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, out RaycastHit gh, Controller.stepOffset + 0.05f, ~0, QueryTriggerInteraction.Ignore))
            {
                Controller.Move(Vector3.down * gh.distance);
                groundedNow = Controller.isGrounded;
                if (groundedNow) velocity.y = -2f;
            }

            // Landing detection
            if (!groundedNow) { peakFallSpeed = Mathf.Max(peakFallSpeed, -velocity.y); airTime += dt; }
            if (groundedNow && !wasGrounded)
            {
                // Ignore one- or two-frame "airborne" blips on curbs and slopes; only real falls count as landings.
                if (peakFallSpeed > 2.5f && airTime > 0.2f) Landed?.Invoke(peakFallSpeed);
                airTime = 0f;
                peakFallSpeed = 0f;
            }
            wasGrounded = groundedNow;

            UpdateStepCycle(dt);
        }

        void UpdateCrouch(float dt)
        {
            if (input.CrouchTogglePressed) crouchToggled = !crouchToggled;
            // Sprinting forward cancels a toggled crouch.
            if (crouchToggled && input.SprintHeld && input.Move.y > 0.4f) crouchToggled = false;

            bool want = crouchToggled || input.CrouchHeld;
            // Stay down while something is overhead.
            if (!want && Controller.height < standHeight - 0.01f && !HasHeadroom()) want = true;
            IsCrouching = want;

            float targetH = want ? crouchHeight : standHeight;
            float h = Mathf.Lerp(Controller.height, targetH, 1f - Mathf.Exp(-crouchLerpSpeed * dt));
            if (Mathf.Abs(h - targetH) < 0.002f) h = targetH;
            Controller.height = h;
            Controller.center = new Vector3(0f, h * 0.5f, 0f);
            CrouchBlend = Mathf.InverseLerp(standHeight, crouchHeight, h);
        }

        bool HasHeadroom()
        {
            float r = Controller.radius * 0.95f;
            Vector3 start = transform.position + Vector3.up * (Controller.height - r);
            float dist = standHeight - Controller.height + 0.05f;
            return !Physics.SphereCast(start, r, Vector3.up, out _, dist, ~0, QueryTriggerInteraction.Ignore);
        }

        void UpdateStamina(float dt)
        {
            if (IsSprinting && HorizontalSpeed > walkSpeed * 0.9f)
            {
                stamina -= dt;
                staminaCooldown = staminaRegenDelay;
                if (stamina <= 0f) { stamina = 0f; exhausted = true; }
            }
            else
            {
                staminaCooldown -= dt;
                if (staminaCooldown <= 0f)
                    stamina = Mathf.Min(staminaMax, stamina + staminaRegenRate * dt * (IsCrouching ? 1.3f : 1f));
            }
            // Must recover a chunk before sprinting again — makes exhaustion felt.
            if (exhausted && stamina > staminaMax * 0.35f) exhausted = false;
        }

        void UpdateStepCycle(float dt)
        {
            float speed = HorizontalSpeed;
            // The cycle advances by distance / stride, so a stride change alters the pace, never the phase
            // (no sudden jumps in the camera bob). It simply holds when standing still or airborne.
            if (IsGrounded && speed > 0.15f)
            {
                stepCycle += speed * dt / CurrentStride;
                if (stepCycle >= StepIndex + 1)
                {
                    StepIndex = Mathf.FloorToInt(stepCycle);
                    float intensity = IsCrouching ? 0.2f : Mathf.Lerp(0.45f, 1f, Mathf.InverseLerp(walkSpeed, sprintSpeed, speed));
                    Footstep?.Invoke(intensity);
                }
            }

            float targetNoise = !IsGrounded ? 0.3f
                : speed < 0.15f ? 0f
                : IsCrouching ? 0.1f
                : IsSprinting ? 1f : 0.4f;
            CurrentNoise = Mathf.MoveTowards(CurrentNoise, targetNoise, dt * 3f);
        }

        /// <summary>Teleport safely (CharacterController must be disabled while moving it).</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            Controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Controller.enabled = true;
            velocity = Vector3.zero;
            if (TryGetComponent(out CameraRig rig)) rig.SetYawPitch(yaw, 0f);
        }

        public void AddImpulse(Vector3 v) => velocity += v;
    }
}
