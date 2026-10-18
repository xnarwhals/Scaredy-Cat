using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEditor;
namespace ScaredyCat.Editor
{
    public static partial class FirstPlayableBuilder
    {
        static readonly Color Cream = new Color(.96f, .92f, .8f);
        static readonly Color Ink = new Color(.075f, .1f, .13f, .94f);
        static Sprite FillSprite()
        {
            string path = Content + "/Materials/HUD white.asset";
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite existing) return existing;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "HUD white", filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white); texture.Apply(); AssetDatabase.CreateAsset(texture, path);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            sprite.name = "HUD fill"; AssetDatabase.AddObjectToAsset(sprite, texture); return sprite;
        }
        static HudView BuildHud(Transform parent, Camera camera, GameFlow flow, AgroMeter agro, PawAttack paw, CutsceneManager cutscenes)
        {
            var canvasObject = new GameObject("Game HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var output = new GameObject("Pixel world", typeof(RectTransform), typeof(RawImage)); output.transform.SetParent(canvasObject.transform, false); Stretch(output.GetComponent<RectTransform>());
            var composition = canvasObject.AddComponent<PixelComposition>(); composition.worldCamera = camera; composition.pixelWorldOutput = output.GetComponent<RawImage>(); composition.pixelHeight = 288;
            var hud = canvasObject.AddComponent<HudView>(); hud.flow = flow; hud.agro = agro; hud.attack = paw; hud.cutscenes = cutscenes;
            var title = Text("Title", canvasObject.transform, "SCAREDY CAT", 22, TextAnchor.MiddleLeft); Place(title.rectTransform, new Vector2(0, 1), new Vector2(32, -30), new Vector2(320, 32), new Vector2(0, 1));
            var sub = Text("Subtitle", canvasObject.transform, "A VERY UNFAMILIAR HOME", 11, TextAnchor.MiddleLeft); sub.color = new Color(.85f, .8f, .65f); Place(sub.rectTransform, new Vector2(0, 1), new Vector2(33, -62), new Vector2(300, 20), new Vector2(0, 1));
            var meter = MakeImage("Agro track", canvasObject.transform, Ink); Place(meter.rectTransform, new Vector2(0, 1), new Vector2(32, -94), new Vector2(270, 12), new Vector2(0, 1));
            hud.agroFill = MakeImage("Agro fill", meter.transform, new Color(.96f, .39f, .25f)); Stretch(hud.agroFill.rectTransform); hud.agroFill.type = Image.Type.Filled; hud.agroFill.fillMethod = Image.FillMethod.Horizontal;
            hud.agroFill.sprite = FillSprite();
            var reticle = Text("Reticle", canvasObject.transform, "+", 22, TextAnchor.MiddleCenter); Place(reticle.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(30, 30), new Vector2(.5f, .5f)); hud.reticle = reticle.gameObject;
            var charge = MakeImage("Charge track", canvasObject.transform, Ink); Place(charge.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -38), new Vector2(110, 5), new Vector2(.5f, .5f)); hud.chargeGroup = charge.gameObject;
            hud.chargeFill = MakeImage("Charge fill", charge.transform, Cream); Stretch(hud.chargeFill.rectTransform); hud.chargeFill.type = Image.Type.Filled; hud.chargeFill.fillMethod = Image.FillMethod.Horizontal;
            hud.chargeFill.sprite = hud.agroFill.sprite;
            hud.caption = Text("Narrative caption", canvasObject.transform, "", 25, TextAnchor.MiddleCenter); Place(hud.caption.rectTransform, new Vector2(.5f, 0), new Vector2(0, 100), new Vector2(1050, 80), new Vector2(.5f, 0));
            var shadow = hud.caption.gameObject.AddComponent<Shadow>(); shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(2, -2);
            var menu = MakeImage("Pause and completion", canvasObject.transform, Ink); Stretch(menu.rectTransform); menu.raycastTarget = true; hud.menu = menu.gameObject;
            hud.menuTitle = Text("Menu title", menu.transform, "CAT NAP", 38, TextAnchor.MiddleCenter); Place(hud.menuTitle.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 175), new Vector2(950, 60), new Vector2(.5f, .5f));
            hud.menuBody = Text("Menu caption", menu.transform, "", 18, TextAnchor.MiddleCenter); Place(hud.menuBody.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 120), new Vector2(900, 40), new Vector2(.5f, .5f));
            hud.resume = Button("Back to mischief", menu.transform, new Vector2(0, 55));
            hud.replay = Button("Start again", menu.transform, new Vector2(0, -5));
            hud.sensitivity = Slider("Mouse sensitivity", menu.transform, -100, .25f, 2.5f, 1);
            hud.motion = Slider("Camera motion", menu.transform, -175, 0, 1, 1);
            menu.gameObject.SetActive(false);
            hud.fade = MakeImage("Transition fade", canvasObject.transform, new Color(0, 0, 0, 0)); Stretch(hud.fade.rectTransform);
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.transform.SetParent(parent, false);
            return hud;
        }
        static Image MakeImage(string name, Transform parent, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }
        static Text Text(string name, Transform parent, string content, int size, TextAnchor alignment)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>(); text.text = content; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size;
            text.alignment = alignment; text.color = Cream; text.raycastTarget = false; return text;
        }
        static Button Button(string label, Transform parent, Vector2 position)
        {
            var image = MakeImage(label, parent, new Color(.24f, .32f, .34f)); image.raycastTarget = true;
            Place(image.rectTransform, new Vector2(.5f, .5f), position, new Vector2(330, 48), new Vector2(.5f, .5f));
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var text = Text("Label", image.transform, label, 18, TextAnchor.MiddleCenter); Stretch(text.rectTransform); return button;
        }
        static Slider Slider(string label, Transform parent, float y, float minimum, float maximum, float initial)
        {
            var root = new GameObject(label, typeof(RectTransform), typeof(Slider)); root.transform.SetParent(parent, false);
            Place(root.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(0, y), new Vector2(330, 25), new Vector2(.5f, .5f));
            var title = Text("Label", root.transform, label, 14, TextAnchor.MiddleLeft); Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 10), new Vector2(330, 24), new Vector2(0, 0));
            var track = MakeImage("Track", root.transform, new Color(.24f, .31f, .34f)); Stretch(track.rectTransform); track.rectTransform.offsetMin = new Vector2(0, 9); track.rectTransform.offsetMax = new Vector2(0, -9); track.raycastTarget = true;
            var handleArea = new GameObject("Handle area", typeof(RectTransform)).GetComponent<RectTransform>(); handleArea.SetParent(root.transform, false); Stretch(handleArea); handleArea.offsetMin = new Vector2(6, 0); handleArea.offsetMax = new Vector2(-6, 0);
            var handle = MakeImage("Handle", handleArea, Cream); handle.rectTransform.sizeDelta = new Vector2(12, 24); handle.raycastTarget = true;
            var slider = root.GetComponent<Slider>(); slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.minValue = minimum; slider.maxValue = maximum; slider.value = initial; return slider;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.sizeDelta = size; rect.anchoredPosition = position; }
    }
}
