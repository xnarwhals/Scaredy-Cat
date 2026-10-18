using Unity.AI.Navigation;
using UnityEngine;

namespace ScaredyCat
{
    public sealed class ApartmentNavMeshLinks : MonoBehaviour
    {
        public Transform[] doorways;
        [Min(.1f)] public float halfDepth = .7f;
        [Min(.1f)] public float width = 1.1f;
        public Vector3 doorCenterOffset = new Vector3(.5f, .04f, 0);

        void Start()
        {
            if (doorways == null) return;
            foreach (var doorway in doorways)
            {
                if (!doorway) continue;
                var linkObject = new GameObject("Owner NavMesh doorway link - " + doorway.name);
                linkObject.transform.SetParent(transform, false);
                linkObject.transform.SetPositionAndRotation(doorway.TransformPoint(doorCenterOffset), doorway.rotation);

                var link = linkObject.AddComponent<NavMeshLink>();
                link.agentTypeID = 0;
                link.startPoint = Vector3.back * halfDepth;
                link.endPoint = Vector3.forward * halfDepth;
                link.width = width;
                link.costModifier = -1;
                link.bidirectional = true;
                link.autoUpdate = true;
                link.area = 0;
                link.activated = true;
            }
        }
    }
}
