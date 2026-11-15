using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>The polished look: palette, fonts, generated sprites and themed widgets.</summary>
    public static class Theme
    {
        // ───────────── Palette ─────────────
        public static readonly Color Night = new Color32(8, 11, 30, 255);
        public static readonly Color Glass = new Color32(12, 16, 38, 246);
        public static readonly Color GlassLight = new Color32(255, 255, 255, 14);
        public static readonly Color Line = new Color32(255, 255, 255, 28);
        public static readonly Color Gold = new Color32(245, 192, 74, 255);
        public static readonly Color GoldDeep = new Color32(214, 150, 40, 255);
        public static readonly Color Ink = new Color32(18, 14, 8, 255);
        public static readonly Color Text = new Color32(244, 246, 255, 255);
        public static readonly Color TextDim = new Color32(166, 176, 214, 255);
        public static readonly Color TextFaint = new Color32(166, 176, 214, 140);
        public static readonly Color Danger = new Color32(255, 110, 110, 255);
        public static readonly Color Success = new Color32(120, 230, 160, 255);

        // ───────────── Fonts ─────────────
        static Font _display;
        /// <summary>Program OT Bold (the Brawlhalla UI font from the original project), for headings and buttons.</summary>
        public static Font Display => _display ??= Resources.Load<Font>("BrawlTCG/Fonts/ProgramOTBold") ?? UI.Font;
        public static Font Body => UI.Font;

        // ───────────── Generated sprites ─────────────
        static Sprite _rounded, _roundedOutline, _soft, _ring;
        static readonly Dictionary<string, Sprite> Gradients = new Dictionary<string, Sprite>();

        /// <summary>9-sliced rounded rectangle with a 24px corner radius (scale with pixelsPerUnitMultiplier).</summary>
        public static Sprite Rounded => _rounded ??= MakeRounded(false);
        public static Sprite RoundedOutline => _roundedOutline ??= MakeRounded(true);
        /// <summary>Soft radial glow.</summary>
        public static Sprite Soft => _soft ??= MakeSoft();
        /// <summary>Thin circle with a gap, used as a loading spinner.</summary>
        public static Sprite Ring => _ring ??= MakeRing();

        static Sprite _heart;
        /// <summary>Anti-aliased heart shape (white, tint it).</summary>
        public static Sprite Heart => _heart ??= MakeHeart();

        static Sprite MakeHeart()
        {
            const int size = 128, ss = 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
            for (int pxl = 0; pxl < size; pxl++)
            {
                int inside = 0;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                {
                    float u = (pxl + (sx + 0.5f) / ss) / size, v = (py + (sy + 0.5f) / ss) / size;
                    float x = (u - 0.5f) * 2.45f, y = v * 2.45f - 1.12f;
                    float a = x * x + y * y - 1;
                    if (a * a * a - x * x * y * y * y <= 0) inside++;
                }
                px[py * size + pxl] = new Color32(255, 255, 255, (byte)(inside * 255 / (ss * ss)));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Outline AddOutline(Graphic g, float size = 2, float alpha = 0.85f)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, alpha);
            o.effectDistance = new Vector2(size, -size);
            return o;
        }

        const int Radius = 24;

        static Sprite MakeRounded(bool outline)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = RoundedDistance(x + 0.5f, y + 0.5f, size, Radius);
                float a = Mathf.Clamp01(0.5f - d);
                if (outline) a *= Mathf.Clamp01(d + 2.5f); // 2px ring
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(Radius, Radius, Radius, Radius));
        }

        static float RoundedDistance(float x, float y, int size, float r)
        {
            float cx = Mathf.Clamp(x, r, size - r);
            float cy = Mathf.Clamp(y, r, size - r);
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        static Sprite MakeSoft()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2 - 1, dy = (y + 0.5f) / size * 2 - 1;
                float t = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(t * t * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeRing()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - size / 2f, dy = y + 0.5f - size / 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1 - Mathf.Abs(r - 26) / 3f);
                float angle = (Mathf.Atan2(dy, dx) / Mathf.PI + 1) / 2; // 0..1
                a *= Mathf.Clamp01(angle * 1.3f);                      // fading tail
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Alpha ramp. horizontal: left→right; otherwise bottom→top. Alpha goes from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static Sprite Gradient(bool horizontal, float from, float to)
        {
            string key = $"{horizontal}{from}{to}";
            if (Gradients.TryGetValue(key, out Sprite s)) return s;
            const int n = 256;
            var tex = new Texture2D(horizontal ? n : 1, horizontal ? 1 : n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int i = 0; i < n; i++)
            {
                float t = i / (n - 1f);
                float a = Mathf.Lerp(from, to, t * t * (3 - 2 * t)); // smoothstep
                tex.SetPixel(horizontal ? i : 0, horizontal ? 0 : i, new Color(1, 1, 1, a));
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            Gradients[key] = s;
            return s;
        }

        // ───────────── Widgets ─────────────

        public static Image RoundedPanel(Transform parent, Color color, float x, float y, float w, float h, float radius = 18, string name = "Panel", bool outline = false)
        {
            var img = UI.Panel(parent, color, x, y, w, h, name);
            img.sprite = outline ? RoundedOutline : Rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = Radius / Mathf.Max(1f, radius);
            return img;
        }

        /// <summary>Glass card: translucent fill with a faint outline.</summary>
        public static Image GlassPanel(Transform parent, float x, float y, float w, float h, float radius = 22, string name = "Glass")
        {
            var fill = RoundedPanel(parent, Glass, x, y, w, h, radius, name);
            var line = RoundedPanel(fill.transform, Line, 0, 0, w, h, radius, "Outline", outline: true);
            line.raycastTarget = false;
            return fill;
        }

        public static Text Label(Transform parent, string text, int size, Color color, float x, float y, float w, float h,
            TextAnchor anchor = TextAnchor.UpperLeft, bool display = false, FontStyle style = FontStyle.Normal)
        {
            var t = UI.Label(parent, text, size, color, x, y, w, h, anchor, style);
            t.font = display ? Display : Body;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Shadow AddShadow(Graphic g, float distance = 3, float alpha = 0.6f)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, alpha);
            s.effectDistance = new Vector2(distance, -distance);
            return s;
        }

        /// <summary>Gold call-to-action button.</summary>
        public static Button Cta(Transform parent, string text, float x, float y, float w, float h, Action onClick, int fontSize = 28)
        {
            var bg = RoundedPanel(parent, Gold, x, y, w, h, 14, "CTA");
            var shine = UI.Panel(bg.transform, Color.white, 0, 0, w, h * 0.5f, "Shine");
            shine.sprite = Gradient(false, 0f, 0.22f);
            shine.raycastTarget = false;
            var b = bg.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => onClick?.Invoke());
            var label = Label(bg.transform, text, fontSize, Ink, 0, 0, w, h, TextAnchor.MiddleCenter, display: true);
            UI.Stretch(label.rectTransform);
            var hover = bg.gameObject.AddComponent<HoverFx>();
            hover.Init(bg, Gold, new Color32(255, 214, 120, 255), 1.025f);
            return b;
        }

        /// <summary>Outlined secondary button.</summary>
        public static Button Ghost(Transform parent, string text, float x, float y, float w, float h, Action onClick, int fontSize = 20)
        {
            var bg = RoundedPanel(parent, new Color(1, 1, 1, 0.06f), x, y, w, h, 12, "Ghost");
            var line = RoundedPanel(bg.transform, new Color(1, 1, 1, 0.22f), 0, 0, w, h, 12, "Outline", outline: true);
            line.raycastTarget = false;
            var b = bg.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => onClick?.Invoke());
            var label = Label(bg.transform, text, fontSize, Text, 0, 0, w, h, TextAnchor.MiddleCenter, display: true);
            UI.Stretch(label.rectTransform);
            bg.gameObject.AddComponent<HoverFx>().Init(bg, new Color(1, 1, 1, 0.06f), new Color(1, 1, 1, 0.16f), 1.02f);
            return b;
        }

        /// <summary>Labelled text field.</summary>
        public static InputField Field(Transform parent, string caption, string placeholder, string value, float x, float y, float w, int maxLength = 0)
        {
            if (!string.IsNullOrEmpty(caption))
                Label(parent, caption.ToUpperInvariant(), 14, TextDim, x + 2, y, w, 20, display: true);
            float fy = string.IsNullOrEmpty(caption) ? y : y + 24;
            var bg = RoundedPanel(parent, new Color(1, 1, 1, 0.07f), x, fy, w, 52, 12, "Field");
            var line = RoundedPanel(bg.transform, new Color(1, 1, 1, 0.16f), 0, 0, w, 52, 12, "Outline", outline: true);
            line.raycastTarget = false;
            var text = UI.Label(bg.transform, "", 20, Text, 0, 0, w, 52, TextAnchor.MiddleLeft, FontStyle.Normal);
            UI.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(18, 0);
            text.rectTransform.offsetMax = new Vector2(-18, 0);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ph = UI.Label(bg.transform, placeholder, 20, TextFaint, 0, 0, w, 52, TextAnchor.MiddleLeft, FontStyle.Italic);
            UI.Stretch(ph.rectTransform);
            ph.rectTransform.offsetMin = new Vector2(18, 0);
            ph.rectTransform.offsetMax = new Vector2(-18, 0);
            var input = bg.gameObject.AddComponent<InputField>();
            input.targetGraphic = bg;
            input.transition = Selectable.Transition.None;
            input.textComponent = text;
            input.placeholder = ph;
            input.caretColor = Gold;
            input.customCaretColor = true;
            input.selectionColor = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
            input.lineType = InputField.LineType.SingleLine;
            if (maxLength > 0) input.characterLimit = maxLength;
            input.text = value ?? "";
            bg.gameObject.AddComponent<FocusGlow>().Init(input, line);
            return input;
        }

        /// <summary>A 0..1 ease-out curve.</summary>
        public static float EaseOut(float t) => 1 - Mathf.Pow(1 - Mathf.Clamp01(t), 3);
    }

    /// <summary>Hover colour + scale animation.</summary>
    public sealed class HoverFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        Graphic _target;
        Color _normal, _hover;
        float _scale = 1.03f;
        bool _over, _down;
        float _t;

        public void Init(Graphic target, Color normal, Color hover, float scale)
        {
            _target = target;
            _normal = normal;
            _hover = hover;
            _scale = scale;
        }

        public void OnPointerEnter(PointerEventData e) => _over = true;
        public void OnPointerExit(PointerEventData e) { _over = false; _down = false; }
        public void OnPointerDown(PointerEventData e) => _down = true;
        public void OnPointerUp(PointerEventData e) => _down = false;

        void OnDisable() { _over = _down = false; _t = 0; Apply(); }

        void Update()
        {
            var sel = GetComponent<Selectable>();
            bool enabledButton = sel == null || sel.IsInteractable();
            float goal = _over && enabledButton ? 1 : 0;
            _t = Mathf.MoveTowards(_t, goal, Time.unscaledDeltaTime * 8);
            Apply();
            if (_target != null && !enabledButton) _target.color = new Color(_normal.r, _normal.g, _normal.b, _normal.a * 0.4f);
        }

        void Apply()
        {
            if (_target == null) return;
            _target.color = Color.Lerp(_normal, _hover, _t);
            float s = Mathf.Lerp(1, _scale, Theme.EaseOut(_t)) * (_down ? 0.98f : 1);
            transform.localScale = new Vector3(s, s, 1);
        }
    }

    /// <summary>Gold outline when an input field is focused.</summary>
    public sealed class FocusGlow : MonoBehaviour
    {
        InputField _input;
        Image _line;
        float _t;

        public void Init(InputField input, Image line)
        {
            _input = input;
            _line = line;
        }

        void Update()
        {
            _t = Mathf.MoveTowards(_t, _input.isFocused ? 1 : 0, Time.unscaledDeltaTime * 8);
            _line.color = Color.Lerp(new Color(1, 1, 1, 0.16f), Theme.Gold, _t);
        }
    }

    /// <summary>Slow cinematic zoom / drift on a full-screen picture.</summary>
    public sealed class KenBurns : MonoBehaviour
    {
        public float Amplitude = 0.04f;
        public float Speed = 0.045f;
        RectTransform _rt;
        Vector2 _base;

        void Awake()
        {
            _rt = (RectTransform)transform;
            _base = _rt.anchoredPosition;
        }

        void Update()
        {
            float t = Time.unscaledTime * Speed;
            float s = 1.08f + Amplitude * Mathf.Sin(t);
            _rt.localScale = new Vector3(s, s, 1);
            _rt.anchoredPosition = _base + new Vector2(Mathf.Sin(t * 0.7f) * 24, Mathf.Cos(t * 0.5f) * 12);
        }
    }

    /// <summary>Drifting, twinkling motes of light.</summary>
    public sealed class Particles : MonoBehaviour
    {
        struct Mote
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Phase, Alpha;
        }

        readonly List<Mote> _motes = new List<Mote>();
        Rect _area;

        public void Init(int count, Rect area, Color color)
        {
            _area = area;
            for (int i = 0; i < count; i++)
            {
                float size = UnityEngine.Random.Range(4f, 14f);
                var img = UI.Panel(transform, color, UnityEngine.Random.Range(area.xMin, area.xMax), UnityEngine.Random.Range(area.yMin, area.yMax), size, size, "Mote");
                img.sprite = Theme.Soft;
                img.raycastTarget = false;
                _motes.Add(new Mote
                {
                    Rect = img.rectTransform,
                    Image = img,
                    Velocity = new Vector2(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(10f, 34f)),
                    Phase = UnityEngine.Random.Range(0f, 10f),
                    Alpha = UnityEngine.Random.Range(0.25f, 0.85f),
                });
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;
            foreach (Mote m in _motes)
            {
                Vector2 p = m.Rect.anchoredPosition + m.Velocity * dt;
                if (-p.y < _area.yMin) p = new Vector2(UnityEngine.Random.Range(_area.xMin, _area.xMax), -_area.yMax);
                m.Rect.anchoredPosition = p;
                Color c = m.Image.color;
                c.a = m.Alpha * (0.55f + 0.45f * Mathf.Sin(t * 1.7f + m.Phase));
                m.Image.color = c;
            }
        }
    }

    public sealed class Spinner : MonoBehaviour
    {
        void Update() => transform.Rotate(0, 0, -360 * Time.unscaledDeltaTime);
    }

    /// <summary>Fades / slides a panel in when it is enabled.</summary>
    public sealed class SlideIn : MonoBehaviour
    {
        public Vector2 Offset = new Vector2(60, 0);
        public float Duration = 0.35f;
        CanvasGroup _group;
        RectTransform _rt;
        Vector2 _target;
        float _t;
        bool _ready;

        void Awake()
        {
            _rt = (RectTransform)transform;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _target = _rt.anchoredPosition;
            _ready = true;
        }

        void OnEnable()
        {
            if (!_ready) Awake();
            _t = 0;
            Step();
        }

        void Update()
        {
            if (_t >= 1) return;
            _t += Time.unscaledDeltaTime / Duration;
            Step();
        }

        void Step()
        {
            float e = Theme.EaseOut(_t);
            _group.alpha = e;
            _rt.anchoredPosition = _target + Offset * (1 - e);
        }
    }

    /// <summary>Gentle floating motion for showcase cards.</summary>
    public sealed class Bob : MonoBehaviour
    {
        public float Amplitude = 8, Speed = 1.2f, Phase, BaseRotation;
        RectTransform _rt;
        Vector2 _base;

        void Start()
        {
            _rt = (RectTransform)transform;
            _base = _rt.anchoredPosition;
        }

        void Update()
        {
            float t = Time.unscaledTime * Speed + Phase;
            _rt.anchoredPosition = _base + new Vector2(0, Mathf.Sin(t) * Amplitude);
            _rt.localRotation = Quaternion.Euler(0, 0, BaseRotation + Mathf.Sin(t * 0.8f) * 1.2f);
        }
    }

    /// <summary>"‹ value ›" selector in the polished style.</summary>
    public sealed class ThemedSelector
    {
        readonly Text _label;
        List<string> _options = new List<string>();

        public int Index { get; private set; }
        public string Value => Index >= 0 && Index < _options.Count ? _options[Index] : null;
        public event Action<string> Changed;

        public ThemedSelector(Transform parent, string caption, float x, float y, float w, IEnumerable<string> options, string initial = null)
        {
            if (!string.IsNullOrEmpty(caption))
                Theme.Label(parent, caption.ToUpperInvariant(), 14, Theme.TextDim, x + 2, y, w, 20, display: true);
            float fy = string.IsNullOrEmpty(caption) ? y : y + 24;
            var bg = Theme.RoundedPanel(parent, new Color(1, 1, 1, 0.07f), x, fy, w, 52, 12, "Selector");
            var line = Theme.RoundedPanel(bg.transform, new Color(1, 1, 1, 0.16f), 0, 0, w, 52, 12, "Outline", outline: true);
            line.raycastTarget = false;
            Arrow(bg.transform, "‹", 4, () => Step(-1));
            Arrow(bg.transform, "›", w - 52, () => Step(1));
            _label = Theme.Label(bg.transform, "", 22, Theme.Text, 52, 0, w - 104, 52, TextAnchor.MiddleCenter, display: true);
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 12;
            _label.resizeTextMaxSize = 22;
            SetOptions(options, initial);
        }

        static void Arrow(Transform parent, string glyph, float x, Action onClick)
        {
            var hit = UI.Panel(parent, new Color(1, 1, 1, 0), x, 4, 48, 44, "Arrow");
            hit.sprite = Theme.Rounded;
            hit.type = Image.Type.Sliced;
            hit.pixelsPerUnitMultiplier = 2;
            var b = hit.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => onClick());
            hit.gameObject.AddComponent<HoverFx>().Init(hit, new Color(1, 1, 1, 0), new Color(1, 1, 1, 0.12f), 1f);
            Theme.Label(hit.transform, glyph, 34, Theme.Gold, 0, -4, 48, 44, TextAnchor.MiddleCenter);
        }

        public void SetOptions(IEnumerable<string> options, string select = null)
        {
            _options = new List<string>(options);
            int i = select != null ? _options.IndexOf(select) : -1;
            Index = i >= 0 ? i : (_options.Count > 0 ? 0 : -1);
            _label.text = Value ?? "No decks yet";
        }

        void Step(int d)
        {
            if (_options.Count == 0) return;
            Index = (Index + d + _options.Count) % _options.Count;
            _label.text = Value;
            Changed?.Invoke(Value);
        }
    }
}
