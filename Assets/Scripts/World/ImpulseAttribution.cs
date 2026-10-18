using UnityEngine;
namespace ScaredyCat
{
    public sealed class ImpulseAttribution : MonoBehaviour
    {
        public GameObject Player { get; private set; }
        public double ExpiresAt { get; private set; }
        public bool Active => Player && Time.timeAsDouble <= ExpiresAt;
        public void Mark(GameObject player, double expiresAt) { Player = player; ExpiresAt = expiresAt; }
        public void Clear() { Player = null; ExpiresAt = 0; }
        public void CopyFrom(ImpulseAttribution other)
        {
            if (other && other.Active && (!Active || other.ExpiresAt > ExpiresAt)) Mark(other.Player, other.ExpiresAt);
        }
        void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody) CopyFrom(collision.rigidbody.GetComponentInParent<ImpulseAttribution>());
        }
    }
}
