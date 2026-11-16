using System;
using System.Collections.Generic;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Frame-by-frame effect clips (Resources/BrawlTCG/FX/*.png, packed left-to-right, top-to-bottom) and small
    /// procedural effects. Every position is in board design space: x right, y down from the top-left corner.
    /// </summary>
    public static class Fx
    {
        readonly struct Clip
        {
            public readonly int Cols, Count;
            public readonly float Aspect;

            public Clip(int cols, int count, int w, int h)
            {
                Cols = cols;
                Count = count;
                Aspect = (float)w / h;
            }
        }

        static readonly Dictionary<string, Clip> Clips = new Dictionary<string, Clip>
        {
            ["HitFire"] = new Clip(4, 12, 172, 200),
            ["HitMagic"] = new Clip(5, 22, 256, 212),
            ["HitNature"] = new Clip(3, 8, 256, 240),
            ["HitCosmic"] = new Clip(4, 11, 252, 236),
            ["HitArctic"] = new Clip(4, 11, 256, 212),
            ["HitWild"] = new Clip(4, 13, 236, 256),
            ["HitShadow"] = new Clip(4, 11, 256, 248),
            ["HitBig"] = new Clip(4, 10, 244, 256),
            ["Slash"] = new Clip(3, 7, 208, 256),
            ["Clash"] = new Clip(4, 11, 256, 240),
            ["KoMagic"] = new Clip(7, 34, 228, 320),
            ["KoFire"] = new Clip(7, 36, 284, 320),
            ["KoArctic"] = new Clip(6, 35, 320, 232),
            ["KoShadow"] = new Clip(6, 38, 320, 284),
            ["KoCosmic"] = new Clip(5, 33, 320, 240),
            ["KoNature"] = new Clip(6, 33, 320, 252),
            ["KoWild"] = new Clip(6, 37, 320, 244),
            ["Explosion"] = new Clip(5, 19, 320, 284),
            ["Sparkle"] = new Clip(5, 18, 136, 144),
            ["Ring"] = new Clip(4, 12, 256, 196),
        };

        static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

        // No domain reload on Play: frame sprites from the previous run are destroyed, so cut them again.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Cache.Clear();

        public static Sprite[] Frames(string name)
        {
            if (Cache.TryGetValue(name, out Sprite[] frames)) return frames;
            frames = Array.Empty<Sprite>();
            Texture2D tex = Resources.Load<Texture2D>("BrawlTCG/FX/" + name);
            if (tex != null && Clips.TryGetValue(name, out Clip clip))
            {
                int rows = (clip.Count + clip.Cols - 1) / clip.Cols;
                float cw = (float)tex.width / clip.Cols, ch = (float)tex.height / rows;
                frames = new Sprite[clip.Count];
                for (int i = 0; i < clip.Count; i++)
                {
                    int col = i % clip.Cols, row = i / clip.Cols;
                    var rect = new Rect(col * cw, tex.height - (row + 1) * ch, cw, ch);
                    frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
                }
            }
            else
            {
                Debug.LogWarning($"[BrawlTCG] Missing effect '{name}'");
            }
            Cache[name] = frames;
            return frames;
        }

        public static float Aspect(string name) => Clips.TryGetValue(name, out Clip c) ? c.Aspect : 1;

        public static string HitClip(Element e) => "Hit" + e;
        public static string KoClip(Element e) => "Ko" + e;

        /// <summary>Bright colour used for numbers, sparks and bolts of an element.</summary>
        public static Color Tint(Element e) => Color.Lerp(CardStyle.FrameColor(e), Color.white, 0.3f);

        // ───────────── Spawners ─────────────

        /// <summary>Plays a clip centred on <paramref name="center"/>, <paramref name="height"/> units tall.</summary>
        public static FxSprite Play(Transform layer, string clip, Vector2 center, float height, float delay = 0, float fps = 30,
            Color? tint = null, float rotation = 0, bool mirror = false)
        {
            Sprite[] frames = Frames(clip);
            if (frames.Length == 0) return null;
            float w = height * Aspect(clip);
            var rt = UI.Rect("Fx " + clip, layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(center.x, -center.y);
            rt.sizeDelta = new Vector2(w, height);
            rt.localRotation = Quaternion.Euler(0, 0, rotation);
            rt.localScale = new Vector3(mirror ? -1 : 1, 1, 1);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = tint ?? Color.white;
            var fx = rt.gameObject.AddComponent<FxSprite>();
            fx.Init(img, frames, fps, delay);
            return fx;
        }

        /// <summary>A rising, fading number or word ("-3", "+2 ATK", "K.O.").</summary>
        public static void Float(Transform layer, string text, Vector2 center, Color color, int size = 56, float delay = 0, float rise = 90)
        {
            var rt = UI.Rect("Float " + text, layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(center.x, -center.y);
            rt.sizeDelta = new Vector2(420, size * 1.6f);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = Theme.Display;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = text;
            label.color = color;
            label.raycastTarget = false;
            Theme.AddOutline(label, 3, 1f).effectColor = new Color(0.03f, 0.02f, 0.08f, 1);
            Theme.AddShadow(label, 5, 0.7f);
            rt.gameObject.AddComponent<FloatingText>().Init(label, delay, rise);
        }

        /// <summary>Full-screen colour flash.</summary>
        public static void Flash(Transform layer, Color color, float duration = 0.35f, float delay = 0)
        {
            var img = UI.FullPanel(layer, color, "Flash");
            img.raycastTarget = false;
            img.gameObject.AddComponent<Fade>().Init(img, color.a, duration, delay);
        }

        /// <summary>An expanding, fading ring.</summary>
        public static void Shockwave(Transform layer, Vector2 center, Color color, float size, float delay = 0, float duration = 0.5f)
        {
            var rt = UI.Rect("Shockwave", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(center.x, -center.y);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Ring;
            img.color = color;
            img.raycastTarget = false;
            rt.gameObject.AddComponent<Expand>().Init(img, color, 0.15f, 1f, duration, delay);
        }

        /// <summary>A soft glow that blooms and fades.</summary>
        public static void Glow(Transform layer, Vector2 center, Color color, float size, float delay = 0, float duration = 0.5f)
        {
            var rt = UI.Rect("Glow", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(center.x, -center.y);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Soft;
            img.color = color;
            img.raycastTarget = false;
            rt.gameObject.AddComponent<Expand>().Init(img, color, 0.5f, 1.2f, duration, delay);
        }

        /// <summary>Glowing sparks thrown outwards that fall with gravity.</summary>
        public static void Sparks(Transform layer, Vector2 center, Color color, int count, float speed = 520, float delay = 0, float gravity = 900)
        {
            var root = UI.Rect("Sparks", layer);
            UI.Stretch(root);
            root.gameObject.AddComponent<SparkBurst>().Init(center, color, count, speed, delay, gravity);
        }

        /// <summary>A jagged, flickering lightning bolt between two points.</summary>
        public static void Bolt(Transform layer, Vector2 from, Vector2 to, Color color, float delay = 0, float duration = 0.35f, float width = 7)
        {
            var root = UI.Rect("Bolt", layer);
            UI.Stretch(root);
            root.gameObject.AddComponent<LightningBolt>().Init(from, to, color, delay, duration, width);
        }

        /// <summary>A sweeping banner across the middle of the board ("YOUR TURN").</summary>
        public static void Banner(Transform layer, string title, string subtitle, Color color, float delay = 0)
        {
            foreach (TurnBanner old in layer.GetComponentsInChildren<TurnBanner>())
                UnityEngine.Object.Destroy(old.gameObject);
            var root = UI.Rect("Banner", layer);
            UI.Place(root, 0, 430, 1920, 220);
            root.gameObject.AddComponent<TurnBanner>().Init(title, subtitle, color, delay);
        }
    }

    /// <summary>Plays sprite frames once, then removes itself.</summary>
    public sealed class FxSprite : MonoBehaviour
    {
        Image _img;
        Sprite[] _frames;
        float _fps, _delay, _t;

        public void Init(Image img, Sprite[] frames, float fps, float delay)
        {
            _img = img;
            _frames = frames;
            _fps = fps;
            _delay = delay;
            _img.sprite = frames[0];
            _img.enabled = delay <= 0;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0)
            {
                _delay -= dt;
                if (_delay > 0) return;
                _img.enabled = true;
            }
            _t += dt;
            int frame = (int)(_t * _fps);
            if (frame >= _frames.Length)
            {
                Destroy(gameObject);
                return;
            }
            _img.sprite = _frames[frame];
        }
    }

    public sealed class FloatingText : MonoBehaviour
    {
        Text _label;
        RectTransform _rt;
        Vector2 _base;
        float _delay, _rise, _t;
        const float Life = 1.25f;

        public void Init(Text label, float delay, float rise)
        {
            _label = label;
            _rt = (RectTransform)transform;
            _base = _rt.anchoredPosition;
            _delay = delay;
            _rise = rise;
            _rt.localScale = Vector3.zero;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0) { _delay -= dt; return; }
            _t += dt;
            float k = _t / Life;
            if (k >= 1) { Destroy(gameObject); return; }
            // Punch in, settle, drift up, fade.
            float pop = _t < 0.12f ? Mathf.Lerp(0.3f, 1.35f, _t / 0.12f) : Mathf.Lerp(1.35f, 1f, Mathf.Clamp01((_t - 0.12f) / 0.15f));
            _rt.localScale = new Vector3(pop, pop, 1);
            _rt.anchoredPosition = _base + new Vector2(0, _rise * Theme.EaseOut(k));
            Color c = _label.color;
            c.a = k < 0.65f ? 1 : 1 - (k - 0.65f) / 0.35f;
            _label.color = c;
        }
    }

    public sealed class Fade : MonoBehaviour
    {
        Graphic _g;
        float _from, _duration, _delay, _t;

        public void Init(Graphic g, float from, float duration, float delay)
        {
            _g = g;
            _from = from;
            _duration = duration;
            _delay = delay;
            _g.enabled = delay <= 0;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0)
            {
                _delay -= dt;
                if (_delay > 0) return;
                _g.enabled = true;
            }
            _t += dt / _duration;
            if (_t >= 1) { Destroy(gameObject); return; }
            Color c = _g.color;
            c.a = _from * (1 - _t) * (1 - _t);
            _g.color = c;
        }
    }

    /// <summary>Scales a graphic up while fading it out.</summary>
    public sealed class Expand : MonoBehaviour
    {
        Graphic _g;
        Color _color;
        float _from, _to, _duration, _delay, _t;

        public void Init(Graphic g, Color color, float from, float to, float duration, float delay)
        {
            _g = g;
            _color = color;
            _from = from;
            _to = to;
            _duration = duration;
            _delay = delay;
            _g.enabled = delay <= 0;
            transform.localScale = new Vector3(from, from, 1);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0)
            {
                _delay -= dt;
                if (_delay > 0) return;
                _g.enabled = true;
            }
            _t += dt / _duration;
            if (_t >= 1) { Destroy(gameObject); return; }
            float s = Mathf.Lerp(_from, _to, Theme.EaseOut(_t));
            transform.localScale = new Vector3(s, s, 1);
            _g.color = new Color(_color.r, _color.g, _color.b, _color.a * (1 - _t));
        }
    }

    public sealed class SparkBurst : MonoBehaviour
    {
        struct Spark
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Pos, Vel;
            public float Life, Size;
        }

        readonly List<Spark> _sparks = new List<Spark>();
        float _delay, _gravity, _t;
        bool _visible;

        public void Init(Vector2 center, Color color, int count, float speed, float delay, float gravity)
        {
            _delay = delay;
            _gravity = gravity;
            for (int i = 0; i < count; i++)
            {
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2);
                float v = speed * UnityEngine.Random.Range(0.35f, 1f);
                float size = UnityEngine.Random.Range(8f, 22f);
                var img = UI.Panel(transform, Color.Lerp(color, Color.white, UnityEngine.Random.Range(0f, 0.6f)), 0, 0, size, size, "Spark");
                img.sprite = Theme.Soft;
                img.raycastTarget = false;
                img.enabled = false;
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _sparks.Add(new Spark
                {
                    Rect = img.rectTransform,
                    Image = img,
                    Pos = center,
                    Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v + new Vector2(0, -speed * 0.3f),
                    Life = UnityEngine.Random.Range(0.45f, 0.9f),
                    Size = size,
                });
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0) { _delay -= dt; return; }
            if (!_visible)
            {
                _visible = true;
                foreach (Spark s in _sparks) s.Image.enabled = true;
            }
            _t += dt;
            bool alive = false;
            for (int i = 0; i < _sparks.Count; i++)
            {
                Spark s = _sparks[i];
                s.Vel = new Vector2(s.Vel.x * (1 - 2.2f * dt), s.Vel.y + _gravity * dt);
                s.Pos += s.Vel * dt;
                _sparks[i] = s;
                float k = _t / s.Life;
                if (k >= 1) { s.Image.enabled = false; continue; }
                alive = true;
                s.Rect.anchoredPosition = new Vector2(s.Pos.x, -s.Pos.y);
                float scale = 1 - k;
                // Stretch along the velocity for a streak.
                float speed = s.Vel.magnitude;
                s.Rect.sizeDelta = new Vector2(s.Size * scale * (1 + speed / 400f), s.Size * scale);
                s.Rect.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(s.Vel.y, s.Vel.x) * Mathf.Rad2Deg);
            }
            if (!alive) Destroy(gameObject);
        }
    }

    /// <summary>Procedural lightning: a glow layer and a white core, re-jagged every few frames.</summary>
    public sealed class LightningBolt : MonoBehaviour
    {
        const int Segments = 14;
        readonly List<Image> _glow = new List<Image>(), _core = new List<Image>(), _branch = new List<Image>();
        Vector2 _from, _to;
        Color _color;
        float _delay, _duration, _width, _t, _jitterTimer;

        public void Init(Vector2 from, Vector2 to, Color color, float delay, float duration, float width)
        {
            _from = from;
            _to = to;
            _color = color;
            _delay = delay;
            _duration = duration;
            _width = width;
            for (int i = 0; i < Segments; i++)
            {
                _glow.Add(Segment(new Color(color.r, color.g, color.b, 0.55f), Theme.Soft));
                _core.Add(Segment(Color.white, null));
            }
            for (int i = 0; i < 6; i++) _branch.Add(Segment(new Color(color.r, color.g, color.b, 0.9f), null));
            SetVisible(false);
        }

        Image Segment(Color c, Sprite sprite)
        {
            var img = UI.Panel(transform, c, 0, 0, 1, 1, "Seg");
            img.sprite = sprite;
            img.raycastTarget = false;
            img.rectTransform.pivot = new Vector2(0, 0.5f);
            return img;
        }

        void SetVisible(bool on)
        {
            foreach (Image i in _glow) i.enabled = on;
            foreach (Image i in _core) i.enabled = on;
            foreach (Image i in _branch) i.enabled = on;
        }

        static void Place(Image img, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 d = b - a;
            var rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(a.x, -a.y);
            rt.sizeDelta = new Vector2(d.magnitude + thickness * 0.5f, thickness);
            rt.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void Rebuild()
        {
            Vector2 dir = _to - _from;
            Vector2 normal = new Vector2(-dir.y, dir.x).normalized;
            float amp = Mathf.Min(70, dir.magnitude * 0.12f);
            var pts = new Vector2[Segments + 1];
            for (int i = 0; i <= Segments; i++)
            {
                float k = (float)i / Segments;
                float taper = Mathf.Sin(k * Mathf.PI);
                pts[i] = _from + dir * k + normal * UnityEngine.Random.Range(-amp, amp) * taper;
            }
            for (int i = 0; i < Segments; i++)
            {
                Place(_glow[i], pts[i], pts[i + 1], _width * 6);
                Place(_core[i], pts[i], pts[i + 1], _width);
            }
            for (int i = 0; i < _branch.Count; i++)
            {
                Vector2 a = pts[UnityEngine.Random.Range(2, Segments - 1)];
                Vector2 b = a + (dir.normalized * UnityEngine.Random.Range(20f, 60f) + normal * UnityEngine.Random.Range(-80f, 80f));
                Place(_branch[i], a, b, _width * 0.45f);
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0)
            {
                _delay -= dt;
                if (_delay > 0) return;
                SetVisible(true);
                Rebuild();
            }
            _t += dt;
            if (_t >= _duration) { Destroy(gameObject); return; }
            _jitterTimer -= dt;
            if (_jitterTimer <= 0)
            {
                _jitterTimer = 0.045f;
                Rebuild();
            }
            float a = _t < _duration * 0.6f ? 1 : 1 - (_t - _duration * 0.6f) / (_duration * 0.4f);
            float flicker = UnityEngine.Random.Range(0.75f, 1f);
            foreach (Image i in _glow) i.color = new Color(_color.r, _color.g, _color.b, 0.55f * a * flicker);
            foreach (Image i in _core) i.color = new Color(1, 1, 1, a);
            foreach (Image i in _branch) i.color = new Color(_color.r, _color.g, _color.b, 0.9f * a * flicker);
        }
    }

    /// <summary>A calm banner: a band wipes in from the left, the title fades in with a slow drift, then it wipes away.</summary>
    public sealed class TurnBanner : MonoBehaviour
    {
        RectTransform _wipe, _content, _edge;
        CanvasGroup _group, _titleGroup;
        RectTransform _title;
        float _delay, _t;
        const float Life = 2.1f, In = 0.55f, Out = 0.5f, W = 1920, H = 220;

        public void Init(string title, string subtitle, Color color, float delay)
        {
            _delay = delay;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0;

            // The wipe is a clipping window that grows from the left and later shrinks to the right;
            // the content inside is counter-moved so it stays put while the window travels.
            _wipe = UI.Place(UI.Rect("Wipe", transform), 0, 0, 0, H);
            _wipe.gameObject.AddComponent<RectMask2D>();
            _content = UI.Place(UI.Rect("Content", _wipe), 0, 0, W, H);

            var band = UI.Panel(_content, new Color(0.02f, 0.03f, 0.1f, 0.94f), 0, 40, W, 140, "Band");
            band.raycastTarget = false;
            foreach (float y in new[] { 0f, 138f })
            {
                var edge = UI.Panel(band.transform, color, 0, y, W / 2, 2, "Line");
                edge.sprite = Theme.Gradient(true, 0f, 0.8f);
                edge.raycastTarget = false;
                var edge2 = UI.Panel(band.transform, color, W / 2, y, W / 2, 2, "Line");
                edge2.sprite = Theme.Gradient(true, 0.8f, 0f);
                edge2.raycastTarget = false;
            }

            var titleRt = UI.Place(UI.Rect("Title", _content), 0, 44, W, 100);
            _titleGroup = titleRt.gameObject.AddComponent<CanvasGroup>();
            var t = Theme.Label(titleRt, title, 64, color, 0, 0, W, 100, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(t, 3, 0.6f);
            _title = titleRt;
            if (!string.IsNullOrEmpty(subtitle))
                Theme.Label(titleRt, subtitle, 20, Theme.TextDim, 0, 92, W, 32, TextAnchor.MiddleCenter, display: true);

            // A soft light on the leading edge of the wipe.
            var glow = UI.Panel(transform, new Color(color.r, color.g, color.b, 0.35f), 0, 40, 90, 140, "Edge");
            glow.sprite = Theme.Soft;
            glow.raycastTarget = false;
            _edge = glow.rectTransform;
        }

        static float Smooth(float k) { k = Mathf.Clamp01(k); return k * k * (3 - 2 * k); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0) { _delay -= dt; return; }
            _t += dt;
            if (_t >= Life) { Destroy(gameObject); return; }
            _group.alpha = 1;

            float left = 0, right;
            if (_t < In) right = W * Smooth(_t / In);
            else
            {
                right = W;
                float k = Smooth((_t - (Life - Out)) / Out);
                left = W * k;
            }
            _wipe.anchoredPosition = new Vector2(left, 0);
            _wipe.sizeDelta = new Vector2(Mathf.Max(0, right - left), H);
            _content.anchoredPosition = new Vector2(-left, 0);

            bool moving = _t < In || _t > Life - Out;
            float edgeX = _t < In ? right : left;
            _edge.anchoredPosition = new Vector2(edgeX - 45, -40);
            _edge.gameObject.SetActive(moving && edgeX > 0 && edgeX < W);

            _titleGroup.alpha = Smooth((_t - 0.2f) / 0.5f);
            _title.anchoredPosition = new Vector2(-24 + 48 * (_t / Life), -44);
        }
    }

    /// <summary>
    /// Moves a card through a list of legs (centre position, scale), optionally with an arc and a flip, then lands.
    /// Works in board design space; the card keeps its top-left pivot so the centre is converted back.
    /// </summary>
    public sealed class CardFlight : MonoBehaviour
    {
        struct Leg
        {
            public Vector2 Center;
            public float Scale, Duration, Hold, Arc;
        }

        readonly List<Leg> _legs = new List<Leg>();
        RectTransform _rt;
        CanvasGroup _group;
        Vector2 _c0;
        float _s0, _delay, _t, _hold;
        int _leg;
        bool _flip, _fadeOut, _destroyAtEnd, _started, _noRaise;
        public Action Landed;

        public bool Running => _leg < _legs.Count;

        public static CardFlight Begin(CardView view, Vector2 center, float scale, float delay = 0)
        {
            var old = view.GetComponent<CardFlight>();
            if (old != null) DestroyImmediate(old);
            var f = view.gameObject.AddComponent<CardFlight>();
            f._rt = view.Rect;
            f._group = view.GetComponent<CanvasGroup>();
            f._c0 = center;
            f._s0 = scale;
            f._delay = delay;
            f.Apply(center, scale, 1, 0);
            return f;
        }

        public CardFlight To(Vector2 center, float scale, float duration, float hold = 0, float arc = 0)
        {
            _legs.Add(new Leg { Center = center, Scale = scale, Duration = Mathf.Max(0.01f, duration), Hold = hold, Arc = arc });
            return this;
        }

        public CardFlight Flip() { _flip = true; return this; }
        public CardFlight FadeOutAndDestroy() { _fadeOut = true; _destroyAtEnd = true; return this; }
        public CardFlight DestroyAtEnd() { _destroyAtEnd = true; return this; }
        public CardFlight NoRaise() { _noRaise = true; return this; }

        void Apply(Vector2 center, float scale, float scaleX, float lift)
        {
            float s = scale * (1 + lift);
            _rt.localScale = new Vector3(s * scaleX, s, 1);
            Vector2 topLeft = center - new Vector2(CardView.W * s * scaleX, CardView.H * s) / 2;
            _rt.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0) { _delay -= dt; return; }
            if (!_started)
            {
                _started = true;
                if (!_noRaise) transform.SetAsLastSibling();
            }
            if (_leg >= _legs.Count) { Finish(); return; }
            Leg leg = _legs[_leg];
            if (_t < 1)
            {
                _t = Mathf.Min(1, _t + dt / leg.Duration);
                float e = _t < 0.5f ? 4 * _t * _t * _t : 1 - Mathf.Pow(-2 * _t + 2, 3) / 2; // ease in-out cubic
                Vector2 d = leg.Center - _c0;
                Vector2 normal = d.sqrMagnitude > 1 ? new Vector2(-d.y, d.x).normalized : Vector2.zero;
                if (normal.y > 0) normal = -normal; // arc upwards on screen
                Vector2 c = Vector2.Lerp(_c0, leg.Center, e) + normal * leg.Arc * Mathf.Sin(_t * Mathf.PI);
                float s = Mathf.Lerp(_s0, leg.Scale, e);
                float scaleX = _flip && _leg == 0 ? Mathf.Max(0.04f, Mathf.Abs(Mathf.Cos(_t * Mathf.PI))) : 1;
                Apply(c, s, scaleX, 0.12f * Mathf.Sin(_t * Mathf.PI));
                if (_fadeOut && _leg == _legs.Count - 1 && _group != null) _group.alpha = 1 - e * e;
                return;
            }
            if (_hold < leg.Hold) { _hold += dt; return; }
            _c0 = leg.Center;
            _s0 = leg.Scale;
            _t = 0;
            _hold = 0;
            _leg++;
            if (_leg >= _legs.Count) Finish();
        }

        /// <summary>Jumps to the end of the flight (e.g. when the player grabs the card mid-flight).</summary>
        public void Complete()
        {
            if (_legs.Count > 0)
            {
                Leg last = _legs[_legs.Count - 1];
                Apply(last.Center, last.Scale, 1, 0);
            }
            if (_group != null && _fadeOut) _group.alpha = 0;
            _leg = _legs.Count;
            Finish();
        }

        void Finish()
        {
            Landed?.Invoke();
            Landed = null;
            if (_destroyAtEnd) Destroy(gameObject);
            else Destroy(this);
        }
    }

    /// <summary>Wind up, strike towards a point, recoil back.</summary>
    public sealed class Lunge : MonoBehaviour
    {
        RectTransform _rt;
        Vector2 _base, _offset;
        float _impact, _t;

        public void Init(Vector2 designOffset, float impact)
        {
            _rt = (RectTransform)transform;
            _base = _rt.anchoredPosition;
            _offset = new Vector2(designOffset.x, -designOffset.y);
            _impact = impact;
            transform.SetAsLastSibling();
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float windup = _impact * 0.45f;
            float k;
            if (_t < windup) k = -0.08f * Theme.EaseOut(_t / windup);
            else if (_t < _impact) { float u = (_t - windup) / (_impact - windup); k = Mathf.Lerp(-0.08f, 0.62f, u * u); }
            else if (_t < _impact + 0.06f) k = 0.62f;
            else if (_t < _impact + 0.45f) k = Mathf.Lerp(0.62f, 0, Theme.EaseOut((_t - _impact - 0.06f) / 0.39f));
            else { _rt.anchoredPosition = _base; Destroy(this); return; }
            _rt.anchoredPosition = _base + _offset * k;
            float s = 1 + 0.1f * Mathf.Clamp01(k / 0.62f);
            transform.localScale = new Vector3(s, s, 1);
        }

        void OnDestroy()
        {
            if (_rt != null) transform.localScale = Vector3.one;
        }
    }

    /// <summary>Shakes a card's face and washes it in a colour (red for damage, green for healing).</summary>
    public sealed class CardHit : MonoBehaviour
    {
        RectTransform _face;
        Image _wash;
        Color _color;
        float _delay, _t, _strength;
        const float Life = 0.5f;

        public static void Apply(CardView view, Color color, float strength, float delay)
        {
            if (view == null) return;
            var face = view.transform.Find("Face") as RectTransform;
            if (face == null) return;
            var hit = view.gameObject.AddComponent<CardHit>();
            hit._face = face;
            hit._color = color;
            hit._strength = strength;
            hit._delay = delay;
            hit._wash = Theme.RoundedPanel(face, new Color(color.r, color.g, color.b, 0), 0, 0, 300, 400, 22, "Wash");
            hit._wash.raycastTarget = false;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0) { _delay -= dt; return; }
            _t += dt;
            float k = _t / Life;
            if (k >= 1)
            {
                _face.anchoredPosition = Vector2.zero;
                if (_wash != null) Destroy(_wash.gameObject);
                Destroy(this);
                return;
            }
            float amp = 22 * _strength * (1 - k) * (1 - k);
            _face.anchoredPosition = new Vector2(UnityEngine.Random.Range(-amp, amp), UnityEngine.Random.Range(-amp, amp) * 0.5f);
            _wash.color = new Color(_color.r, _color.g, _color.b, 0.6f * (1 - k));
        }
    }

    /// <summary>Breathing glow for cards that are ready to act.</summary>
    public sealed class GlowPulse : MonoBehaviour
    {
        Image _glow;
        float _base = -1, _phase;

        void Start()
        {
            var highlight = transform.Find("Highlight");
            if (highlight != null) _glow = highlight.GetComponent<Image>();
            _phase = UnityEngine.Random.Range(0f, 6f);
        }

        void Update()
        {
            if (_glow == null || !_glow.gameObject.activeInHierarchy) return;
            if (_base < 0) _base = _glow.color.a;
            Color c = _glow.color;
            c.a = _base * (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3.2f + _phase));
            _glow.color = c;
        }
    }

    /// <summary>Screen shake for the whole board ("trauma" model: offset grows with trauma squared).</summary>
    public sealed class Shaker : MonoBehaviour
    {
        RectTransform _rt;
        float _trauma;

        public void Add(float amount) => _trauma = Mathf.Min(1, _trauma + amount);

        void Awake() => _rt = (RectTransform)transform;

        void LateUpdate()
        {
            if (_trauma <= 0) return;
            _trauma = Mathf.Max(0, _trauma - Time.unscaledDeltaTime * 1.6f);
            float shake = _trauma * _trauma;
            float t = Time.unscaledTime * 38;
            _rt.anchoredPosition = new Vector2(Mathf.PerlinNoise(t, 1.3f) * 2 - 1, Mathf.PerlinNoise(4.7f, t) * 2 - 1) * 34 * shake;
            _rt.localRotation = Quaternion.Euler(0, 0, (Mathf.PerlinNoise(t, 9.1f) * 2 - 1) * 1.4f * shake);
            if (_trauma <= 0)
            {
                _rt.anchoredPosition = Vector2.zero;
                _rt.localRotation = Quaternion.identity;
            }
        }
    }

    /// <summary>Dotted aiming line from the attacker to the pointer, with a spinning reticle.</summary>
    public sealed class TargetLine : MonoBehaviour
    {
        const int Dots = 22;
        readonly List<Image> _dots = new List<Image>();
        RectTransform _reticle, _layer;
        Func<Vector2> _from;
        Color _color;

        public void Init(RectTransform layer, Func<Vector2> from, Color color)
        {
            _layer = layer;
            _from = from;
            _color = color;
            for (int i = 0; i < Dots; i++)
            {
                var d = UI.Panel(transform, color, 0, 0, 14, 14, "Dot");
                d.sprite = Theme.Soft;
                d.raycastTarget = false;
                d.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _dots.Add(d);
            }
            var ret = UI.Panel(transform, color, 0, 0, 70, 70, "Reticle");
            ret.sprite = Theme.Ring;
            ret.raycastTarget = false;
            _reticle = ret.rectTransform;
            _reticle.pivot = new Vector2(0.5f, 0.5f);
        }

        void LateUpdate()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || _from == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, mouse.position.ReadValue(), null, out Vector2 local);
            Rect r = _layer.rect;
            Vector2 to = new Vector2(local.x - r.xMin, r.yMax - local.y);
            Vector2 from = _from();
            Vector2 d = to - from;
            Vector2 normal = new Vector2(-d.y, d.x).normalized;
            float bend = Mathf.Min(120, d.magnitude * 0.25f);
            float flow = Time.unscaledTime * 1.6f % 1f;
            for (int i = 0; i < Dots; i++)
            {
                float k = (i + flow) / Dots;
                Vector2 p = Vector2.Lerp(from, to, k) + normal * bend * Mathf.Sin(k * Mathf.PI) * (normal.y > 0 ? -1 : 1);
                var rt = _dots[i].rectTransform;
                rt.anchoredPosition = new Vector2(p.x, -p.y);
                float size = Mathf.Lerp(8, 22, k);
                rt.sizeDelta = new Vector2(size, size);
                _dots[i].color = new Color(_color.r, _color.g, _color.b, Mathf.Lerp(0.25f, 0.95f, k));
            }
            _reticle.anchoredPosition = new Vector2(to.x, -to.y);
            float pulse = 1 + 0.12f * Mathf.Sin(Time.unscaledTime * 8);
            _reticle.localScale = new Vector3(pulse, pulse, 1);
            _reticle.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 120);
        }
    }
}
