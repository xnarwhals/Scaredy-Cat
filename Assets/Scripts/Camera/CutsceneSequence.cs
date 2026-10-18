using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;
namespace ScaredyCat
{
    [Serializable]
    public sealed class CameraShot
    {
        public string label;
        public Transform pose, lookAt;
        public SplineContainer spline;
        [Range(0, 1)] public float splineStart;
        [Range(0, 1)] public float splineEnd = 1;
        public Vector3 lookAtOffset;
        public float duration = 2, blendIn = .5f, fieldOfView = 65;
        [TextArea] public string caption;
        public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public UnityEvent onEnter = new UnityEvent();
        public bool Valid => pose || spline;
        public void Evaluate(float t, out Vector3 position, out Quaternion rotation)
        {
            float eased = ease.Evaluate(Mathf.Clamp01(t));
            float u = Mathf.Lerp(splineStart, splineEnd, eased);
            position = spline ? (Vector3)spline.EvaluatePosition(u) : pose.position;
            rotation = pose ? pose.rotation : Quaternion.identity;
            Vector3 target = lookAt ? lookAt.position + lookAtOffset : position;
            if (lookAt && (target - position).sqrMagnitude > .001f) rotation = Quaternion.LookRotation(target - position);
        }
    }
    public sealed class CutsceneSequence : MonoBehaviour
    {
        public CameraShot[] shots;
        public bool playOnce = true, waitForGrounded = true, skippable = true;
        public UnityEvent onStarted = new UnityEvent(), onFinished = new UnityEvent();
        public bool HasPlayed { get; internal set; }
    }
}
