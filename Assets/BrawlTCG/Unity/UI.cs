using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>Small helpers to build uGUI from code. Coordinates are in a 1920x1080 top-left design space.</summary>
    public static class UI
    {
        static Font _font;
        public static Font Font => _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static readonly Color ZoneColor = new Color32(105, 105, 105, 255);       // SystemColors.ControlDarkDark
        public static readonly Color GameBackground = new Color32(25, 25, 112, 255);     // MidnightBlue
        public static readonly Color ActiveGreen = new Color32(0, 128, 0, 255);
        public static readonly Color CardBack = new Color32(173, 216, 230, 255);         // LightBlue
        public static readonly Color ButtonGray = new Color32(211, 211, 211, 255);       // LightGray
        public static readonly Color ButtonGreen = new Color32(144, 238, 144, 255);      // LightGreen
        public static readonly Color MenuPink = new Color32(255, 192, 255, 255);
        public static readonly Color MediumPurple = new Color32(147, 112, 219, 255);
        public static readonly Color Highlight = new Color32(255, 215, 0, 255);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Panel(Transform parent, Color color, float x, float y, float w, float h, string name = "Panel")
        {
            var rt = Place(Rect(name, parent), x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image FullPanel(Transform parent, Color color, string name = "Panel")
        {
            var rt = Stretch(Rect(name, parent));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, float x, float y, float w, float h,
            TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Bold, bool bestFit = false)
        {
            var rt = Place(Rect("Label", parent), x, y, w, h);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.supportRichText = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            if (bestFit)
            {
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = Mathf.Max(8, size / 2);
                t.resizeTextMaxSize = size;
            }
            return t;
        }

        public static Button Button(Transform parent, string text, float x, float y, float w, float h, Action onClick,
            Color? background = null, int fontSize = 16, Color? textColor = null)
        {
            var img = Panel(parent, background ?? ButtonGray, x, y, w, h, "Button");
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            var label = Label(img.transform, text, fontSize, textColor ?? Color.black, 0, 0, w, h, TextAnchor.MiddleCenter, FontStyle.Bold, bestFit: true);
            Stretch(label.rectTransform, 4);
            return button;
        }

        public static Text ButtonText(Button b) => b.GetComponentInChildren<Text>();

        public static InputField Input(Transform parent, string placeholder, string value, float x, float y, float w, float h, int fontSize = 18)
        {
            var bg = Panel(parent, Color.white, x, y, w, h, "Input");
            var text = Label(bg.transform, "", fontSize, Color.black, 0, 0, w, h, TextAnchor.MiddleLeft, FontStyle.Normal);
            Stretch(text.rectTransform, 6);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ph = Label(bg.transform, placeholder, fontSize, new Color(0.5f, 0.5f, 0.5f), 0, 0, w, h, TextAnchor.MiddleLeft, FontStyle.Italic);
            Stretch(ph.rectTransform, 6);
            var input = bg.gameObject.AddComponent<InputField>();
            input.targetGraphic = bg;
            input.textComponent = text;
            input.placeholder = ph;
            input.lineType = InputField.LineType.SingleLine;
            input.text = value ?? "";
            return input;
        }

        public static RawImage Picture(Transform parent, Texture texture, float x, float y, float w, float h, bool cover)
        {
            var rt = Place(Rect("Picture", parent), x, y, w, h);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.raycastTarget = false;
            SetPicture(img, texture, w, h, cover);
            return img;
        }

        /// <summary>Cover: crop to fill the box. Fit: shrink the box to keep the aspect ratio (centered).</summary>
        public static void SetPicture(RawImage img, Texture texture, float w, float h, bool cover)
        {
            img.texture = texture;
            if (texture == null)
            {
                img.color = new Color(0, 0, 0, 0.25f);
                return;
            }
            img.color = Color.white;
            float texAspect = (float)texture.width / texture.height;
            float boxAspect = w / h;
            if (cover)
            {
                if (texAspect > boxAspect)
                {
                    float u = boxAspect / texAspect;
                    img.uvRect = new Rect((1 - u) / 2, 0, u, 1);
                }
                else
                {
                    float v = texAspect / boxAspect;
                    img.uvRect = new Rect(0, (1 - v) / 2, 1, v);
                }
            }
            else
            {
                var rt = img.rectTransform;
                Vector2 pos = rt.anchoredPosition;
                float nw = w, nh = h;
                if (texAspect > boxAspect) nh = w / texAspect; else nw = h * texAspect;
                rt.anchoredPosition = new Vector2(pos.x + (w - nw) / 2, pos.y - (h - nh) / 2);
                rt.sizeDelta = new Vector2(nw, nh);
            }
        }

        public static ClickCatcher Clicks(GameObject go, Action<PointerEventData> onClick)
        {
            var c = go.GetComponent<ClickCatcher>();
            if (c == null) c = go.AddComponent<ClickCatcher>();
            c.OnClick = onClick;
            return c;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        public static bool IsTyping()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return false;
            var input = es.currentSelectedGameObject.GetComponent<InputField>();
            return input != null && input.isFocused;
        }
    }

    /// <summary>Raises a callback when the graphic on this object is clicked.</summary>
    public class ClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        public Action<PointerEventData> OnClick;
        public void OnPointerClick(PointerEventData eventData) => OnClick?.Invoke(eventData);
    }

    /// <summary>"&lt; option &gt;" cycling selector (simpler than a Dropdown built from code).</summary>
    public sealed class Selector
    {
        readonly Text _label;
        List<string> _options = new List<string>();

        public int Index { get; private set; }
        public string Value => Index >= 0 && Index < _options.Count ? _options[Index] : null;
        public event Action<string> Changed;

        public Selector(Transform parent, float x, float y, float w, float h, IEnumerable<string> options, string initial = null, int fontSize = 16)
        {
            var bg = UI.Panel(parent, Color.white, x, y, w, h, "Selector");
            UI.Button(bg.transform, "<", 0, 0, h, h, () => Step(-1), UI.ButtonGray, fontSize);
            UI.Button(bg.transform, ">", w - h, 0, h, h, () => Step(1), UI.ButtonGray, fontSize);
            _label = UI.Label(bg.transform, "", fontSize, Color.black, h, 0, w - 2 * h, h, TextAnchor.MiddleCenter, FontStyle.Bold, bestFit: true);
            SetOptions(options, initial);
        }

        public void SetOptions(IEnumerable<string> options, string select = null)
        {
            _options = new List<string>(options);
            int i = select != null ? _options.IndexOf(select) : -1;
            Index = i >= 0 ? i : (_options.Count > 0 ? 0 : -1);
            _label.text = Value ?? "(none)";
        }

        public void Select(string value)
        {
            int i = _options.IndexOf(value);
            if (i < 0) return;
            Index = i;
            _label.text = Value;
            Changed?.Invoke(Value);
        }

        void Step(int d)
        {
            if (_options.Count == 0) return;
            Index = (Index + d + _options.Count) % _options.Count;
            _label.text = Value;
            Changed?.Invoke(Value);
        }
    }

    /// <summary>A vertical scrolling list of rows.</summary>
    public sealed class ScrollList
    {
        public RectTransform Content { get; }
        public ScrollRect Scroll { get; }

        public ScrollList(Transform parent, float x, float y, float w, float h, Color background)
        {
            var bg = UI.Panel(parent, background, x, y, w, h, "ScrollList");
            Scroll = bg.gameObject.AddComponent<ScrollRect>();
            Scroll.horizontal = false;
            Scroll.scrollSensitivity = 30;
            Scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = UI.Stretch(UI.Rect("Viewport", bg.transform), 2);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0);

            Content = UI.Rect("Content", viewport);
            Content.anchorMin = new Vector2(0, 1);
            Content.anchorMax = new Vector2(1, 1);
            Content.pivot = new Vector2(0.5f, 1);
            Content.offsetMin = Content.offsetMax = Vector2.zero;
            var layout = Content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 1;
            var fitter = Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scroll.viewport = viewport;
            Scroll.content = Content;
        }

        public void Clear() => UI.Clear(Content);

        /// <summary>Adds a clickable row; columns are (text, width) pairs laid out left to right.</summary>
        public Image AddRow(float height, Color color, Action onClick, params (string text, float width)[] columns)
        {
            var row = UI.Rect("Row", Content);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            var img = row.gameObject.AddComponent<Image>();
            img.color = color;
            if (onClick != null)
            {
                var b = row.gameObject.AddComponent<Button>();
                b.targetGraphic = img;
                b.onClick.AddListener(() => onClick());
            }
            float x = 6;
            foreach (var (text, width) in columns)
            {
                var t = UI.Label(row, text, 15, Color.black, x, 0, width - 8, height, TextAnchor.MiddleLeft, FontStyle.Normal);
                t.rectTransform.anchorMin = new Vector2(0, 0);
                t.rectTransform.anchorMax = new Vector2(0, 1);
                t.rectTransform.pivot = new Vector2(0, 0.5f);
                t.rectTransform.anchoredPosition = new Vector2(x, 0);
                t.rectTransform.sizeDelta = new Vector2(width - 8, 0);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                x += width;
            }
            return img;
        }
    }
}
