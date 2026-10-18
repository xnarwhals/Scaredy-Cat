using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ScaredyCat
{
    public sealed class RubInteractor : MonoBehaviour
    {
        public GameInput input;
        public Transform aim;
        public RubTarget[] targets;
        public float reach = 2.1f;
        public float aimAngle = 36;
        public LayerMask occluders = ~0;
        public TMP_Text nearbyPrompt;
        public GameObject holdBar;
        public Image holdFill;
        [Min(.1f)] public float holdDuration = 1.4f;
        RubTarget focused;
        bool holding;
        float heldFor;
        void OnEnable()
        {
            if (input)
            {
                input.InteractStarted += BeginHold;
                input.InteractReleased += CancelHold;
                input.GameplayCancelled += CancelHold;
            }
            if (targets != null)
                foreach (var target in targets)
                    if (target && target.worldIndicator) target.worldIndicator.SetActive(false);
            if (nearbyPrompt) nearbyPrompt.gameObject.SetActive(false);
            if (holdBar) holdBar.SetActive(false);
        }
        void OnDisable()
        {
            if (input)
            {
                input.InteractStarted -= BeginHold;
                input.InteractReleased -= CancelHold;
                input.GameplayCancelled -= CancelHold;
            }
            SetFocus(null);
        }
        void Update()
        {
            SetFocus(FindTarget());
            if (holding && focused)
            {
                heldFor += Time.deltaTime;
                if (holdFill) holdFill.fillAmount = Mathf.Clamp01(heldFor / holdDuration);
                if (heldFor >= holdDuration)
                {
                    bool marked = focused.Mark(gameObject);
                    CancelHold();
                    if (marked) SetFocus(null);
                }
            }
        }
        void BeginHold()
        {
            if (!focused || (input && input.Controls.IsLocked)) return;
            holding = true;
            heldFor = 0;
            if (holdFill) holdFill.fillAmount = 0;
            if (holdBar) holdBar.SetActive(true);
        }
        void CancelHold()
        {
            holding = false;
            heldFor = 0;
            if (holdFill) holdFill.fillAmount = 0;
            if (holdBar) holdBar.SetActive(false);
        }
        RubTarget FindTarget()
        {
            if (!aim || targets == null || (input && input.Controls.IsLocked)) return null;
            RubTarget best = null;
            float bestScore = float.MaxValue;
            foreach (var target in targets)
            {
                if (!target || target.Marked) continue;
                Vector3 direction = target.TargetPosition - aim.position;
                float distance = Vector3.Distance(transform.position, target.TargetPosition);
                // A large sofa or counter can be reached from its near edge.
                var colliders = target.GetComponentsInChildren<Collider>();
                foreach (var collider in colliders)
                    if (collider.enabled && !collider.isTrigger)
                        distance = Mathf.Min(distance, Vector3.Distance(transform.position, collider.ClosestPoint(transform.position)));
                if (distance > reach || direction.sqrMagnitude < .0001f) continue;
                float angle = Vector3.Angle(aim.forward, direction);
                // Near a large object, its authored anchor can sit off-centre or behind
                // the surface being rubbed. Proximity should still reveal the prompt.
                if (angle > (distance <= 1.4f ? 115f : Mathf.Max(aimAngle, 75f))) continue;
                // The selected object's own colliders are allowed to be the first hit.
                float rayDistance = direction.magnitude;
                if (Physics.Raycast(aim.position, direction / rayDistance, out var hit, rayDistance - .03f, occluders, QueryTriggerInteraction.Ignore)
                    && !hit.collider.transform.IsChildOf(target.transform)) continue;
                float score = angle * .08f + distance;
                if (score >= bestScore) continue;
                best = target; bestScore = score;
            }
            return best;
        }
        void SetFocus(RubTarget target)
        {
            if (focused != target)
            {
                if (holding) CancelHold();
                if (focused && focused.worldIndicator) focused.worldIndicator.SetActive(false);
                focused = target;
                if (focused && focused.worldIndicator && !focused.Marked) focused.worldIndicator.SetActive(true);
            }
            bool available = focused && !focused.Marked;
            if (nearbyPrompt)
            {
                nearbyPrompt.gameObject.SetActive(available);
                if (available) nearbyPrompt.text = holding ? "MARKING TERRITORY" : "HOLD E  MARK TERRITORY";
            }
            if (holdBar) holdBar.SetActive(available && holding);
        }
    }
}
