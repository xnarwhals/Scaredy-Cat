using UnityEngine;
namespace ScaredyCat
{
    [DefaultExecutionOrder(100)]
    public sealed class CameraDirector : MonoBehaviour
    {
        public Camera worldCamera;
        public CatMotor cat;
        public GameObject catVisual;
        public FeedbackDirector feedback;
        public bool Cinematic { get; private set; }
        Vector3 shotPosition;
        Quaternion shotRotation;
        float shotFov, returnRemaining, returnDuration;
        Vector3 returningFrom;
        Quaternion returningRotation;
        float returningFov;
        public void SetShot(Vector3 position, Quaternion rotation, float fov)
        { Cinematic = true; shotPosition = position; shotRotation = rotation; shotFov = fov; returnRemaining = 0; }
        public void ReturnToPlayer(float duration = .35f)
        {
            returningFrom = worldCamera.transform.position; returningRotation = worldCamera.transform.rotation; returningFov = worldCamera.fieldOfView;
            Cinematic = false; returnDuration = duration; returnRemaining = duration;
        }
        void LateUpdate()
        {
            if (!worldCamera || !cat || !cat.head) return;
            Vector3 position = Cinematic ? shotPosition : cat.head.position;
            Quaternion rotation = Cinematic ? shotRotation : cat.ViewRotation;
            float fov = Cinematic ? shotFov : cat.settings.fieldOfView;
            if (!Cinematic && returnRemaining > 0)
            {
                returnRemaining = Mathf.Max(0, returnRemaining - Time.deltaTime);
                float t = Mathf.SmoothStep(0, 1, 1 - returnRemaining / Mathf.Max(.001f, returnDuration));
                position = Vector3.Lerp(returningFrom, position, t); rotation = Quaternion.Slerp(returningRotation, rotation, t);
                fov = Mathf.Lerp(returningFov, fov, t);
            }
            if (catVisual) catVisual.SetActive(Cinematic);
            if (!Cinematic && feedback) { position += rotation * feedback.CameraOffset; rotation *= feedback.CameraRotation; }
            worldCamera.transform.SetPositionAndRotation(position, rotation); worldCamera.fieldOfView = fov;
        }
    }
}
