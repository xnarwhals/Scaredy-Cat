using UnityEngine;
namespace ScaredyCat
{
    [RequireComponent(typeof(Rigidbody), typeof(ImpulseAttribution))]
    public sealed class MischiefProp : MonoBehaviour, ISwatReceiver
    {
        public AgroMeter agro;
        public DebrisBudget debris;
        public FeedbackDirector feedback;
        public GameObject intactVisual, brokenRoot;
        public bool breakable = true;
        public float breakCharge = .72f, collisionBreakSpeed = 2.6f;
        public bool Broken { get; private set; }
        Rigidbody body;
        Collider[] intactColliders;
        Rigidbody[] fragments;
        ImpulseAttribution attribution;
        Vector3 initialPosition;
        Quaternion initialRotation;
        Vector3[] fragmentPositions;
        Quaternion[] fragmentRotations;
        bool knocked;
        void Awake()
        {
            body = GetComponent<Rigidbody>(); attribution = GetComponent<ImpulseAttribution>();
            intactColliders = GetComponents<Collider>();
            initialPosition = transform.position; initialRotation = transform.rotation;
            fragments = brokenRoot ? brokenRoot.GetComponentsInChildren<Rigidbody>(true) : new Rigidbody[0];
            fragmentPositions = new Vector3[fragments.Length]; fragmentRotations = new Quaternion[fragments.Length];
            for (int i = 0; i < fragments.Length; i++)
            { fragmentPositions[i] = fragments[i].transform.localPosition; fragmentRotations[i] = fragments[i].transform.localRotation; }
            if (brokenRoot) brokenRoot.SetActive(false);
        }
        public void ReceiveHit(HitContext hit)
        {
            if (Broken) return;
            attribution.Mark(hit.Instigator, Time.timeAsDouble + (agro && agro.settings ? agro.settings.attributionSeconds : 6));
            body.AddForceAtPosition(hit.Direction * hit.Impulse, hit.Point, ForceMode.Impulse);
            if (breakable && hit.Charge >= breakCharge) Break(hit.Point, hit.Direction * hit.Impulse);
        }
        void FixedUpdate()
        {
            if (Broken) return;
            if (transform.position.y < initialPosition.y - 12) { ResetProp(); return; }
            if (knocked || !attribution.Active || !agro) return;
            float distance = agro.settings ? agro.settings.knockDistance : .25f;
            float angle = agro.settings ? agro.settings.knockAngle : 35;
            if (Vector3.Distance(initialPosition, transform.position) >= distance || Quaternion.Angle(initialRotation, transform.rotation) >= angle)
            { knocked = true; agro.Report(new MischiefEvent(GetInstanceID(), MischiefKind.Knock, true)); }
        }
        void OnCollisionEnter(Collision collision)
        {
            if (Broken) return;
            if (collision.rigidbody) attribution.CopyFrom(collision.rigidbody.GetComponentInParent<ImpulseAttribution>());
            if (breakable && collision.relativeVelocity.magnitude >= collisionBreakSpeed && Time.timeSinceLevelLoad > .5f)
                Break(collision.contactCount > 0 ? collision.GetContact(0).point : transform.position, collision.impulse * .2f);
        }
        public void Break(Vector3 point, Vector3 impulse)
        {
            if (Broken || !breakable || !brokenRoot || fragments.Length == 0) return;
            Broken = true;
            var velocity = body.linearVelocity; var angularVelocity = body.angularVelocity;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true;
            foreach (var collider in intactColliders) collider.enabled = false;
            if (intactVisual) intactVisual.SetActive(false);
            brokenRoot.SetActive(true);
            for (int i = 0; i < fragments.Length; i++)
            {
                var fragment = fragments[i]; fragment.gameObject.SetActive(true);
                fragment.isKinematic = false; fragment.constraints = RigidbodyConstraints.None;
                fragment.linearVelocity = velocity + Vector3.Cross(angularVelocity, fragment.worldCenterOfMass - body.worldCenterOfMass);
                fragment.angularVelocity = angularVelocity;
                fragment.GetComponent<ImpulseAttribution>()?.CopyFrom(attribution);
                Vector3 outward = (fragment.worldCenterOfMass - point).normalized;
                fragment.AddForce(impulse / fragments.Length + outward * .12f, ForceMode.Impulse);
                if (debris) debris.Register(fragment);
            }
            if (agro) agro.Report(new MischiefEvent(GetInstanceID(), MischiefKind.Break, attribution.Active));
            if (feedback) feedback.Shatter(point);
        }
        public void ResetProp()
        {
            if (!body) return;
            if (brokenRoot) brokenRoot.SetActive(false);
            for (int i = 0; i < fragments.Length; i++)
            {
                var fragment = fragments[i]; fragment.isKinematic = true;
                fragment.transform.localPosition = fragmentPositions[i]; fragment.transform.localRotation = fragmentRotations[i];
                fragment.GetComponent<ImpulseAttribution>()?.Clear();
            }
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            body.position = initialPosition; body.rotation = initialRotation; body.isKinematic = false;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            foreach (var collider in intactColliders) collider.enabled = true;
            if (intactVisual) intactVisual.SetActive(true);
            attribution.Clear(); Broken = false;
            // Reward ledger intentionally survives a station reset; restart the scene for a fresh run.
        }
    }
}
