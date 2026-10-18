using UnityEngine;

namespace ScaredyCat
{
    [RequireComponent(typeof(Collider))]
    public sealed class OwnerActivityTrigger : MonoBehaviour
    {
        public OwnerRagdoll owner;
        public GameFlow flow;
        bool activated;
        bool playerInside;

        void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<CatMotor>()) return;
            playerInside = true;
            TryActivate();
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<CatMotor>()) playerInside = false;
        }

        void Update()
        {
            if (playerInside) TryActivate();
        }

        void TryActivate()
        {
            if (!flow) flow = FindFirstObjectByType<GameFlow>();
            if (flow && flow.State != GameState.Playing) return;
            if (activated || !owner || owner.State != OwnerState.Animated || !owner.brain) return;
            activated = true;
            owner.brain.BeginFreeRoam();
        }
    }
}
