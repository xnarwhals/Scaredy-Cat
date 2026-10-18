using System;
using System.Collections;
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
        public CutsceneSequence intro, ending;
        public Transform acceptanceCat, acceptanceOwner;
        public FeedbackDirector feedback;
        public HudView hud;
        public bool mechanicsLab;
        public GameState State { get; private set; } = GameState.Intro;
        public bool Paused { get; private set; }
        public bool DebugVisible { get; private set; }
        IDisposable stateLock, pauseLock, debugLock;
        void Awake() { stateLock = input.Controls.Acquire(); }
        void Start()
        {
            input.PausePressed += TogglePause; input.DebugPressed += ToggleDebug;
            cutscenes.Finished += SequenceFinished; agro.Ledger.Calmed += BeginEnding;
            if (mechanicsLab || !intro) EnterPlaying(); else cutscenes.Play(intro);
            UpdateCursor();
        }
        void EnterPlaying() { State = GameState.Playing; stateLock?.Dispose(); stateLock = null; UpdateCursor(); }
        void SequenceFinished(CutsceneSequence sequence, SequenceResult result)
        {
            if (sequence == intro && State == GameState.Intro) EnterPlaying();
            else if (sequence == ending && State == GameState.Ending)
            {
                State = GameState.Complete;
                if (ending.shots.Length > 0 && ending.shots[ending.shots.Length - 1].Valid)
                {
                    var shot = ending.shots[ending.shots.Length - 1]; shot.Evaluate(1, out var position, out var rotation);
                    cutscenes.director.SetShot(position, rotation, shot.fieldOfView);
                }
                UpdateCursor();
            }
        }
        void BeginEnding() { if (State == GameState.Playing) StartCoroutine(EndRoutine()); }
        IEnumerator EndRoutine()
        {
            State = GameState.Ending; stateLock = input.Controls.Acquire(); SetDebug(false); cutscenes.Cancel();
            float until = Time.time + 1.5f;
            while (!cat.Grounded && Time.time < until) yield return null;
            yield return Fade(1);
            if (acceptanceCat) cat.Teleport(acceptanceCat.position, acceptanceCat.eulerAngles.y);
            if (owner && acceptanceOwner) owner.ForceCalmAt(acceptanceOwner.position, acceptanceOwner.rotation);
            feedback?.Accept();
            if (ending) cutscenes.PlayPriority(ending); else { State = GameState.Complete; UpdateCursor(); }
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
            if (agro && agro.Ledger != null) agro.Ledger.Calmed -= BeginEnding;
            stateLock?.Dispose(); pauseLock?.Dispose(); debugLock?.Dispose();
            Time.timeScale = 1; AudioListener.pause = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
