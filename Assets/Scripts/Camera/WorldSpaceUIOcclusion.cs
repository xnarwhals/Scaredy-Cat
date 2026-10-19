using UnityEngine;

/// <summary>
/// Hides a composited world-space canvas when a world collider blocks the
/// view camera. The canvas owner's own colliders are ignored.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[DefaultExecutionOrder(200)]
public sealed class WorldSpaceUIOcclusion : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform occlusionRoot;
    [SerializeField] private LayerMask occluderLayers = Physics.DefaultRaycastLayers;

    private Canvas targetCanvas;
    private bool canvasWasEnabled;

    private void Awake()
    {
        targetCanvas = GetComponent<Canvas>();
        canvasWasEnabled = targetCanvas.enabled;
    }

    private void LateUpdate()
    {
        if (targetCanvas == null || !canvasWasEnabled || viewCamera == null)
            return;

        RectTransform rect = targetCanvas.transform as RectTransform;
        if (rect == null)
            return;

        Vector3 target = rect.TransformPoint(rect.rect.center);
        Vector3 offset = target - viewCamera.transform.position;
        float distance = offset.magnitude;
        bool isOccluded = false;

        if (distance > 0.001f)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                viewCamera.transform.position,
                offset / distance,
                distance,
                occluderLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider.transform;
                if (occlusionRoot != null &&
                    (hitTransform == occlusionRoot || hitTransform.IsChildOf(occlusionRoot)))
                    continue;

                isOccluded = true;
                break;
            }
        }

        targetCanvas.enabled = !isOccluded;
    }
}
