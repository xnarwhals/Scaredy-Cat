using UnityEngine;

namespace ScaredyCat
{
    [DefaultExecutionOrder(200)]
    public sealed class WorldIndicator : MonoBehaviour
    {
        public Camera view;
        void LateUpdate()
        {
            if (view) transform.rotation = view.transform.rotation;
        }
    }
}
