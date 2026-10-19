using UnityEngine;
namespace ScaredyCat
{
    [CreateAssetMenu(menuName = "Scaredy Cat/Feedback Settings")]
    public sealed class FeedbackSettings : ScriptableObject
    {
        [Range(0, 1)] public float cameraMotion = .6f;
        [Range(0, 1)] public float volume = .65f;
        public float impactPause = .045f;
        [Range(.02f, 1f)] public float preImpactTimeScale = .2f;
        [Min(0)] public float preImpactSlowDuration = .12f;
        [Range(.01f, 1f)] public float impactStopTimeScale = .02f;
        [Min(0)] public float impactStopDuration = .08f;
        public AudioClip swish, impact, shatter, landing, reward, acceptance;
        public AudioClip uiClick;
        public AudioClip[] shatterVariations, landingVariations, rewardVariations, ownerRagdollVariations, footstepVariations;
    }
}
