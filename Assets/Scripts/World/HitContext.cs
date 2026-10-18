using UnityEngine;
namespace ScaredyCat
{
    public readonly struct HitContext
    {
        public readonly int AttackId;
        public readonly GameObject Instigator;
        public readonly Vector3 Point, Direction;
        public readonly float Charge, Impulse;
        public HitContext(int id, GameObject instigator, Vector3 point, Vector3 direction, float charge, float impulse)
        { AttackId = id; Instigator = instigator; Point = point; Direction = direction; Charge = charge; Impulse = impulse; }
    }
    public interface ISwatReceiver { void ReceiveHit(HitContext hit); }
    public static class SwatQuery
    {
        // Sorting by distance ensures an occluding wall wins over a receiver behind it.
        public static bool TryHit(Vector3 origin, Vector3 direction, AttackSettings settings,
            out RaycastHit hit, out ISwatReceiver receiver)
        {
            var hits = Physics.SphereCastAll(origin, settings.radius, direction, settings.reach,
                settings.hitMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            hit = default; receiver = null;
            if (hits.Length == 0) return false;
            hit = hits[0]; receiver = hit.collider.GetComponentInParent<ISwatReceiver>();
            return true;
        }
    }
}
