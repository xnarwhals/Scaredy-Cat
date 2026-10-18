using NUnit.Framework;
namespace ScaredyCat.Tests
{
    public sealed class GameplayRulesTests
    {
        [TestCase(0, 0)] [TestCase(.4, .5f)] [TestCase(.8, 1)] [TestCase(2, 1)]
        public void ChargeUsesElapsedTimeAndCapsAtFull(double elapsed, float expected)
        {
            var charge = new ChargeClock(); charge.Press(10);
            Assert.That(charge.Release(10 + elapsed, .8f, out var value), Is.True);
            Assert.That(value, Is.EqualTo(expected).Within(.0001));
            Assert.That(charge.Release(12, .8f, out _), Is.False);
        }
        [Test] public void CancelledChargeCannotFireAfterControlReturns()
        {
            var charge = new ChargeClock(); charge.Press(0); charge.Cancel();
            Assert.That(charge.Release(2, .8f, out _), Is.False);
            charge.Press(3); charge.Release(3.1, .8f, out var value); Assert.That(value, Is.EqualTo(.125f).Within(.001));
        }
        [Test] public void LocksAreOwnedAndDisposalIsIdempotent()
        {
            var gate = new ControlGate(); int changes = 0; gate.Changed += _ => changes++;
            var cinematic = gate.Acquire(); var pause = gate.Acquire();
            cinematic.Dispose(); cinematic.Dispose(); Assert.That(gate.IsLocked, Is.True);
            pause.Dispose(); Assert.That(gate.IsLocked, Is.False); Assert.That(changes, Is.EqualTo(2));
        }
        [Test] public void AmbientAndInvalidRewardsDoNotConsumeEligibility()
        {
            var ledger = new AgroLedger(100);
            Assert.That(ledger.Apply(new MischiefEvent(1, MischiefKind.Break, false), 10), Is.False);
            Assert.That(ledger.Apply(new MischiefEvent(1, MischiefKind.Break, true), 0), Is.False);
            Assert.That(ledger.Apply(new MischiefEvent(1, MischiefKind.Break, true), 10), Is.True);
            Assert.That(ledger.Value, Is.EqualTo(90));
        }
        [Test] public void CompoundHitsAndDebrisCannotRepeatPropCredit()
        {
            var ledger = new AgroLedger(100);
            for (int i = 0; i < 20; i++) ledger.Apply(new MischiefEvent(4, MischiefKind.Break, true, i), 10);
            ledger.Apply(new MischiefEvent(4, MischiefKind.Knock, true), 3);
            Assert.That(ledger.Value, Is.EqualTo(87));
        }
        [Test] public void OwnerCreditRequiresANewRecoveryCycle()
        {
            var ledger = new AgroLedger(100);
            ledger.Apply(new MischiefEvent(2, MischiefKind.OwnerKnockdown, true, 1), 12);
            ledger.Apply(new MischiefEvent(2, MischiefKind.OwnerKnockdown, true, 1), 12);
            ledger.Apply(new MischiefEvent(2, MischiefKind.OwnerKnockdown, true, 2), 12);
            Assert.That(ledger.Value, Is.EqualTo(76));
        }
        [Test] public void ReentrantCompletionClampsAndFiresOnlyOnce()
        {
            var ledger = new AgroLedger(5); int completions = 0;
            ledger.Calmed += () => { completions++; ledger.Apply(new MischiefEvent(2, MischiefKind.Break, true), 10); };
            ledger.Apply(new MischiefEvent(1, MischiefKind.Break, true), 10);
            Assert.That(ledger.Value, Is.Zero); Assert.That(completions, Is.EqualTo(1));
        }
    }
}
