using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ScaredyCat
{
    public enum GameState { Intro, Playing, Ending, Complete }
    [DefaultExecutionOrder(-100)]
    public sealed class GameFlow : MonoBehaviour
    {
        public GameInput input;
        public AgroMeter agro;
        public CatMotor cat;
        public OwnerRagdoll owner;
        public CutsceneManager cutscenes;
        public CutsceneSequence intro, stageOne, stageTwo, ending;
        public Transform acceptanceCat, acceptanceOwner;
        [Range(.2f, 1f)] public float endingCatScaleMultiplier = .62f;
        public FeedbackDirector feedback;
        [Header("Menace stage soundtrack")]
        public AudioSource musicSource;
        public AudioClip menaceMusicPt1, menaceMusicPt2, menaceMusicPt3;
        public HudView hud;
        public StagePresentation presentation;
        public bool mechanicsLab;
        public GameState State { get; private set; } = GameState.Intro;
        public bool Paused { get; private set; }
        public bool DebugVisible { get; private set; }
        public bool StageDialogueActive { get; private set; }
        public float ElapsedTime { get; private set; }
        bool timerRunning, processingStages;
        int highestStage;
        readonly Queue<int> milestones = new Queue<int>();
        IDisposable stateLock, pauseLock, debugLock;
        Vector3 originalCatScale;
        void Awake() { stateLock = input.Controls.Acquire(); if (cat) originalCatScale = cat.transform.localScale; }
        void Start()
        {
            input.PausePressed += TogglePause; input.DebugPressed += ToggleDebug;
            cutscenes.Finished += SequenceFinished; agro.Ledger.Changed += MeterChanged;
            UpdateSoundtrack(agro.Ledger.Value);
            if (mechanicsLab || !intro) EnterPlaying(); else cutscenes.Play(intro);
            UpdateCursor();
        }
        void EnterPlaying() { State = GameState.Playing; stateLock?.Dispose(); stateLock = null; timerRunning = true; UpdateCursor(); }
        void Update() { if (timerRunning && !Paused) ElapsedTime += Time.unscaledDeltaTime; }
        void MeterChanged(float value, float decrease)
        {
            if (State != GameState.Playing) return;
            float maximum = agro && agro.Ledger != null ? agro.Ledger.Maximum : 100f;
            UpdateSoundtrack(value);
            int reached = value <= 0 ? 3 : value <= maximum / 3f ? 2 : value <= maximum * (2f / 3f) ? 1 : 0;
            for (int stage = highestStage + 1; stage <= reached; stage++) milestones.Enqueue(stage);
            if (reached > highestStage)
            {
                highestStage = reached;
                if (reached == 3) timerRunning = false;
            }
            if (!processingStages && milestones.Count > 0) StartCoroutine(ProcessMilestones());
        }
        void UpdateSoundtrack(float menace)
        {
            if (!musicSource || !agro || agro.Ledger == null) return;
            float ratio = menace / agro.Ledger.Maximum;
            AudioClip next = ratio <= 1f / 3f ? menaceMusicPt1 :
                ratio <= 2f / 3f ? menaceMusicPt2 : menaceMusicPt3;
            if (!next || (musicSource.clip == next && musicSource.isPlaying)) return;
            musicSource.Stop();
            musicSource.clip = next;
            musicSource.loop = true;
            musicSource.Play();
        }
        IEnumerator ProcessMilestones()
        {
            processingStages = true;
            while (milestones.Count > 0)
            {
                int stage = milestones.Dequeue();
                presentation?.SetStage(stage);
                if (stage == 3) { yield return EndRoutine(); break; }
                var sequence = stage == 1 ? stageOne : stageTwo;
                if (!sequence || !cutscenes) continue;
                while (State == GameState.Intro || cutscenes.IsPlaying || (owner && owner.State != OwnerState.Animated)) yield return null;
                var brain = owner ? owner.brain : null;
                bool previousStay = brain && brain.Stay;
                if (brain) brain.Stay = true;
                StageDialogueActive = true;
                cutscenes.Play(sequence);
                while (!sequence.HasPlayed) yield return null;
                while (cutscenes.IsPlaying) yield return null;
                StageDialogueActive = false;
                if (brain) brain.Stay = previousStay;
            }
            processingStages = false;
        }
        void SequenceFinished(CutsceneSequence sequence, SequenceResult result)
        {
            if (sequence == intro && State == GameState.Intro) EnterPlaying();
            else if (sequence == ending && State == GameState.Ending)
            {
                StageDialogueActive = false;
                if (ending.shots.Length > 0 && ending.shots[ending.shots.Length - 1].Valid)
                {
                    var shot = ending.shots[ending.shots.Length - 1]; shot.Evaluate(1, out var position, out var rotation);
                    cutscenes.director.SetShot(position, rotation, shot.fieldOfView);
                }
                StartCoroutine(CompleteRoutine());
            }
        }
        IEnumerator CompleteRoutine()
        {
            yield return Fade(1);
            State = GameState.Complete;
            presentation?.ShowVictory();
            UpdateCursor();
            yield return Fade(0);
        }
        IEnumerator EndRoutine()
        {
            State = GameState.Ending; stateLock = input.Controls.Acquire(); SetDebug(false); cutscenes.Cancel();
            float until = Time.time + 1.5f;
            while (!cat.Grounded && Time.time < until) yield return null;
            yield return Fade(1);
            if (acceptanceCat)
            {
                cat.transform.localScale = originalCatScale * endingCatScaleMultiplier;
                cat.Teleport(acceptanceCat.position, acceptanceCat.eulerAngles.y);
            }
            if (owner && acceptanceOwner) owner.ForceCalmAt(acceptanceOwner.position, acceptanceOwner.rotation);
            if (owner && owner.brain) owner.brain.BeginSitting();
            feedback?.Accept();
            if (ending) { StageDialogueActive = true; cutscenes.PlayPriority(ending); }
            else StartCoroutine(CompleteRoutine());
            yield return Fade(0);
        }
        IEnumerator Fade(float target)
        {
            if (!hud || !hud.fade) yield break;
            float start = hud.fade.color.a;
            for (float t = 0; t < .3f; t += Time.deltaTime)
            { hud.SetFade(Mathf.Lerp(start, target, t / .3f)); yield return null; }
            hud.SetFade(target);
        }
        public void TogglePause()
        {
            if (State == GameState.Complete) return;
            Paused = !Paused;
            if (Paused) { pauseLock = input.Controls.Acquire(); Time.timeScale = 0; AudioListener.pause = true; }
            else { Time.timeScale = 1; AudioListener.pause = false; pauseLock?.Dispose(); pauseLock = null; }
            UpdateCursor();
        }
        void ToggleDebug() { if (mechanicsLab) SetDebug(!DebugVisible); }
        public void SetDebug(bool visible)
        {
            DebugVisible = visible;
            if (visible && debugLock == null) debugLock = input.Controls.Acquire();
            if (!visible) { debugLock?.Dispose(); debugLock = null; }
            UpdateCursor();
        }
        void UpdateCursor()
        {
            bool free = Paused || DebugVisible || State == GameState.Complete;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = free;
        }
        public void Restart()
        {
            Time.timeScale = 1; AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        void OnApplicationFocus(bool focus) { if (!Application.isBatchMode && !focus && !Paused && State != GameState.Complete) TogglePause(); }
        void OnDestroy()
        {
            if (input) { input.PausePressed -= TogglePause; input.DebugPressed -= ToggleDebug; }
            if (cutscenes) cutscenes.Finished -= SequenceFinished;
            if (agro && agro.Ledger != null) agro.Ledger.Changed -= MeterChanged;
            stateLock?.Dispose(); pauseLock?.Dispose(); debugLock?.Dispose();
            Time.timeScale = 1; AudioListener.pause = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
