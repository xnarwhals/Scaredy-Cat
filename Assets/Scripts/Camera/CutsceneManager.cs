using System;
using System.Collections.Generic;
using UnityEngine;
namespace ScaredyCat
{
    public enum SequenceResult { Completed, Skipped, Cancelled, Invalid }
    [DefaultExecutionOrder(50)]
    public sealed class CutsceneManager : MonoBehaviour
    {
        public GameInput input;
        public CatMotor cat;
        public CameraDirector director;
        public GameFlow flow;
        public AudioSource dialogueSource;
        [Header("Dialogue Typewriter")]
        public AudioClip ownerTalking;
        [Min(1)] public float charactersPerSecond = 34f;
        [Range(0, 1)] public float talkingVolume = .35f;
        public CutsceneSequence Active { get; private set; }
        public bool IsPlaying => Active;
        public string Caption { get; private set; } = "";
        public int VisibleCharacterCount { get; private set; }
        public bool IsTyping { get; private set; }
        public event Action<CutsceneSequence, SequenceResult> Finished;
        readonly Queue<CutsceneSequence> queue = new Queue<CutsceneSequence>();
        IDisposable controlLock;
        int shotIndex;
        float elapsed;
        Vector3 blendPosition;
        Quaternion blendRotation;
        float blendFov;
        bool finishing;
        bool requiresLookTarget;
        float typewriterProgress;
        bool talkingAudioPaused;
        void Awake()
        {
            if (!flow) flow = FindFirstObjectByType<GameFlow>();
            if (!dialogueSource) dialogueSource = GetComponent<AudioSource>();
        }
        void OnEnable() { if (input) { input.SkipPressed += Skip; input.AdvancePressed += Advance; } }
        void OnDisable() { if (input) { input.SkipPressed -= Skip; input.AdvancePressed -= Advance; } Cancel(); }
        public void Play(CutsceneSequence sequence)
        {
            if (!sequence || (sequence.playOnce && sequence.HasPlayed) || sequence == Active || queue.Contains(sequence)) return;
            queue.Enqueue(sequence);
        }
        public void PlayPriority(CutsceneSequence sequence) { Cancel(); Play(sequence); }
        void Update()
        {
            if (Time.timeScale == 0)
            {
                if (IsTyping && OwnerVoiceEnabled && dialogueSource)
                {
                    dialogueSource.Pause();
                    talkingAudioPaused = true;
                }
                return;
            }
            if (talkingAudioPaused)
            {
                talkingAudioPaused = false;
                if (IsTyping && OwnerVoiceEnabled && dialogueSource) dialogueSource.UnPause();
            }
            UpdateTypewriter();
            if (!Active)
            {
                if (controlLock != null) Finish(SequenceResult.Invalid);
                if (queue.Count == 0) return;
                var pending = queue.Peek();
                if (!pending) { queue.Dequeue(); return; }
                if (pending.waitForGrounded && cat && !cat.Grounded) return;
                Active = queue.Dequeue(); Active.HasPlayed = true;
                controlLock = input ? input.Controls.Acquire() : null; shotIndex = 0;
                Active.onStarted?.Invoke();
                if (!Active || Active.shots == null || Active.shots.Length == 0) { Finish(SequenceResult.Invalid); return; }
                BeginShot();
            }
            if (!Active) return;
            var shot = Active.shots[shotIndex];
            if (shot == null || !shot.Valid || (requiresLookTarget && !shot.lookAt)) { Finish(SequenceResult.Invalid); return; }
            elapsed += Time.deltaTime;
            shot.Evaluate(elapsed / Mathf.Max(.01f, shot.duration), out var position, out var rotation);
            float blend = Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(.001f, shot.blendIn));
            director.SetShot(Vector3.Lerp(blendPosition, position, blend), Quaternion.Slerp(blendRotation, rotation, blend), Mathf.Lerp(blendFov, shot.fieldOfView, blend));
            if (elapsed >= shot.duration && !shot.waitForAdvance)
            {
                NextShot();
            }
        }
        void NextShot() { if (++shotIndex >= Active.shots.Length) Finish(SequenceResult.Completed); else BeginShot(); }
        public void Advance()
        {
            if (Time.timeScale == 0 || !Active || Active.shots == null || shotIndex >= Active.shots.Length) return;
            if (flow && flow.feedback) flow.feedback.UIClick();
            if (IsTyping)
            {
                RevealCaption();
                return;
            }
            if (Active.shots[shotIndex].waitForAdvance && elapsed >= .15f) NextShot();
        }
        void BeginShot()
        {
            elapsed = 0; blendPosition = director.worldCamera.transform.position;
            blendRotation = director.worldCamera.transform.rotation; blendFov = director.worldCamera.fieldOfView;
            var shot = Active.shots[shotIndex];
            requiresLookTarget = shot != null && shot.lookAt;
            Caption = shot != null ? shot.caption : "";
            VisibleCharacterCount = 0;
            typewriterProgress = 0;
            IsTyping = !string.IsNullOrEmpty(Caption);
            talkingAudioPaused = false;
            if (dialogueSource)
            {
                dialogueSource.Stop();
                dialogueSource.clip = ownerTalking;
                dialogueSource.volume = talkingVolume;
                dialogueSource.loop = true;
                if (IsTyping && OwnerVoiceEnabled && ownerTalking) dialogueSource.Play();
            }
            shot?.onEnter?.Invoke();
        }
        void UpdateTypewriter()
        {
            if (!IsTyping) return;
            typewriterProgress += Time.deltaTime * Mathf.Max(1, charactersPerSecond);
            VisibleCharacterCount = Mathf.Min(Caption.Length, Mathf.FloorToInt(typewriterProgress));
            if (VisibleCharacterCount >= Caption.Length) RevealCaption();
        }
        void RevealCaption()
        {
            VisibleCharacterCount = Caption.Length;
            IsTyping = false;
            talkingAudioPaused = false;
            if (dialogueSource) dialogueSource.Stop();
        }
        public void Skip() { if (Time.timeScale > 0 && Active && Active.skippable) Finish(SequenceResult.Skipped); }
        public void Cancel() { queue.Clear(); if (Active || controlLock != null) Finish(SequenceResult.Cancelled); }
        void Finish(SequenceResult result)
        {
            if (finishing) return;
            finishing = true;
            var sequence = Active; Active = null; Caption = "";
            VisibleCharacterCount = 0; IsTyping = false;
            talkingAudioPaused = false;
            if (dialogueSource) dialogueSource.Stop();
            if (director) director.ReturnToPlayer();
            controlLock?.Dispose(); controlLock = null;
            if (sequence && (result == SequenceResult.Completed || result == SequenceResult.Skipped)) sequence.onFinished?.Invoke();
            Finished?.Invoke(sequence, result); finishing = false;
        }
        bool OwnerVoiceEnabled => flow && (flow.StageDialogueActive ||
            (flow.presentation && Active == flow.presentation.ownerDialogueSequence));
    }
}
