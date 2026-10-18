using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace ScaredyCat
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class OwnerBrain : MonoBehaviour
    {
        public Animator animator;
        public OwnerRagdoll ragdoll;
        public Transform[] wanderPoints;
        public AnimationClip introDance;
        public bool Stay;
        NavMeshAgent agent;
        RuntimeAnimatorController locomotionController;
        AnimatorOverrideController danceController;
        float waitUntil;
        int next;
        bool waiting = true;
        public Vector3 LastSafePosition { get; private set; }
        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator) locomotionController = animator.runtimeAnimatorController;
            BuildDanceController();
            LastSafePosition = transform.position; waitUntil = Time.time + 1;
        }
        void OnDestroy()
        {
            if (danceController) Destroy(danceController);
        }
        void BuildDanceController()
        {
            if (!animator || !locomotionController || !introDance) return;
            danceController = new AnimatorOverrideController(locomotionController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            danceController.GetOverrides(overrides);
            bool replacedIdle = false;
            for (int i = 0; i < overrides.Count; i++)
            {
                var original = overrides[i].Key;
                if (!original || original.name != "Idle") continue;
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, introDance);
                replacedIdle = true;
                break;
            }
            if (replacedIdle) danceController.ApplyOverrides(overrides);
            else { Destroy(danceController); danceController = null; }
        }
        public void BeginIntroDance()
        {
            Stay = true;
            if (agent && agent.enabled)
            {
                if (agent.isOnNavMesh) agent.ResetPath();
                agent.enabled = false;
            }
            if (!animator || !danceController) return;
            animator.runtimeAnimatorController = danceController;
            animator.Rebind(); animator.Update(0);
            animator.SetFloat("Speed", 0);
        }
        public void BeginFreeRoam()
        {
            if (ragdoll && ragdoll.State != OwnerState.Animated) return;
            if (animator && danceController && animator.runtimeAnimatorController == danceController)
            {
                animator.runtimeAnimatorController = locomotionController;
                animator.Rebind(); animator.Update(0);
                animator.SetFloat("Speed", 0);
            }
            Stay = false; waiting = true; next = 0;
            if (!agent || agent.enabled || !NavMesh.SamplePosition(transform.position, out var start, .75f, agent.areaMask)) return;
            agent.enabled = true; agent.Warp(start.position);
        }
        void Update()
        {
            if (!agent.enabled && !Stay && (!ragdoll || ragdoll.State == OwnerState.Animated) &&
                NavMesh.SamplePosition(transform.position, out var start, .75f, agent.areaMask))
            { agent.enabled = true; agent.Warp(start.position); }
            if (!agent.enabled || !agent.isOnNavMesh || (ragdoll && ragdoll.State != OwnerState.Animated)) return;
            LastSafePosition = transform.position;
            if (animator && animator.isInitialized) animator.SetFloat("Speed", agent.velocity.magnitude, .12f, Time.deltaTime);
            if (Stay) { if (agent.hasPath) agent.ResetPath(); return; }
            if (agent.pathPending || Time.time < waitUntil) return;
            if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + .15f && agent.pathStatus == NavMeshPathStatus.PathComplete) return;
            if (!waiting)
            {
                agent.ResetPath(); waitUntil = Time.time + Random.Range(1.1f, 2.8f); waiting = true; return;
            }
            if (wanderPoints == null || wanderPoints.Length == 0) return;
            var point = wanderPoints[next++ % wanderPoints.Length];
            if (point && NavMesh.SamplePosition(point.position, out var hit, 1.5f, agent.areaMask))
            { agent.SetDestination(hit.position); waiting = false; }
        }
    }
}
