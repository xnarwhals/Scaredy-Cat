using UnityEngine;
namespace ScaredyCat
{
    public sealed class NarrativeTrigger : MonoBehaviour
    {
        public CutsceneManager manager;
        public CutsceneSequence sequence;
        public MischiefProp target;
        void OnTriggerEnter(Collider other)
        { if ((!target || !target.Broken) && other.GetComponentInParent<CatMotor>()) manager.Play(sequence); }
    }
}
