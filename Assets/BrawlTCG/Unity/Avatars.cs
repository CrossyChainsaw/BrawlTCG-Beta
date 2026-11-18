using System;
using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Player avatars: a legend's portrait chosen by the player, or the first letter of their name (#44).
    /// The choice is a legend card id (<see cref="None"/> = initial), saved in PlayerPrefs and sent to the opponent in HELLO.
    /// </summary>
    public static class Avatars
    {
        public const int None = -1;
        const string Pref = "BrawlTCG.Avatar";

        public static int Saved
        {
            get => Protocol.ValidAvatar(PlayerPrefs.GetInt(Pref, None));
            set
            {
                PlayerPrefs.SetInt(Pref, Protocol.ValidAvatar(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Every legend that has art, in catalogue order.</summary>
        public static List<LegendCard> Choices() =>
            CardCatalogue.All.OfType<LegendCard>().Where(l => Art.Get(l.Art) != null).ToList();

        /// <summary>
        /// Fills the round <paramref name="circle"/> (<paramref name="size"/> wide) with the legend's portrait, or with the
        /// initial of <paramref name="name"/> when no avatar is chosen. Clears what the circle showed before.
        /// </summary>
        public static void Paint(Image circle, float size, int avatarId, string name, int fontSize, Color textColor)
        {
            UI.Clear(circle.transform);
            Texture2D tex = Protocol.ValidAvatar(avatarId) == None ? null : Art.Get(CardCatalogue.Get(avatarId).Art);
            if (tex != null)
            {
                // The circle masks the square art into a round portrait.
                if (circle.GetComponent<Mask>() == null) circle.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                UI.Picture(circle.transform, tex, 0, 0, size, size, cover: true);
                return;
            }
            string trimmed = (name ?? "").Trim();
            string initial = trimmed.Length > 0 ? trimmed.Substring(0, 1).ToUpperInvariant() : "?";
            Theme.Label(circle.transform, initial, fontSize, textColor, 0, 0, size, size, TextAnchor.MiddleCenter, display: true).raycastTarget = false;
        }

        /// <summary>
        /// A modal grid of legend portraits on <paramref name="layer"/>. Picking one (or USE INITIAL) calls
        /// <paramref name="picked"/> and closes it; clicking outside closes it without a change.
        /// </summary>
        public static void OpenPicker(RectTransform layer, int current, string name, Action<int> picked)
        {
            UI.Clear(layer);
            layer.SetAsLastSibling();
            void Close() => UI.Clear(layer);

            var dim = UI.FullPanel(layer, new Color(Theme.Night.r, Theme.Night.g, Theme.Night.b, 0.78f), "Dim");
            UI.Clicks(dim.gameObject, _ => Close());

            const float w = 940, h = 760, cell = 88, gap = 14;
            var box = Theme.GlassPanel(dim.rectTransform, 960 - w / 2, 540 - h / 2, w, h, 28, "AvatarPicker");
            box.color = new Color32(14, 18, 42, 255);
            UI.Clicks(box.gameObject, _ => { });
            box.gameObject.AddComponent<SlideIn>().Offset = new Vector2(0, -24);
            Transform t = box.transform;

            var title = Theme.Label(t, "CHOOSE YOUR AVATAR", 38, Theme.Text, 0, 26, w, 54, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(title, 3, 0.6f);
            var hovered = Theme.Label(t, "Your opponent sees it next to your name.", 18, Theme.TextDim, 0, 82, w, 28, TextAnchor.MiddleCenter);
            var close = Theme.Ghost(t, "×", w - 70, 26, 44, 44, Close, 30);
            close.GetComponentInChildren<Text>().font = Theme.Body; // the display font has no multiplication sign

            // Scrolling grid of portraits
            var area = UI.Place(UI.Rect("Grid", t), 30, 126, w - 60, h - 126 - 104);
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 50;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = UI.Stretch(UI.Rect("Viewport", area));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var content = UI.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cell, cell);
            grid.spacing = new Vector2(gap, gap);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            foreach (LegendCard legend in Choices())
            {
                LegendCard l = legend;
                var slot = UI.Rect(l.Name, content);
                bool selected = l.Id == current;
                var ring = Theme.RoundedPanel(slot, selected ? Theme.Gold : new Color(1, 1, 1, 0.12f), 0, 0, cell, cell, cell / 2, "Ring");
                var portrait = Theme.RoundedPanel(ring.transform, CardStyle.FrameColor(l.Element), 5, 5, cell - 10, cell - 10, (cell - 10) / 2, "Portrait");
                portrait.raycastTarget = false;
                Paint(portrait, cell - 10, l.Id, l.Name, 30, Theme.Ink);
                var b = ring.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Close(); picked(l.Id); });
                ring.gameObject.AddComponent<HoverFx>().Init(ring, ring.color, selected ? ring.color : new Color(1, 1, 1, 0.35f), 1.08f);
                ring.gameObject.AddComponent<HoverText>().Init(hovered, l.Name.ToUpperInvariant());
            }

            Theme.Ghost(t, "USE MY INITIAL", 30, h - 88, (w - 74) / 2, 60, () => { Close(); picked(None); }, 20);
            Theme.Ghost(t, "CANCEL", 44 + (w - 74) / 2, h - 88, (w - 74) / 2, 60, Close, 20);
        }
    }

    /// <summary>While hovered, shows a text in a shared label (restored on exit).</summary>
    public sealed class HoverText : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        Text _label;
        string _text, _previous;

        public void Init(Text label, string text)
        {
            _label = label;
            _text = text;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
        {
            if (_label == null) return;
            _previous = _label.text;
            _label.text = _text;
            _label.color = Theme.Gold;
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
        {
            if (_label == null || _previous == null) return;
            _label.text = _previous;
            _label.color = Theme.TextDim;
            _previous = null;
        }
    }
}
