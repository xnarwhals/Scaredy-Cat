using UnityEngine;

namespace ScaredyCat
{
    public sealed class RubTarget : MonoBehaviour
    {
        public AgroMeter agro;
        public FeedbackDirector feedback;
        public Transform interactionTarget;
        public Renderer[] markedRenderers;
        public GameObject worldIndicator;
        public bool Marked { get; private set; }
        public Vector3 TargetPosition => interactionTarget ? interactionTarget.position : transform.position;
        public bool Mark(GameObject player)
        {
            if (Marked || !player || !agro) return false;
            if (!agro.Report(new MischiefEvent(GetInstanceID(), MischiefKind.Mark, true))) return false;
            Marked = true;
            foreach (var renderer in markedRenderers)
                if (renderer) renderer.renderingLayerMask |= 1u << 9;
            if (worldIndicator) worldIndicator.SetActive(false);
            if (feedback) feedback.MarkTerritory(TargetPosition);
            return true;
        }
    }
}
