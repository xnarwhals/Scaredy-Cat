using UnityEngine;
namespace ScaredyCat
{
    [CreateAssetMenu(menuName = "Scaredy Cat/Movement Settings")]
    public sealed class MovementSettings : ScriptableObject
    {
        public float speed = 4.2f, sprintSpeed = 6.4f;
        public float acceleration = 48, braking = 60, airAcceleration = 22;
        public float jumpHeight = 1.35f, gravity = 24, fallMultiplier = 1.35f;
        public float coyoteTime = .12f, jumpBuffer = .12f, slopeLimit = 50;
        public float stepHeight = .16f, mouseSensitivity = .085f, fieldOfView = 80;
        public float gamepadLookDegreesPerSecond = 170f;
    }
}
