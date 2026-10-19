using System.Collections.Generic;
using System;
using UnityEngine;
namespace ScaredyCat
{
    [RequireComponent(typeof(Rigidbody), typeof(ImpulseAttribution))]
    public sealed class MischiefProp : MonoBehaviour, ISwatReceiver
    {
        const uint DefaultRenderingLayer = 1u;
        const uint SmashableRenderingLayer = 1u << 8;
        public AgroMeter agro;
        public DebrisBudget debris;
        public FeedbackDirector feedback;
        public GameObject intactVisual, brokenRoot;
        public bool breakable = true;
        [Tooltip("Zero keeps the Rigidbody mass already authored on the prefab.")]
        [Min(0)] public float objectMass;
        [Min(0)] public float launchMultiplier = 1;
        [Tooltip("Keep a wall-mounted prop in place until a paw hit releases it.")]
        public bool startKinematicUntilHit;
        public float breakCharge = .72f, collisionBreakSpeed = 2.6f;
        [Min(0)] public float fragmentBurstImpulse = .12f;
        public bool Broken { get; private set; }
        public event Action<MischiefProp> PawSmacked;
        Rigidbody body;
        Collider[] intactColliders;
        Renderer[] intactRenderers;
        bool[] initiallyVisible;
        Rigidbody[] fragments;
        ImpulseAttribution attribution;
        Vector3 initialPosition;
        Quaternion initialRotation;
        Vector3[] fragmentPositions;
        Quaternion[] fragmentRotations;
        bool knocked;
        bool initialKinematic;
        bool smashableOutlineConsumed;
        void Awake()
        {
            body = GetComponent<Rigidbody>(); attribution = GetComponent<ImpulseAttribution>();
            initialKinematic = body.isKinematic || startKinematicUntilHit;
            if (startKinematicUntilHit) body.isKinematic = true;
            if (objectMass > 0) body.mass = Mathf.Max(.01f, objectMass);
            var visible = new List<Renderer>();
            var visualRoot = intactVisual ? intactVisual : gameObject;
            foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
                if (!brokenRoot || !renderer.transform.IsChildOf(brokenRoot.transform)) visible.Add(renderer);
            intactRenderers = visible.ToArray();
            initiallyVisible = new bool[intactRenderers.Length];
            for (int i = 0; i < intactRenderers.Length; i++)
            {
                initiallyVisible[i] = intactRenderers[i].enabled;
                intactRenderers[i].renderingLayerMask |= DefaultRenderingLayer | SmashableRenderingLayer;
            }
            var solid = new List<Collider>();
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                if (!brokenRoot || !collider.transform.IsChildOf(brokenRoot.transform)) solid.Add(collider);
            intactColliders = solid.ToArray();
            initialPosition = transform.position; initialRotation = transform.rotation;
            fragments = brokenRoot ? brokenRoot.GetComponentsInChildren<Rigidbody>(true) : new Rigidbody[0];
            fragmentPositions = new Vector3[fragments.Length]; fragmentRotations = new Quaternion[fragments.Length];
            for (int i = 0; i < fragments.Length; i++)
            { fragmentPositions[i] = fragments[i].transform.localPosition; fragmentRotations[i] = fragments[i].transform.localRotation; }
            if (brokenRoot) brokenRoot.SetActive(false);
        }
        void Start()
        {
            body.collisionDetectionMode = body.isKinematic ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.ContinuousDynamic;
        }
        public void ReceiveHit(HitContext hit)
        {
            if (Broken) return;
            PawSmacked?.Invoke(this);
            attribution.Mark(hit.Instigator, Time.timeAsDouble + (agro && agro.settings ? agro.settings.attributionSeconds : 6));
            float appliedImpulse = hit.Impulse * launchMultiplier;
            if (!breakable && !smashableOutlineConsumed)
            {
                smashableOutlineConsumed = true;
                foreach (var renderer in intactRenderers)
                    if (renderer) renderer.renderingLayerMask &= ~SmashableRenderingLayer;
            }
            if (startKinematicUntilHit && body.isKinematic)
            {
                body.isKinematic = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.AddForce(Vector3.up * appliedImpulse * .2f, ForceMode.Impulse);
                body.AddTorque(Vector3.Cross(hit.Direction, Vector3.up).normalized * appliedImpulse * .2f, ForceMode.Impulse);
            }
            if (!body.isKinematic) body.AddForceAtPosition(hit.Direction * appliedImpulse, hit.Point, ForceMode.Impulse);
            if (!knocked && hit.Instigator && agro)
            { knocked = true; agro.Report(new MischiefEvent(GetInstanceID(), MischiefKind.Knock, true)); }
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
            // Walking into a prop, including shoving it into a wall, is not a paw strike.
            if (collision.collider.GetComponentInParent<CatMotor>()) return;
            if (collision.rigidbody) attribution.CopyFrom(collision.rigidbody.GetComponentInParent<ImpulseAttribution>());
            if (breakable && attribution.Active && collision.relativeVelocity.magnitude >= collisionBreakSpeed && Time.timeSinceLevelLoad > .5f)
                Break(collision.contactCount > 0 ? collision.GetContact(0).point : transform.position, collision.impulse * .2f);
        }
        public void Break(Vector3 point, Vector3 impulse)
        {
            if (Broken || !breakable || !brokenRoot || fragments.Length == 0) return;
            Broken = true;
            var velocity = body.linearVelocity; var angularVelocity = body.angularVelocity;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            foreach (var collider in intactColliders) collider.enabled = false;
            SetIntactVisible(false);
            brokenRoot.SetActive(true);
            for (int i = 0; i < fragments.Length; i++)
            {
                var fragment = fragments[i]; fragment.gameObject.SetActive(true);
                fragment.isKinematic = false; fragment.constraints = RigidbodyConstraints.None;
                fragment.linearVelocity = velocity + Vector3.Cross(angularVelocity, fragment.worldCenterOfMass - body.worldCenterOfMass);
                fragment.angularVelocity = angularVelocity;
                fragment.GetComponent<ImpulseAttribution>()?.CopyFrom(attribution);
                Vector3 outward = fragment.worldCenterOfMass - transform.position;
                outward.y = 0;
                if (outward.sqrMagnitude < .01f)
                {
                    float angle = 2 * Mathf.PI * i / fragments.Length;
                    outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                }
                outward = (outward.normalized + Vector3.up * .4f).normalized;
                fragment.AddForce(impulse / fragments.Length + outward * fragmentBurstImpulse, ForceMode.Impulse);
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
            body.position = initialPosition; body.rotation = initialRotation;
            if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = initialKinematic;
            body.collisionDetectionMode = body.isKinematic ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.ContinuousDynamic;
            foreach (var collider in intactColliders) collider.enabled = true;
            SetIntactVisible(true);
            attribution.Clear(); Broken = false;
            // Reward ledger intentionally survives a station reset; restart the scene for a fresh run.
        }
        void SetIntactVisible(bool show)
        {
            if (!intactVisual) return;
            if (!brokenRoot || !brokenRoot.transform.IsChildOf(intactVisual.transform))
                intactVisual.SetActive(show);
            else
                for (int i = 0; i < intactRenderers.Length; i++)
                    intactRenderers[i].enabled = show && initiallyVisible[i];
        }
    }
}
