using UnityEngine;
using UnityEngine.UI;
namespace ScaredyCat
{
    public sealed class HudView : MonoBehaviour
    {
        public GameFlow flow;
        public AgroMeter agro;
        public PawAttack attack;
        public CutsceneManager cutscenes;
        public Image agroFill, chargeFill, fade;
        public Text caption, menuTitle, menuBody;
        public GameObject menu, reticle, chargeGroup;
        public Button resume, replay;
        public Slider sensitivity, motion;
        float displayedAgro;
        void Start()
        {
            displayedAgro = agro.Ledger.Maximum;
            resume.onClick.AddListener(flow.TogglePause); replay.onClick.AddListener(flow.Restart);
            sensitivity.onValueChanged.AddListener(value => flow.cat.SensitivityMultiplier = value);
            motion.onValueChanged.AddListener(value => flow.feedback.MotionMultiplier = value);
        }
        void Update()
        {
            displayedAgro = Mathf.MoveTowards(displayedAgro, agro.Ledger.Value, Time.unscaledDeltaTime * 55);
            agroFill.fillAmount = displayedAgro / agro.Ledger.Maximum;
            bool playing = flow.State == GameState.Playing && !cutscenes.IsPlaying && !flow.Paused && !flow.DebugVisible;
            reticle.SetActive(playing); chargeGroup.SetActive(playing && attack.State == PawState.Charging);
            chargeFill.fillAmount = attack.Charge;
            caption.text = cutscenes.Caption;
            bool completed = flow.State == GameState.Complete;
            menu.SetActive(flow.Paused || completed);
            if (menu.activeSelf)
            {
                menuTitle.text = completed ? "THIS FEELS LIKE HOME." : "CAT NAP";
                menuBody.text = completed ? "A little chaos. A new beginning." : "Take a breath. The vases can wait.";
                resume.gameObject.SetActive(!completed);
            }
        }
        public void SetFade(float alpha) { if (fade) fade.color = new Color(0, 0, 0, alpha); }
    }
}
