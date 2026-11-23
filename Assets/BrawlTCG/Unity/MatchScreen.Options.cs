using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// In-match options: a gear button (top right) or Esc opens a menu with a sound-effects switch, a volume slider
    /// and "leave match".
    /// It lives on its own layer above every overlay, so it can be opened over the hand-off and game-over screens
    /// and closing it leaves those untouched.
    /// </summary>
    public sealed partial class MatchScreen
    {
        RectTransform _menu;
        bool _optionsOpen; // a flag, because cleared children only disappear at the end of the frame

        bool OptionsOpen => _optionsOpen;

        void ToggleOptions()
        {
            if (OptionsOpen) CloseOptions();
            else OpenOptions();
        }

        void CloseOptions()
        {
            _optionsOpen = false;
            if (_menu != null) UI.Clear(_menu);
        }

        RectTransform MenuDim()
        {
            UI.Clear(_menu);
            _optionsOpen = true;
            var dim = UI.FullPanel(_menu, new Color(Theme.Night.r, Theme.Night.g, Theme.Night.b, 0.72f), "Dim");
            UI.Clicks(dim.gameObject, e => CloseOptions()); // clicking outside the panel closes it
            return dim.rectTransform;
        }

        Transform MenuPanel(RectTransform dim, string title, float height)
        {
            const float w = 560;
            float x = 960 - w / 2, y = 540 - height / 2;
            var box = Theme.GlassPanel(dim, x, y, w, height, 28, "Options");
            box.color = new Color32(14, 18, 42, 255);
            UI.Clicks(box.gameObject, e => { }); // clicks on the panel itself don't close it
            box.gameObject.AddComponent<SlideIn>().Offset = new Vector2(0, -24);
            var t = Theme.Label(box.transform, title, 40, Theme.Text, 0, 28, w, 56, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(t, 3, 0.6f);
            return box.transform;
        }

        void OpenOptions()
        {
            Sfx.Play("ui_click", 0.6f);
            var dim = MenuDim();
            Transform box = MenuPanel(dim, "OPTIONS", 504);
            const float w = 560;

            // Sound effects switch
            var row = Theme.RoundedPanel(box, new Color(1, 1, 1, 0.05f), 40, 112, w - 80, 72, 16, "SfxRow");
            Theme.Label(row.transform, "SOUND EFFECTS", 22, Theme.Text, 24, 0, 300, 72, TextAnchor.MiddleLeft, display: true);
            bool on = !Sfx.Muted;
            Theme.Label(row.transform, on ? "ON" : "OFF", 18, on ? Theme.Success : Theme.TextDim, w - 80 - 168, 0, 60, 72, TextAnchor.MiddleRight, display: true);
            Switch(row.transform, w - 80 - 96, 20, on, () =>
            {
                Sfx.Muted = !Sfx.Muted;
                if (!Sfx.Muted) Sfx.Play("ui_click", 0.8f);
                OpenOptions(); // redraw
            });

            // Volume slider
            var volRow = Theme.RoundedPanel(box, new Color(1, 1, 1, 0.05f), 40, 196, w - 80, 72, 16, "VolumeRow");
            Theme.Label(volRow.transform, "VOLUME", 22, Theme.Text, 24, 0, 140, 72, TextAnchor.MiddleLeft, display: true);
            var percent = Theme.Label(volRow.transform, Percent(Sfx.Volume), 18, Theme.TextDim, w - 80 - 84, 0, 60, 72, TextAnchor.MiddleRight, display: true);
            VolumeSlider(volRow.transform, 164, 20, w - 80 - 164 - 96, Sfx.Volume, v =>
            {
                Sfx.Volume = v;
                percent.text = Percent(v);
                if (Sfx.Muted && v > 0) Sfx.Muted = false; // turning it up turns sound back on
                Sfx.Play("ui_click", 0.8f, 0, 0.12f); // preview at the new volume
            });

            Theme.Cta(box, "RESUME", 40, 300, w - 80, 66, CloseOptions, 26);
            Theme.Ghost(box, _ended || _game.IsOver ? "BACK TO MENU" : "LEAVE MATCH", 40, 382, w - 80, 66, ConfirmLeave, 24);
            Theme.Label(box, "Esc to close", 15, Theme.TextFaint, 0, 462, w, 24, TextAnchor.MiddleCenter);
        }

        void ConfirmLeave()
        {
            if (_ended || _game.IsOver)
            {
                LeaveMatch();
                return;
            }
            var dim = MenuDim();
            Transform box = MenuPanel(dim, "LEAVE MATCH?", 330);
            const float w = 560;
            Theme.Label(box, _hotSeat || VsBot ? "The current game will be lost." : "Your opponent will be told that you left.", 20, Theme.TextDim,
                40, 96, w - 80, 70, TextAnchor.MiddleCenter);
            Theme.Ghost(box, "STAY", 40, 206, (w - 100) / 2, 66, OpenOptions, 24);
            var go = Theme.Cta(box, "LEAVE", 60 + (w - 100) / 2, 206, (w - 100) / 2, 66, LeaveMatch, 26);
            go.GetComponent<HoverFx>().Init(go.GetComponent<Image>(), new Color32(230, 90, 90, 255), new Color32(250, 120, 120, 255), 1.025f);
        }

        static string Percent(float v) => $"{Mathf.RoundToInt(v * 100)}%";

        /// <summary>A gold slider (0..1): track, fill and a round knob; click or drag anywhere along it.</summary>
        static void VolumeSlider(Transform parent, float x, float y, float w, float value, System.Action<float> changed)
        {
            const float h = 32, track = 10;
            var root = UI.Place(UI.Rect("VolumeSlider", parent), x, y, w, h);
            var hit = UI.Panel(root, new Color(0, 0, 0, 0), 0, 0, w, h, "Hit"); // the whole strip takes clicks
            Theme.RoundedPanel(root, new Color(1, 1, 1, 0.16f), 0, (h - track) / 2, w, track, track / 2, "Track").raycastTarget = false;

            var fillArea = UI.Place(UI.Rect("FillArea", root), 0, (h - track) / 2, w, track);
            var fill = Theme.RoundedPanel(fillArea, Theme.Gold, 0, 0, w, track, track / 2, "Fill");
            fill.raycastTarget = false;
            RectTransform fr = fill.rectTransform;
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.pivot = new Vector2(0.5f, 0.5f);
            fr.offsetMin = fr.offsetMax = Vector2.zero;

            // The knob travels inside a strip inset by half its size, so it never sticks out of the ends.
            var handleArea = UI.Stretch(UI.Rect("HandleArea", root));
            handleArea.offsetMin = new Vector2(h / 2, 0);
            handleArea.offsetMax = new Vector2(-h / 2, 0);
            var knob = Theme.RoundedPanel(handleArea, Color.white, 0, 0, h, h, h / 2, "Knob");
            knob.raycastTarget = false;
            RectTransform kr = knob.rectTransform;
            kr.anchorMin = new Vector2(0, 0);
            kr.anchorMax = new Vector2(0, 1);
            kr.pivot = new Vector2(0.5f, 0.5f);
            kr.sizeDelta = new Vector2(h, 0);
            kr.anchoredPosition = Vector2.zero;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.targetGraphic = hit;
            slider.fillRect = fr;
            slider.handleRect = kr;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = Mathf.Clamp01(value);
            slider.onValueChanged.AddListener(v => changed(v));
        }

        /// <summary>A pill-shaped on/off switch.</summary>
        internal static void Switch(Transform parent, float x, float y, bool on, System.Action toggle)
        {
            const float w = 72, h = 32;
            Color track = on ? Theme.Success : new Color(1, 1, 1, 0.16f);
            var bg = Theme.RoundedPanel(parent, track, x, y, w, h, h / 2, "Switch");
            var b = bg.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => toggle());
            var knob = Theme.RoundedPanel(bg.transform, Color.white, on ? w - h + 3 : 3, 3, h - 6, h - 6, (h - 6) / 2, "Knob");
            knob.raycastTarget = false;
        }

        /// <summary>Round gear button: cog drawn from a disc, teeth and a hub.</summary>
        void GearButton(Transform parent, float x, float y, float size)
        {
            var bg = Theme.RoundedPanel(parent, new Color(0.04f, 0.05f, 0.14f, 0.85f), x, y, size, size, size / 2, "Options");
            Theme.RoundedPanel(bg.transform, new Color(1, 1, 1, 0.16f), 0, 0, size, size, size / 2, "Line", outline: true).raycastTarget = false;
            var button = bg.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(ToggleOptions);
            bg.gameObject.AddComponent<HoverFx>().Init(bg, bg.color, new Color(0.12f, 0.14f, 0.3f, 0.95f), 1.06f);

            // Cog: four rounded bars through the centre (eight teeth), a disc over them and a dark hub.
            var holder = UI.Place(UI.Rect("Cog", bg.transform), 0, 0, size, size);
            Color c = Theme.Text;
            for (int i = 0; i < 4; i++)
            {
                var bar = Theme.RoundedPanel(holder, c, size * 0.435f, size * 0.19f, size * 0.13f, size * 0.62f, size * 0.03f, "Teeth");
                bar.raycastTarget = false;
                bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                bar.rectTransform.anchoredPosition = new Vector2(size / 2, -size / 2);
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 45);
            }
            float ring = size * 0.48f, hub = size * 0.22f;
            Theme.RoundedPanel(holder, c, (size - ring) / 2, (size - ring) / 2, ring, ring, ring / 2, "Ring").raycastTarget = false;
            Theme.RoundedPanel(holder, new Color(0.04f, 0.05f, 0.14f, 1f), (size - hub) / 2, (size - hub) / 2, hub, hub, hub / 2, "Hub").raycastTarget = false;
        }
    }
}
