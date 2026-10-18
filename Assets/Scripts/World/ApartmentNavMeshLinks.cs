using Unity.AI.Navigation;
using UnityEngine.AI;
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
                Vector3 center = doorway.TransformPoint(doorCenterOffset);
                linkObject.transform.SetPositionAndRotation(center, doorway.rotation);

                var link = linkObject.AddComponent<NavMeshLink>();
                link.agentTypeID = 0;
                Vector3 acrossDoor = doorway.forward * halfDepth;
                if (!NavMesh.SamplePosition(center - acrossDoor, out var start, 1.5f, NavMesh.AllAreas) ||
                    !NavMesh.SamplePosition(center + acrossDoor, out var end, 1.5f, NavMesh.AllAreas))
                {
                    Debug.LogWarning($"Could not find NavMesh endpoints for doorway {doorway.name}.", doorway);
                    Destroy(linkObject);
                    continue;
                }
                link.startPoint = linkObject.transform.InverseTransformPoint(start.position);
                link.endPoint = linkObject.transform.InverseTransformPoint(end.position);
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
