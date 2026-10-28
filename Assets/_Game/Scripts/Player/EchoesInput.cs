using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Player
{
    /// <summary>
    /// All player input defined in code (no .inputactions asset). Keyboard/mouse first, gamepad as fallback.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class EchoesInput : MonoBehaviour
    {
        InputActionMap map;
        InputAction move, look, lookStick, sprint, crouchToggle, crouchHold, jump, leanLeft, leanRight,
                    interact, fire, aim, reload, throwItem, flashlight, pause, holster, heal;

        public Vector2 Move { get; private set; }
        /// <summary>Look delta in degrees-equivalent units for this frame (mouse pixels or scaled stick).</summary>
        public Vector2 LookDelta { get; private set; }
        public bool SprintHeld => Acts && sprint.IsPressed();
        public bool CrouchHeld => Acts && crouchHold.IsPressed();
        public bool CrouchTogglePressed => Acts && crouchToggle.WasPressedThisFrame();
        public bool JumpPressed => Acts && jump.WasPressedThisFrame();
        public float Lean => Acts ? (leanRight.IsPressed() ? 1f : 0f) - (leanLeft.IsPressed() ? 1f : 0f) : 0f;
        public bool InteractPressed => Acts && interact.WasPressedThisFrame();
        public bool InteractHeld => Acts && interact.IsPressed();
        public bool FirePressed => Acts && fire.WasPressedThisFrame();
        public bool FireHeld => Acts && fire.IsPressed();
        public bool AimHeld => Acts && aim.IsPressed();
        public bool ReloadPressed => Acts && reload.WasPressedThisFrame();
        public bool ThrowPressed => Acts && throwItem.WasPressedThisFrame();
        public bool FlashlightPressed => Acts && flashlight.WasPressedThisFrame();
        public bool PausePressed => pause.WasPressedThisFrame();
        public bool HolsterPressed => Acts && holster.WasPressedThisFrame();
        public bool HealPressed => Acts && heal.WasPressedThisFrame();

        public bool InputLocked { get; set; }
        /// <summary>The pause menu is open: nothing reaches the player (set by UI.PauseMenu).</summary>
        public static bool Paused { get; set; }
        /// <summary>Cutscenes where Daniel can still look around (seated in the ambulance): no movement, no actions.</summary>
        public bool LookOnly { get; set; }
        bool Acts => !LookOnly && !InputLocked && !Paused;
        /// <summary>Fire and aim while manning a mounted gun (8d): they work under LookOnly, which keeps everything else off.</summary>
        public bool GunnerFireHeld => !InputLocked && !Paused && fire.IsPressed();
        public bool GunnerAimHeld => !InputLocked && !Paused && aim.IsPressed();

        void Awake()
        {
            map = new InputActionMap("Player");

            move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            look = map.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            lookStick = map.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            sprint = map.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");
            crouchToggle = map.AddAction("CrouchToggle", InputActionType.Button, "<Keyboard>/c");
            crouchToggle.AddBinding("<Gamepad>/buttonEast");
            crouchHold = map.AddAction("CrouchHold", InputActionType.Button, "<Keyboard>/leftCtrl");
            jump = map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");
            leanLeft = map.AddAction("LeanLeft", InputActionType.Button, "<Keyboard>/q");
            leanRight = map.AddAction("LeanRight", InputActionType.Button, "<Keyboard>/e");
            interact = map.AddAction("Interact", InputActionType.Button, "<Keyboard>/f");
            interact.AddBinding("<Gamepad>/buttonWest");
            fire = map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            fire.AddBinding("<Gamepad>/rightTrigger");
            aim = map.AddAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
            aim.AddBinding("<Gamepad>/leftTrigger");
            reload = map.AddAction("Reload", InputActionType.Button, "<Keyboard>/r");
            reload.AddBinding("<Gamepad>/buttonNorth");
            throwItem = map.AddAction("Throw", InputActionType.Button, "<Keyboard>/g");
            throwItem.AddBinding("<Gamepad>/rightShoulder");
            flashlight = map.AddAction("Flashlight", InputActionType.Button, "<Keyboard>/t");
            flashlight.AddBinding("<Gamepad>/dpad/up");
            pause = map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");
            holster = map.AddAction("Holster", InputActionType.Button, "<Keyboard>/1");
            holster.AddBinding("<Keyboard>/x");
            holster.AddBinding("<Gamepad>/dpad/left");
            heal = map.AddAction("Heal", InputActionType.Button, "<Keyboard>/h");
            heal.AddBinding("<Gamepad>/dpad/down");
        }

        void OnEnable() => map.Enable();
        void OnDisable() => map.Disable();
        void OnDestroy() => map.Dispose();

        void Update()
        {
            if (InputLocked || Paused)
            {
                Move = Vector2.zero;
                LookDelta = Vector2.zero;
                return;
            }
            Move = LookOnly ? Vector2.zero : Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            // Mouse delta is per-frame already; stick is a rate and must be scaled by time.
            LookDelta = look.ReadValue<Vector2>() + lookStick.ReadValue<Vector2>() * (Time.deltaTime * 1800f);
        }
    }
}
