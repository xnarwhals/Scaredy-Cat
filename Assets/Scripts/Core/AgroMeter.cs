using System;
using UnityEngine;
namespace ScaredyCat
{
    [DefaultExecutionOrder(-150)]
    public sealed class AgroMeter : MonoBehaviour
    {
        public RewardSettings settings;
        public AgroLedger Ledger { get; private set; }
        public event Action<MischiefEvent> Rewarded;
        void Awake() => ResetMeter();
        public void ResetMeter() => Ledger = new AgroLedger(settings ? settings.initialAgro : 100);
        public bool Report(MischiefEvent outcome)
        {
            if (Ledger == null) ResetMeter();
            if (!Ledger.Apply(outcome, settings ? settings.Reward(outcome.Kind) : 10)) return false;
            Rewarded?.Invoke(outcome); return true;
        }
    }
}
