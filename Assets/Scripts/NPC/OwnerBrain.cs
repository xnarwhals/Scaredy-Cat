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
        public AnimationClip macarenaDance;
        public AnimationClip endingSit;
        [Range(0, 1)] public float macarenaChance = .3f;
        [Min(.5f)] public float macarenaPauseDuration = 3.2f;
        [Min(.5f)] public float roamingSpeed = 2.25f;
        [Min(0)] public float pauseAtDestination = .35f;
        public bool Stay;
        NavMeshAgent agent;
        RuntimeAnimatorController locomotionController;
        AnimatorOverrideController danceController;
        AnimatorOverrideController macarenaController;
        AnimatorOverrideController sittingController;
        float waitUntil;
        float[] lastVisited;
        int destination = -1;
        float pathStarted;
        Vector3 lastProgressPosition;
        float lastProgressAt;
        public Vector3 LastSafePosition { get; private set; }
        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = roamingSpeed;
            if (animator) locomotionController = animator.runtimeAnimatorController;
            danceController = OverrideIdle(introDance);
            macarenaController = OverrideIdle(macarenaDance);
            sittingController = OverrideIdle(endingSit);
            LastSafePosition = transform.position; waitUntil = Time.time + 1;
        }
        void OnDestroy()
        {
            if (danceController) Destroy(danceController);
            if (macarenaController) Destroy(macarenaController);
            if (sittingController) Destroy(sittingController);
        }
        AnimatorOverrideController OverrideIdle(AnimationClip replacement)
        {
            if (!animator || !locomotionController || !replacement) return null;
            var controller = new AnimatorOverrideController(locomotionController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            bool replacedIdle = false;
            for (int i = 0; i < overrides.Count; i++)
            {
                var original = overrides[i].Key;
                if (!original || original.name != "Idle") continue;
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
                replacedIdle = true;
                break;
            }
            if (replacedIdle) { controller.ApplyOverrides(overrides); return controller; }
            Destroy(controller); return null;
        }
        public void BeginSitting()
        {
            Stay = true;
            EndPauseDance();
            if (agent && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            if (!animator || !sittingController) return;
            animator.runtimeAnimatorController = sittingController;
            animator.Rebind(); animator.Update(0); animator.SetFloat("Speed", 0);
        }
        public void BeginIntroDance()
        {
            Stay = true;
            EndPauseDance();
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
            Stay = false; waitUntil = Time.time;
            destination = -1;
            lastVisited = new float[wanderPoints == null ? 0 : wanderPoints.Length];
            for (int i = 0; i < lastVisited.Length; i++) lastVisited[i] = -1000;
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
            if (Stay) { EndPauseDance(); if (agent.hasPath) agent.ResetPath(); return; }
            if (Time.time < waitUntil || agent.pathPending) return;
            if (destination >= 0 && agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete &&
                agent.remainingDistance > agent.stoppingDistance + .15f)
            {
                if (Vector3.Distance(transform.position, lastProgressPosition) > .35f)
                { lastProgressPosition = transform.position; lastProgressAt = Time.time; }
                if (Time.time - lastProgressAt < 2.5f && Time.time - pathStarted < 14f) return;
            }
            if (destination >= 0)
            {
                // Record arrival even if the agent has already cleared its path.
                // A failed route is also deferred so another room gets a turn.
                lastVisited[destination] = Time.time;
                destination = -1;
                agent.ResetPath();
                waitUntil = Time.time + pauseAtDestination;
                if (macarenaController && animator && Random.value < macarenaChance)
                {
                    animator.runtimeAnimatorController = macarenaController;
                    animator.Rebind(); animator.Update(0); animator.SetFloat("Speed", 0);
                    waitUntil = Time.time + Mathf.Max(pauseAtDestination, macarenaPauseDuration);
                }
                return;
            }
            ChooseDestination();
        }
        void ChooseDestination()
        {
            EndPauseDance();
            if (wanderPoints == null || wanderPoints.Length == 0) return;
            if (lastVisited == null || lastVisited.Length != wanderPoints.Length)
            {
                lastVisited = new float[wanderPoints.Length];
                for (int i = 0; i < lastVisited.Length; i++) lastVisited[i] = -1000;
            }
            int best = -1;
            float oldestVisit = float.MaxValue;
            Vector3 bestPosition = default;
            var path = new NavMeshPath();
            for (int i = 0; i < wanderPoints.Length; i++)
            {
                var point = wanderPoints[i];
                if (!point || !NavMesh.SamplePosition(point.position, out var hit, 1.5f, agent.areaMask)) continue;
                if (Vector3.Distance(transform.position, hit.position) < 1f) continue;
                if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                if (lastVisited[i] >= oldestVisit) continue;
                best = i; oldestVisit = lastVisited[i]; bestPosition = hit.position;
            }
            if (best < 0) { waitUntil = Time.time + 1; return; }
            destination = best;
            pathStarted = lastProgressAt = Time.time;
            lastProgressPosition = transform.position;
            agent.SetDestination(bestPosition);
        }
        void EndPauseDance()
        {
            if (!animator || !macarenaController || animator.runtimeAnimatorController != macarenaController) return;
            animator.runtimeAnimatorController = locomotionController;
            animator.Rebind(); animator.Update(0); animator.SetFloat("Speed", 0);
        }
    }
}
