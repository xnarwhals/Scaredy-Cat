using UnityEngine;

namespace ScaredyCat
{
    /// <summary>Plays a scene-authored dialogue sequence once both assigned props are paw-smacked.</summary>
    public sealed class TwoPropCutsceneTrigger : MonoBehaviour
    {
        public MischiefProp firstProp;
        public MischiefProp secondProp;
        public CutsceneManager manager;
        public CutsceneSequence sequence;

        bool firstHit;
        bool secondHit;
        bool triggered;

        void OnEnable()
        {
            if (firstProp) firstProp.PawSmacked += OnPropSmacked;
            if (secondProp && secondProp != firstProp) secondProp.PawSmacked += OnPropSmacked;
        }

        void OnDisable()
        {
            if (firstProp) firstProp.PawSmacked -= OnPropSmacked;
            if (secondProp && secondProp != firstProp) secondProp.PawSmacked -= OnPropSmacked;
        }

        void OnPropSmacked(MischiefProp prop)
        {
            if (triggered) return;
            if (prop == firstProp) firstHit = true;
            if (prop == secondProp) secondHit = true;
            if (!firstHit || !secondHit || !manager || !sequence) return;

            triggered = true;
            manager.Play(sequence);
        }
    }
}
