using UnityEngine;
using UnityEngine.AI;
namespace ScaredyCat
{
    public enum OwnerState { Animated, Ragdoll, Recovering }
    [RequireComponent(typeof(NavMeshAgent), typeof(ImpulseAttribution))]
    public sealed class OwnerRagdoll : MonoBehaviour, ISwatReceiver
    {
        public Animator animator;
        public OwnerBrain brain;
        public CapsuleCollider standingCollider;
        public Rigidbody[] bones;
        public Transform hips;
        public AgroMeter agro;
        public FeedbackDirector feedback;
        public float impulseMultiplier = 7, recoveryDelay = 1.8f, maximumDownTime = 5, blendDuration = .55f;
        public LayerMask recoveryObstacles = ~((1 << 10) | (1 << 11) | (1 << 12));
        public OwnerState State { get; private set; }
        NavMeshAgent agent;
        ImpulseAttribution attribution;
        Vector3 spawn;
        Quaternion spawnRotation;
        Vector3[] fromPositions, targetPositions;
        Quaternion[] fromRotations, targetRotations;
        float downAt, hitAt, recoveringAt;
        int cycle;
        void Awake()
        {
            agent = GetComponent<NavMeshAgent>(); attribution = GetComponent<ImpulseAttribution>(); spawn = transform.position; spawnRotation = transform.rotation;
            fromPositions = new Vector3[bones.Length]; fromRotations = new Quaternion[bones.Length];
            targetPositions = new Vector3[bones.Length]; targetRotations = new Quaternion[bones.Length];
            SetPhysical(false);
        }
        public void ReceiveHit(HitContext hit)
        {
            if (bones == null || bones.Length == 0 || State == OwnerState.Recovering) return;
            attribution.Mark(hit.Instigator, Time.timeAsDouble + 6);
            hitAt = Time.time;
            if (State == OwnerState.Animated)
            {
                if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
                agent.enabled = false; animator.enabled = false; standingCollider.enabled = false;
                SetPhysical(true); State = OwnerState.Ragdoll; downAt = Time.time;
                if (agro) agro.Report(new MischiefEvent(GetInstanceID(), MischiefKind.OwnerKnockdown, hit.Instigator != null, ++cycle));
            }
            Rigidbody nearest = bones[0];
            foreach (var bone in bones)
            {
                bone.GetComponent<ImpulseAttribution>()?.CopyFrom(attribution);
                if ((bone.worldCenterOfMass - hit.Point).sqrMagnitude < (nearest.worldCenterOfMass - hit.Point).sqrMagnitude) nearest = bone;
            }
            Vector3 force = (hit.Direction + Vector3.up * .35f).normalized * hit.Impulse * impulseMultiplier;
            nearest.AddForceAtPosition(force, hit.Point, ForceMode.Impulse);
            bones[0].AddForce(force * .5f, ForceMode.Impulse);
        }
        void Update()
        {
            if (State != OwnerState.Ragdoll) return;
            bool settled = Time.time - hitAt > recoveryDelay && bones[0].linearVelocity.sqrMagnitude < 1.5f;
            if ((settled || Time.time - downAt > maximumDownTime || hips.position.y < spawn.y - 8) && TryRecoveryPosition(out var position))
                BeginRecovery(position);
        }
        public bool TryRecoveryPosition(out Vector3 position)
        {
            Vector3[] centers = { hips ? hips.position : transform.position, brain ? brain.LastSafePosition : spawn, spawn };
            Vector3[] offsets = { Vector3.zero, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (var center in centers)
            foreach (var offset in offsets)
            {
                if (!NavMesh.SamplePosition(center + offset, out var sample, 2, agent.areaMask)) continue;
                if (Physics.CheckCapsule(sample.position + Vector3.up * .31f, sample.position + Vector3.up * 1.35f,
                    .27f, recoveryObstacles, QueryTriggerInteraction.Ignore)) continue;
                position = sample.position; return true;
            }
            position = default; return false;
        }
        void BeginRecovery(Vector3 position)
        {
            for (int i = 0; i < bones.Length; i++) { fromPositions[i] = bones[i].position; fromRotations[i] = bones[i].rotation; }
            SetPhysical(false); transform.position = position;
            animator.enabled = true; animator.Rebind(); animator.Update(0);
            State = OwnerState.Recovering; recoveringAt = Time.time;
            BlendPose(0);
        }
        void LateUpdate()
        {
            if (State != OwnerState.Recovering) return;
            float t = Mathf.Clamp01((Time.time - recoveringAt) / blendDuration);
            BlendPose(Mathf.SmoothStep(0, 1, t));
            if (t >= 1)
            {
                State = OwnerState.Animated; standingCollider.enabled = true;
                agent.enabled = true; agent.Warp(transform.position); attribution.Clear();
            }
        }
        void BlendPose(float t)
        {
            for (int i = 0; i < bones.Length; i++) { targetPositions[i] = bones[i].transform.position; targetRotations[i] = bones[i].transform.rotation; }
            for (int i = 0; i < bones.Length; i++)
                bones[i].transform.SetPositionAndRotation(Vector3.Lerp(fromPositions[i], targetPositions[i], t), Quaternion.Slerp(fromRotations[i], targetRotations[i], t));
        }
        void SetPhysical(bool physical)
        {
            if (physical)
            {
                // Adjacent and crossing limbs should not fight the joints during exaggerated launches.
                for (int i = 0; i < bones.Length; i++)
                for (int j = i + 1; j < bones.Length; j++)
                    Physics.IgnoreCollision(bones[i].GetComponent<Collider>(), bones[j].GetComponent<Collider>(), true);
            }
            foreach (var bone in bones)
            {
                if (!physical && !bone.isKinematic) { bone.linearVelocity = Vector3.zero; bone.angularVelocity = Vector3.zero; }
                bone.isKinematic = !physical;
                foreach (var collider in bone.GetComponents<Collider>()) collider.enabled = physical;
            }
        }
        public void ForceCalmAt(Vector3 position, Quaternion rotation)
        {
            agent.enabled = false; SetPhysical(false); transform.SetPositionAndRotation(position, rotation);
            animator.enabled = true; animator.Rebind(); animator.Update(0); State = OwnerState.Animated;
            standingCollider.enabled = true; if (brain) brain.Stay = true; attribution.Clear();
        }
        public void ResetOwner()
        {
            ForceCalmAt(spawn, spawnRotation);
            if (TryRecoveryPosition(out var position)) transform.position = position;
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(transform.position);
            if (brain) brain.Stay = false;
        }
    }
}
