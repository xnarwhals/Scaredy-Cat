using UnityEngine;
namespace ScaredyCat
{
    public sealed class CatPresentation : MonoBehaviour
    {
        public Transform visual;
        public CatMotor motor;
        Vector3 scale;
        float squash;
        void Awake() { if (visual) scale = visual.localScale; }
        void OnEnable() { if (motor) motor.Landed += OnLanding; }
        void OnDisable() { if (motor) motor.Landed -= OnLanding; }
        void OnLanding(float speed) => squash = Mathf.Min(.12f, speed * .01f);
        void LateUpdate()
        {
            if (!visual) return;
            squash = Mathf.MoveTowards(squash, 0, Time.deltaTime * .7f);
            float breathe = Mathf.Sin(Time.time * 2.3f) * .008f;
            visual.localScale = Vector3.Scale(scale, new Vector3(1 + squash * .5f, 1 - squash + breathe, 1 + squash * .5f));
        }
    }
}
