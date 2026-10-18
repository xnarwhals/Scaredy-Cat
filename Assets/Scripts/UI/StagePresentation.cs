using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ScaredyCat
{
    public sealed class StagePresentation : MonoBehaviour
    {
        public GameFlow flow;
        public CutsceneManager cutscenes;
        public ParticleSystem purpleMist;
        public Image vignette;
        public TMP_Text timer, dialogue, finalTime;
        public GameObject dialoguePanel, victoryScreen;
        public Button replay;
        public float initialVignette = .24f;
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
            if (finalTime && flow) finalTime.text = "FINAL TIME  " + FormatTime(flow.ElapsedTime);
            if (victoryScreen) victoryScreen.SetActive(true);
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
            bool showing = flow && flow.StageDialogueActive && cutscenes && cutscenes.IsPlaying;
            if (dialoguePanel) dialoguePanel.SetActive(showing);
            if (dialogue && showing) dialogue.text = cutscenes.Caption;
        }
    }
}
