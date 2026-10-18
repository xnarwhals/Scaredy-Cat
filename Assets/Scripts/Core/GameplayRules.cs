using System;
using System.Collections.Generic;
namespace ScaredyCat
{
    // Pure rules with explicit clocks and identities; no scene or global singleton needed.
    public sealed class ControlGate
    {
        readonly HashSet<object> owners = new HashSet<object>();
        public bool IsLocked => owners.Count != 0;
        public event Action<bool> Changed;
        public IDisposable Acquire()
        {
            var key = new object(); bool wasLocked = IsLocked; owners.Add(key);
            if (!wasLocked) Changed?.Invoke(true);
            return new Lease(() => { if (owners.Remove(key) && !IsLocked) Changed?.Invoke(false); });
        }
        sealed class Lease : IDisposable
        {
            Action release;
            public Lease(Action release) => this.release = release;
            public void Dispose() { var callback = release; release = null; callback?.Invoke(); }
        }
    }
    public sealed class ChargeClock
    {
        double pressedAt;
        public bool IsCharging { get; private set; }
        public void Press(double now) { pressedAt = now; IsCharging = true; }
        public float Fraction(double now, float duration) => IsCharging
            ? (float)Math.Max(0, Math.Min(1, (now - pressedAt) / Math.Max(0.001, duration))) : 0;
        public bool Release(double now, float duration, out float charge)
        { charge = Fraction(now, duration); bool valid = IsCharging; Cancel(); return valid; }
        public void Cancel() => IsCharging = false;
    }
    public enum MischiefKind { Knock, Break, OwnerKnockdown }
    public readonly struct MischiefEvent
    {
        public readonly int TargetId;
        public readonly MischiefKind Kind;
        public readonly int Cycle;
        public readonly bool PlayerCaused;
        public MischiefEvent(int targetId, MischiefKind kind, bool playerCaused, int cycle = 0)
        { TargetId = targetId; Kind = kind; PlayerCaused = playerCaused; Cycle = cycle; }
    }
    public sealed class AgroLedger
    {
        readonly HashSet<(int, MischiefKind, int)> credited = new HashSet<(int, MischiefKind, int)>();
        public float Value { get; private set; }
        public float Maximum { get; }
        bool calmedRaised;
        public event Action<float, float> Changed;
        public event Action Calmed;
        public AgroLedger(float maximum) { Maximum = Math.Max(1, maximum); Value = Maximum; }
        public bool Apply(MischiefEvent outcome, float reward)
        {
            if (!outcome.PlayerCaused || reward <= 0 || Value <= 0) return false;
            int cycle = outcome.Kind == MischiefKind.OwnerKnockdown ? outcome.Cycle : 0;
            if (!credited.Add((outcome.TargetId, outcome.Kind, cycle))) return false;
            float previous = Value; Value = Math.Max(0, Value - reward);
            bool becameCalm = Value == 0 && !calmedRaised;
            if (becameCalm) calmedRaised = true;
            Changed?.Invoke(Value, previous - Value);
            if (becameCalm) Calmed?.Invoke();
            return true;
        }
    }
}
