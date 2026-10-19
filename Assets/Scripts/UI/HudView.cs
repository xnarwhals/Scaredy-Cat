using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;
namespace ScaredyCat
{
    public sealed class HudView : MonoBehaviour
    {
        public GameFlow flow;
        public AgroMeter agro;
        public PawAttack attack;
        public CatMotor cat;
        public CutsceneManager cutscenes;
        public Image agroFill, chargeFill, fade;
        public Image[] zoomieSegments;
        public TMP_Text caption, menuTitle, menuBody;
        public GameObject menu, reticle, chargeGroup;
        public GameObject introCaptionPanel;
        [Header("Menace meter juice")]
        public Transform agroPulseTarget;
        public Color menaceHighColor = new Color(.96f, .16f, .12f, 1f);
        public Color menaceLowColor = new Color(.18f, .9f, .34f, 1f);
        public Color menaceFlashColor = new Color(1f, .92f, .6f, 1f);
        [Min(1f)] public float menacePulseScale = 1.12f;
        [Min(.05f)] public float menacePulseDuration = .28f;
        [Min(.05f)] public float menaceFlashDuration = .32f;
        public Button resume, replay;
        public Slider sensitivity, motion;
        float displayedAgro;
        bool menuWasActive;
        float previousAgro;
        float pulseRemaining, flashRemaining;
        Vector3 agroPulseBaseScale = Vector3.one;
        Button[] sceneButtons;
        UnityAction uiClickAction;
        void Start()
        {
            displayedAgro = agro.Ledger.Value;
            previousAgro = agro.Ledger.Value;
            agro.Ledger.Changed += MenaceChanged;
            if (!agroPulseTarget && agroFill) agroPulseTarget = agroFill.transform.parent;
            if (agroPulseTarget) agroPulseBaseScale = agroPulseTarget.localScale;
            resume.onClick.AddListener(flow.TogglePause); replay.onClick.AddListener(flow.Restart);
            sensitivity.onValueChanged.AddListener(value => flow.cat.SensitivityMultiplier = value);
            motion.onValueChanged.AddListener(value => flow.feedback.MotionMultiplier = value);
            uiClickAction = PlayUIClick;
            sceneButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in sceneButtons)
                if (button) button.onClick.AddListener(uiClickAction);
        }
        void OnDestroy()
        {
            if (agro && agro.Ledger != null) agro.Ledger.Changed -= MenaceChanged;
            if (sceneButtons != null)
                foreach (var button in sceneButtons)
                    if (button && uiClickAction != null) button.onClick.RemoveListener(uiClickAction);
        }
        void PlayUIClick() { if (flow && flow.feedback) flow.feedback.UIClick(); }
        void MenaceChanged(float value, float decrease)
        {
            if (decrease <= 0) return;
            float maximum = agro && agro.Ledger != null ? agro.Ledger.Maximum : 100f;
            float stageOneThreshold = maximum * (2f / 3f);
            float stageTwoThreshold = maximum / 3f;
            if ((previousAgro > stageOneThreshold && value <= stageOneThreshold) ||
                (previousAgro > stageTwoThreshold && value <= stageTwoThreshold) ||
                (previousAgro > 0 && value <= 0))
                flashRemaining = menaceFlashDuration;
            pulseRemaining = menacePulseDuration;
            previousAgro = value;
        }
        void Update()
        {
            displayedAgro = Mathf.MoveTowards(displayedAgro, agro.Ledger.Value, Time.unscaledDeltaTime * 55);
            float menaceRatio = displayedAgro / agro.Ledger.Maximum;
            agroFill.fillAmount = menaceRatio;
            Color menaceColor = Color.Lerp(menaceLowColor, menaceHighColor, menaceRatio);
            pulseRemaining = Mathf.Max(0, pulseRemaining - Time.unscaledDeltaTime);
            flashRemaining = Mathf.Max(0, flashRemaining - Time.unscaledDeltaTime);
            float pulseProgress = menacePulseDuration > 0 ? 1 - pulseRemaining / menacePulseDuration : 1;
            float pulse = Mathf.Sin(pulseProgress * Mathf.PI) * (menacePulseScale - 1);
            if (agroPulseTarget) agroPulseTarget.localScale = agroPulseBaseScale * (1 + pulse);
            float flash = menaceFlashDuration > 0 ? flashRemaining / menaceFlashDuration : 0;
            agroFill.color = Color.Lerp(menaceColor, menaceFlashColor, flash);
            bool playing = flow.State == GameState.Playing && !cutscenes.IsPlaying && !flow.Paused && !flow.DebugVisible;
            reticle.SetActive(playing); chargeGroup.SetActive(playing && attack.State == PawState.Charging);
            chargeFill.fillAmount = attack.Charge;
            if (cat && zoomieSegments != null)
                for (int i = 0; i < zoomieSegments.Length; i++)
                    if (zoomieSegments[i])
                    {
                        zoomieSegments[i].fillAmount = Mathf.Clamp01(cat.ZoomieStamina - i);
                        zoomieSegments[i].color = cat.Dashing ? new Color(.82f, .97f, 1f) : new Color(.45f, .85f, 1f);
                    }
            caption.text = cutscenes.Caption;
            caption.maxVisibleCharacters = cutscenes.IsPlaying ? cutscenes.VisibleCharacterCount : int.MaxValue;
            if (introCaptionPanel)
                introCaptionPanel.SetActive(flow.State == GameState.Intro && cutscenes.IsPlaying &&
                    !string.IsNullOrWhiteSpace(cutscenes.Caption));
            bool completed = flow.State == GameState.Complete;
            menu.SetActive(flow.Paused || (completed && (!flow.presentation || !flow.presentation.victoryScreen)));
            if (menu.activeSelf != menuWasActive)
            {
                menuWasActive = menu.activeSelf;
                if (EventSystem.current)
                    EventSystem.current.SetSelectedGameObject(menuWasActive ? (completed ? replay.gameObject : resume.gameObject) : null);
            }
            if (menu.activeSelf)
            {
                resume.gameObject.SetActive(!completed);
            }
        }
        public void SetFade(float alpha) { if (fade) fade.color = new Color(0, 0, 0, alpha); }
    }
}
