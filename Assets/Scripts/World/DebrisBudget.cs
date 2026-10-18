using System.Collections.Generic;
using UnityEngine;
namespace ScaredyCat
{
    public sealed class DebrisBudget : MonoBehaviour
    {
        public int maximumActive = 128;
        public float lifetime = 9;
        readonly List<(Rigidbody body, float expires)> active = new List<(Rigidbody, float)>();
        public int ActiveCount => active.Count;
        public void Register(Rigidbody body)
        {
            active.RemoveAll(entry => entry.body == body || !entry.body || !entry.body.gameObject.activeInHierarchy);
            while (active.Count >= Mathf.Max(1, maximumActive)) Retire(0);
            active.Add((body, Time.time + lifetime));
        }
        void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                if (!active[i].body || !active[i].body.gameObject.activeInHierarchy || active[i].expires < Time.time) Retire(i);
        }
        void Retire(int index)
        {
            if (active[index].body) active[index].body.gameObject.SetActive(false);
            active.RemoveAt(index);
        }
        public void Clear() { while (active.Count > 0) Retire(active.Count - 1); }
    }
}
