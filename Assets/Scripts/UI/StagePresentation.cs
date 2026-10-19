using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ScaredyCat
{
    public sealed class StagePresentation : MonoBehaviour
    {
        public GameFlow flow;
        public CutsceneManager cutscenes;
        public CutsceneSequence ownerDialogueSequence;
        public ParticleSystem purpleMist;
        public Image vignette;
        public TMP_Text timer, dialogue, finalTime;
        public GameObject dialoguePanel, victoryScreen;
        public Button replay;
        public float initialVignette = .24f;
        [Header("Speedrun rank cutoffs (maximum clear time in seconds)")]
        [Min(0)] public float sRankTime = 40f;
        [Min(0)] public float aRankTime = 50f;
        [Min(0)] public float bRankTime = 70f;
        [Min(0)] public float cRankTime = 90f;
        float baseEmission, desiredVignette;
        void Awake()
        {
            if (purpleMist) baseEmission = purpleMist.emission.rateOverTime.constant;
            desiredVignette = initialVignette;
            if (replay && flow) replay.onClick.AddListener(flow.Restart);
            if (victoryScreen) victoryScreen.SetActive(false);
            if (dialoguePanel) dialoguePanel.SetActive(false);
        }
        public void SetStage(int stage)
        {
            float multiplier = stage == 1 ? .6f : stage == 2 ? .25f : stage >= 3 ? 0 : 1;
            desiredVignette = stage == 1 ? .16f : stage == 2 ? .08f : stage >= 3 ? 0 : initialVignette;
            if (purpleMist)
            {
                var emission = purpleMist.emission;
                emission.rateOverTime = baseEmission * multiplier;
                if (stage >= 3) purpleMist.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        public void ShowVictory()
        {
            if (finalTime && flow)
            {
                float clearTime = flow.ElapsedTime;
                finalTime.text = $"FINAL TIME  {FormatTime(clearTime)}    RANK  {GetRank(clearTime)}";
            }
            if (victoryScreen) victoryScreen.SetActive(true);
            if (replay && EventSystem.current) EventSystem.current.SetSelectedGameObject(replay.gameObject);
        }
        string GetRank(float seconds)
        {
            if (seconds <= sRankTime) return "S";
            if (seconds <= aRankTime) return "A";
            if (seconds <= bRankTime) return "B";
            if (seconds <= cRankTime) return "C";
            return "D";
        }
        static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            return $"{minutes:00}:{Mathf.FloorToInt(seconds) % 60:00}.{Mathf.FloorToInt((seconds % 1) * 100):00}";
        }
        void Update()
        {
            if (timer && flow)
            {
                timer.text = FormatTime(flow.ElapsedTime);
            }
            if (vignette)
            {
                Color color = vignette.color;
                color.a = Mathf.MoveTowards(color.a, desiredVignette, Time.unscaledDeltaTime * .12f);
                vignette.color = color;
            }
            bool showing = flow && cutscenes && cutscenes.IsPlaying &&
                (flow.StageDialogueActive || cutscenes.Active == ownerDialogueSequence);
            if (dialoguePanel) dialoguePanel.SetActive(showing);
            if (dialogue)
            {
                dialogue.text = showing ? cutscenes.Caption : string.Empty;
                dialogue.maxVisibleCharacters = showing ? cutscenes.VisibleCharacterCount : int.MaxValue;
            }
        }
    }
}
