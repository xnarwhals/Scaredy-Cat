using UnityEngine;
namespace ScaredyCat
{
    [CreateAssetMenu(menuName = "Scaredy Cat/Attack Settings")]
    public sealed class AttackSettings : ScriptableObject
    {
        public float chargeDuration = .8f, swingDuration = .075f, recoveryDuration = .125f;
        public float reach = 1.05f, radius = .14f, tapImpulse = 1.8f, chargedImpulse = 7;
        public AnimationCurve chargeForce = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public LayerMask hitMask = ~((1 << 8) | (1 << 11) | (1 << 12));
    }
}
