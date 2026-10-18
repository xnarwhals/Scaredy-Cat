using System;
using UnityEngine;
namespace ScaredyCat
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class CatMotor : MonoBehaviour
    {
        public MovementSettings settings;
        public GameInput input;
        public Transform head;
        public LayerMask groundMask = ~((1 << 8) | (1 << 10) | (1 << 11) | (1 << 12));
        public bool Grounded { get; private set; }
        public Rigidbody Body { get; private set; }
        public Quaternion ViewRotation => Quaternion.Euler(pitch, yaw, 0);
        public event Action<float> Landed;
        public float SensitivityMultiplier { get; set; } = 1;
        Vector3 spawn, normal = Vector3.up;
        Vector2 manualMove;
        bool manualSprint;
        float yaw, pitch, groundedAt = -100, jumpAt = -100, jumpedAt = -100, previousVertical;
        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.constraints = RigidbodyConstraints.FreezeRotation;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.useGravity = false;
            spawn = transform.position; yaw = transform.eulerAngles.y;
        }
        void OnEnable() { if (input) { input.Jumped += RequestJump; input.GameplayCancelled += CancelInput; } }
        void OnDisable() { if (input) { input.Jumped -= RequestJump; input.GameplayCancelled -= CancelInput; } }
        public void RequestJump() { if (!input || !input.Controls.IsLocked) jumpAt = Time.time; }
        public void SetMovement(Vector2 direction, bool sprint = false) { manualMove = direction; manualSprint = sprint; }
        void CancelInput() { jumpAt = -100; manualMove = Vector2.zero; manualSprint = false; }
        void Update()
        {
            if (!settings) return;
            if (input && !input.Controls.IsLocked)
            {
                Vector2 look = input.Look * settings.mouseSensitivity * SensitivityMultiplier;
                yaw += look.x; pitch = Mathf.Clamp(pitch - look.y, -82, 82);
            }
            if (transform.position.y < spawn.y - 12) Teleport(spawn, yaw);
        }
        void FixedUpdate()
        {
            if (!settings) return;
            bool wasGrounded = Grounded;
            RaycastHit ground = default;
            Grounded = Time.time - jumpedAt > .12f && Body.linearVelocity.y < 1 &&
                Physics.SphereCast(Body.position + Vector3.up * .24f, .155f, Vector3.down,
                    out ground, .16f, groundMask, QueryTriggerInteraction.Ignore) &&
                Vector3.Angle(ground.normal, Vector3.up) <= settings.slopeLimit;
            normal = Grounded ? ground.normal : Vector3.up;
            if (Grounded)
            {
                groundedAt = Time.time;
                if (!wasGrounded && previousVertical < -2) Landed?.Invoke(-previousVertical);
            }
            Vector2 move = input ? input.Move : manualMove;
            bool sprint = input ? input.Sprint : manualSprint;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(move.x, 0, move.y);
            direction = Vector3.ClampMagnitude(direction, 1);
            Vector3 target = direction * (sprint ? settings.sprintSpeed : settings.speed);
            if (Grounded) target = Vector3.ProjectOnPlane(target, normal);
            var velocity = Body.linearVelocity;
            Vector3 planar = new Vector3(velocity.x, 0, velocity.z);
            float rate = Grounded ? (direction.sqrMagnitude < .01f ? settings.braking : settings.acceleration) : settings.airAcceleration;
            planar = Vector3.MoveTowards(planar, new Vector3(target.x, 0, target.z), rate * Time.fixedDeltaTime);
            velocity.x = planar.x; velocity.z = planar.z;
            if (Grounded && velocity.y <= 0) velocity.y = Mathf.Min(0, target.y);
            if (Time.time - jumpAt <= settings.jumpBuffer && Time.time - groundedAt <= settings.coyoteTime)
            {
                velocity.y = Mathf.Sqrt(2 * settings.gravity * settings.jumpHeight);
                jumpAt = groundedAt = -100; jumpedAt = Time.time; Grounded = false;
            }
            velocity.y -= settings.gravity * (velocity.y < 0 ? settings.fallMultiplier : 1) * Time.fixedDeltaTime;
            Body.linearVelocity = velocity;
            Body.MoveRotation(Quaternion.Euler(0, yaw, 0));
            if (Grounded && direction.sqrMagnitude > .1f) AssistStep(direction.normalized);
            previousVertical = velocity.y;
        }
        void AssistStep(Vector3 direction)
        {
            float distance = .22f;
            Vector3 feet = Body.position + Vector3.up * .06f;
            if (!Physics.Raycast(feet, direction, distance, groundMask, QueryTriggerInteraction.Ignore)) return;
            Vector3 upper = Body.position + Vector3.up * (settings.stepHeight + .05f);
            if (Physics.Raycast(upper, direction, distance + .05f, groundMask, QueryTriggerInteraction.Ignore)) return;
            if (!Physics.Raycast(upper + direction * distance, Vector3.down, out var step,
                settings.stepHeight, groundMask, QueryTriggerInteraction.Ignore) ||
                Vector3.Angle(step.normal, Vector3.up) > settings.slopeLimit) return;
            float lift = step.point.y - Body.position.y + .01f;
            if (lift > 0 && !Physics.SphereCast(Body.position + Vector3.up * .3f, .16f,
                Vector3.up, out _, lift, groundMask, QueryTriggerInteraction.Ignore))
                Body.MovePosition(Body.position + Vector3.up * Mathf.Min(lift, .08f));
        }
        public void Teleport(Vector3 position, float facing)
        {
            if (!Body) Body = GetComponent<Rigidbody>();
            Body.position = position; transform.position = position;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            yaw = facing; pitch = 0; Grounded = false; groundedAt = jumpAt = -100;
            Body.rotation = Quaternion.Euler(0, yaw, 0);
        }
    }
}
