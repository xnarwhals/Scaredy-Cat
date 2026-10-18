using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace ScaredyCat
{
    public enum PawState { Idle, Charging, Swing, Recovery }
    public sealed class PawAttack : MonoBehaviour
    {
        public GameInput input;
        public AttackSettings settings;
        public Transform aim, paw;
        public FeedbackDirector feedback;
        [Header("Charge Haptics")]
        [Range(0, 1)] public float chargeRumbleLow = .08f;
        [Range(0, 1)] public float chargeRumbleHigh = .04f;
        [Range(0, 1)] public float fullChargeRumbleLow = .5f;
        [Range(0, 1)] public float fullChargeRumbleHigh = .35f;
        public PawState State { get; private set; }
        public float Charge => clock.Fraction(Time.timeAsDouble, settings ? settings.chargeDuration : .8f);
        public event Action<HitContext, bool> Struck;
        readonly ChargeClock clock = new ChargeClock();
        Vector3 restPosition, restScale;
        Quaternion restRotation;
        float elapsed, strikeCharge, presentationPause;
        int attackId;
        bool queuedPress, queuedRelease;
        Gamepad chargeGamepad;
        void Awake()
        {
            if (paw) { restPosition = paw.localPosition; restRotation = paw.localRotation; restScale = paw.localScale; paw.gameObject.SetActive(false); }
        }
        void OnEnable()
        {
            if (!input) return;
            input.AttackPressed += Press; input.AttackReleased += Release; input.GameplayCancelled += Cancel;
        }
        void OnDisable()
        {
            if (input) { input.AttackPressed -= Press; input.AttackReleased -= Release; input.GameplayCancelled -= Cancel; }
            Cancel();
        }
        public void Press()
        {
            if (!settings || (input && input.Controls.IsLocked)) return;
            if (State == PawState.Recovery) { queuedPress = true; queuedRelease = false; return; }
            if (State != PawState.Idle) return;
            chargeGamepad = input && input.LastInputWasGamepad ? Gamepad.current : null;
            clock.Press(Time.timeAsDouble); State = PawState.Charging;
            if (paw) paw.gameObject.SetActive(true);
        }
        public void Release()
        {
            StopChargeRumble();
            if (input && input.Controls.IsLocked) { Cancel(); return; }
            if (queuedPress) { queuedRelease = true; return; }
            if (State != PawState.Charging || !clock.Release(Time.timeAsDouble, settings.chargeDuration, out strikeCharge)) return;
            State = PawState.Swing; elapsed = 0;
            bool connected = SwatQuery.TryHit(aim.position, aim.forward, settings, out var contact, out var receiver);
            var hit = new HitContext(++attackId, gameObject, connected ? contact.point : aim.position + aim.forward * settings.reach,
                (aim.forward + Vector3.up * .16f).normalized, strikeCharge,
                Mathf.Lerp(settings.tapImpulse, settings.chargedImpulse, settings.chargeForce.Evaluate(strikeCharge)));
            bool smackableHit = connected && receiver is MischiefProp;
            if (smackableHit && feedback)
            {
                feedback.SuccessfulHit(hit, () =>
                {
                    receiver.ReceiveHit(hit);
                    presentationPause = feedback.settings ? feedback.settings.impactPause : 0;
                });
            }
            else
            {
                receiver?.ReceiveHit(hit);
                if (feedback) feedback.Attack(hit, connected);
            }
            Struck?.Invoke(hit, connected);
        }
        public void Cancel()
        {
            StopChargeRumble();
            clock.Cancel(); queuedPress = queuedRelease = false; State = PawState.Idle; elapsed = presentationPause = 0;
            if (paw) { paw.gameObject.SetActive(false); paw.localPosition = restPosition; paw.localRotation = restRotation; paw.localScale = restScale; }
        }
        public void DebugStrike(float charge)
        {
            Press();
            if (State != PawState.Charging) return;
            clock.Press(Time.timeAsDouble - Mathf.Clamp01(charge) * settings.chargeDuration);
            Release();
        }
        void Update()
        {
            if (!settings || State == PawState.Idle) return;
            if (State == PawState.Charging)
            {
                float charge = Charge;
                UpdateChargeRumble(charge);
                if (!paw) return;
                paw.localPosition = restPosition + new Vector3(.035f, -.04f, -.1f) * charge;
                paw.localRotation = restRotation * Quaternion.Euler(-25 * charge, 0, -18 * charge);
                paw.localScale = Vector3.Scale(restScale, new Vector3(1 + charge * .08f, 1 - charge * .07f, 1));
                return;
            }
            if (!paw) return;
            if (presentationPause > 0) { presentationPause -= Time.deltaTime; return; }
            elapsed += Time.deltaTime;
            Vector3 extended = restPosition + new Vector3(-.24f, .09f, .23f);
            Quaternion swipe = restRotation * Quaternion.Euler(40, -35, 50);
            if (State == PawState.Swing)
            {
                float t = Mathf.Clamp01(elapsed / settings.swingDuration);
                paw.localPosition = Vector3.Lerp(restPosition, extended, t);
                paw.localRotation = Quaternion.Slerp(restRotation, swipe, t);
                if (t >= 1) { State = PawState.Recovery; elapsed = 0; }
            }
            else
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / settings.recoveryDuration);
                paw.localPosition = Vector3.Lerp(extended, restPosition + Vector3.down * .18f, t);
                paw.localRotation = Quaternion.Slerp(swipe, restRotation, t);
                paw.localScale = restScale;
                if (elapsed >= settings.recoveryDuration)
                {
                    bool press = queuedPress, release = queuedRelease; Cancel();
                    if (press) { Press(); if (release) Release(); }
                }
            }
        }
        void UpdateChargeRumble(float charge)
        {
            if (chargeGamepad == null || !chargeGamepad.added) return;
            float low = Mathf.Lerp(chargeRumbleLow, fullChargeRumbleLow, charge);
            float high = Mathf.Lerp(chargeRumbleHigh, fullChargeRumbleHigh, charge);
            chargeGamepad.SetMotorSpeeds(low, high);
        }
        void StopChargeRumble()
        {
            if (chargeGamepad != null && chargeGamepad.added) chargeGamepad.SetMotorSpeeds(0, 0);
            chargeGamepad = null;
        }
    }
}
