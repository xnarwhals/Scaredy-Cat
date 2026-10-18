using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace ScaredyCat
{
    [DefaultExecutionOrder(-200)]
    public sealed class GameInput : MonoBehaviour
    {
        public InputActionAsset actions;
        public readonly ControlGate Controls = new ControlGate();
        InputActionAsset instance;
        InputActionMap player, system;
        InputAction move, look, jump, sprint, attack;
        public Vector2 Move => player != null && player.enabled ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => player != null && player.enabled ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool Sprint => sprint != null && sprint.IsPressed();
        public event Action Jumped, AttackPressed, AttackReleased, GameplayCancelled, PausePressed, SkipPressed, DebugPressed;
        void Awake()
        {
            instance = Instantiate(actions);
            player = instance.FindActionMap("Player", true); system = instance.FindActionMap("System", true);
            move = player.FindAction("Move", true); look = player.FindAction("Look", true);
            jump = player.FindAction("Jump", true); sprint = player.FindAction("Sprint", true); attack = player.FindAction("Attack", true);
            jump.performed += _ => Jumped?.Invoke();
            attack.started += _ => { if (!Controls.IsLocked) AttackPressed?.Invoke(); };
            attack.canceled += _ => { if (!Controls.IsLocked && player.enabled) AttackReleased?.Invoke(); };
            system.FindAction("Pause", true).performed += _ => PausePressed?.Invoke();
            system.FindAction("Skip", true).performed += _ => SkipPressed?.Invoke();
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
        void OnDestroy() { Controls.Changed -= UpdateGate; if (instance) Destroy(instance); }
    }
}
