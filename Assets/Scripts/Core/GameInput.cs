using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Haptics;
namespace ScaredyCat
{
    [DefaultExecutionOrder(-200)]
    public sealed class GameInput : MonoBehaviour
    {
        public InputActionAsset actions;
        public readonly ControlGate Controls = new ControlGate();
        InputActionAsset instance;
        InputActionMap player, system;
        InputAction move, look, jump, dash, attack, interact, advance;
        public Vector2 Move => player != null && player.enabled ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => player != null && player.enabled ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool LookIsGamepad => look?.activeControl?.device is Gamepad || look?.activeControl?.device is Joystick;
        public InputDevice LastInputDevice { get; private set; }
        public bool LastInputWasGamepad => LastInputDevice is Gamepad || LastInputDevice is Joystick;
        public bool LastInputSupportsHaptics => LastInputDevice is IDualMotorRumble;
        int warnedUnsupportedHapticsDeviceId = -1;
        public event Action Jumped, DashPressed, AttackPressed, AttackReleased, InteractStarted, InteractReleased, AdvancePressed, GameplayCancelled, PausePressed, SkipPressed, DebugPressed;
        void Awake()
        {
            instance = Instantiate(actions);
            player = instance.FindActionMap("Player", true); system = instance.FindActionMap("System", true);
            move = player.FindAction("Move", true); look = player.FindAction("Look", true);
            jump = player.FindAction("Jump", true); dash = player.FindAction("Dash", true); attack = player.FindAction("Attack", true);
            interact = player.FindAction("Interact", true);
            advance = system.AddAction("AdvanceDialogue", InputActionType.Button, "<Keyboard>/enter");
            advance.AddBinding("<Mouse>/leftButton");
            advance.AddBinding("<Gamepad>/buttonSouth");
            move.performed += RememberDevice; look.performed += RememberDevice;
            dash.performed += context => { RememberDevice(context); if (!Controls.IsLocked) DashPressed?.Invoke(); };
            jump.performed += context => { RememberDevice(context); Jumped?.Invoke(); };
            attack.started += context => { RememberDevice(context); if (!Controls.IsLocked) AttackPressed?.Invoke(); };
            attack.canceled += context => { RememberDevice(context); if (!Controls.IsLocked && player.enabled) AttackReleased?.Invoke(); };
            interact.started += context => { RememberDevice(context); if (!Controls.IsLocked) InteractStarted?.Invoke(); };
            interact.canceled += context => { RememberDevice(context); InteractReleased?.Invoke(); };
            advance.performed += context => { RememberDevice(context); AdvancePressed?.Invoke(); };
            system.FindAction("Pause", true).performed += context => { RememberDevice(context); PausePressed?.Invoke(); };
            system.FindAction("Skip", true).performed += context => { RememberDevice(context); SkipPressed?.Invoke(); };
            system.FindAction("Debug", true).performed += _ => DebugPressed?.Invoke();
            Controls.Changed += UpdateGate;
        }
        void OnEnable() { system?.Enable(); UpdateGate(Controls.IsLocked); }
        void OnDisable() { GameplayCancelled?.Invoke(); instance?.Disable(); }
        void UpdateGate(bool locked)
        {
            if (player == null || !isActiveAndEnabled) return;
            if (locked) { GameplayCancelled?.Invoke(); player.Disable(); } else player.Enable();
        }
        void OnApplicationFocus(bool focus) { if (!focus) GameplayCancelled?.Invoke(); }
        void RememberDevice(InputAction.CallbackContext context)
        {
            LastInputDevice = context.control.device;
            if (LastInputDevice is Joystick && !(LastInputDevice is IDualMotorRumble) &&
                warnedUnsupportedHapticsDeviceId != LastInputDevice.deviceId)
            {
                warnedUnsupportedHapticsDeviceId = LastInputDevice.deviceId;
                Debug.LogWarning($"'{LastInputDevice.displayName}' is recognized as a generic Joystick without Unity's IDualMotorRumble support. Input works, but Unity cannot send its vibration until the controller is recognized as a haptics-capable Gamepad/layout.", this);
            }
        }
        void OnDestroy() { Controls.Changed -= UpdateGate; if (instance) Destroy(instance); }
    }
}
