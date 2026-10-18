using UnityEngine;

namespace ScaredyCat
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class OwnerRewardArea : MonoBehaviour
    {
        public string roomName;
        BoxCollider volume;
        void Awake() { volume = GetComponent<BoxCollider>(); }
        public bool Contains(Vector3 worldPoint)
        {
            if (!volume) volume = GetComponent<BoxCollider>();
            Vector3 point = volume.transform.InverseTransformPoint(worldPoint) - volume.center;
            Vector3 half = volume.size * .5f;
            return Mathf.Abs(point.x) < half.x && Mathf.Abs(point.y) < half.y && Mathf.Abs(point.z) < half.z;
        }
    }
}
