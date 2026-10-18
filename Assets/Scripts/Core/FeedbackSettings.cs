using UnityEngine;
namespace ScaredyCat
{
    [CreateAssetMenu(menuName = "Scaredy Cat/Feedback Settings")]
    public sealed class FeedbackSettings : ScriptableObject
    {
        [Range(0, 1)] public float cameraMotion = .6f;
        [Range(0, 1)] public float volume = .65f;
        public float impactPause = .025f;
        public AudioClip swish, impact, shatter, landing, reward, acceptance;
    }
}
