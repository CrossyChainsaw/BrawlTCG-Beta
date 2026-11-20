using System.Collections.Generic;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>Starts the game in whatever scene is loaded: no scene setup is required.</summary>
    static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindAnyObjectByType<App>() != null) return;
            var go = new GameObject("BrawlTCG");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<App>();
        }
    }

    public sealed class App : MonoBehaviour
    {
        public const float Width = 1920, Height = 1080;

        Canvas _canvas;
        RectTransform _screenRoot;
        RectTransform _toastLayer;
        RectTransform _backdrop;
        Image _background;
        GameObject _current;
        readonly List<(GameObject go, float until)> _toasts = new List<(GameObject, float)>();

        public static App Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            Application.runInBackground = true; // keep the network game alive when the window loses focus
            Application.targetFrameRate = 60;
            Protocol.GameVersion = Application.version;
            WindowTitle.Apply();

            EnsureCameraAndEventSystem();
            BuildCanvas();
            Collection.Initialize();
            ShowMenu();
            ShowcaseDirector.StartRequestedFromEditor(this);
        }

        static void EnsureCameraAndEventSystem()
        {
            if (Camera.main == null && Object.FindAnyObjectByType<Camera>() == null)
            {
                var cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                Object.DontDestroyOnLoad(cam.gameObject);
            }
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
                Object.DontDestroyOnLoad(es);
            }
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // the 1920x1080 design area always fits
            canvasGo.AddComponent<GraphicRaycaster>();

            _background = UI.FullPanel(canvasGo.transform, Color.black, "Background");
            _backdrop = UI.Stretch(UI.Rect("Backdrop", canvasGo.transform));

            _screenRoot = UI.Rect("Screen", canvasGo.transform);
            _screenRoot.anchorMin = _screenRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _screenRoot.pivot = new Vector2(0.5f, 0.5f);
            _screenRoot.sizeDelta = new Vector2(Width, Height);
            _screenRoot.anchoredPosition = Vector2.zero;

            _toastLayer = UI.Stretch(UI.Rect("Toasts", canvasGo.transform));
        }

        T Show<T>(string name, Color background) where T : Component
        {
            if (_current != null) Destroy(_current);
            UI.Clear(_backdrop);
            ClearToasts();
            _background.color = background;
            var rt = UI.Stretch(UI.Rect(name, _screenRoot));
            _current = rt.gameObject;
            return _current.AddComponent<T>();
        }

        /// <summary>Full-canvas layer behind the 1920x1080 screen area (for backgrounds on any aspect ratio). Cleared on screen change.</summary>
        public RectTransform Backdrop => _backdrop;

        public void ShowMenu()
        {
            Show<MenuScreen>("Menu", UI.MenuPink).Build(this);
        }

        public void ShowDeckBuilder()
        {
            Show<DeckBuilderScreen>("DeckBuilder", new Color32(240, 240, 240, 255)).Build(this);
        }

        public void StartShowcase(ShowcaseClip clip)
        {
            Game game = clip.CreateGame();
            StartMatch(new MatchSetup { Game = game, Net = null, LocalIndex = clip.Hero, Showcase = clip });
        }

        public void StartDeckShowcase(DeckClip clip)
        {
            Show<DeckBuilderScreen>("DeckBuilder", new Color32(240, 240, 240, 255)).BuildShowcase(this, clip);
        }

        public void StartMatch(MatchSetup setup)
        {
            Show<MatchScreen>("Match", UI.GameBackground).Build(this, setup);
        }

        // ───────────── Toasts (replace WinForms MessageBox pop-ups) ─────────────

        public void Toast(string message, float seconds = 3f)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (_toasts.Count >= 5)
            {
                Destroy(_toasts[0].go);
                _toasts.RemoveAt(0);
            }
            var panel = Theme.RoundedPanel(_toastLayer, new Color32(10, 14, 34, 235), 0, 0, 640, 52, 14, "Toast");
            panel.raycastTarget = false;
            var rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            var outline = Theme.RoundedPanel(panel.transform, new Color(1, 1, 1, 0.12f), 0, 0, 640, 52, 14, "Outline", outline: true);
            outline.raycastTarget = false;
            var accent = Theme.RoundedPanel(panel.transform, Theme.Gold, 10, 14, 5, 24, 3, "Accent");
            accent.raycastTarget = false;
            var t = UI.Label(panel.transform, message, 19, Theme.Text, 0, 0, 640, 52, TextAnchor.MiddleCenter, FontStyle.Bold, bestFit: true);
            UI.Stretch(t.rectTransform, 10);
            t.rectTransform.offsetMin = new Vector2(26, 8);
            _toasts.Add((panel.gameObject, Time.unscaledTime + seconds));
            LayoutToasts();
        }

        void LayoutToasts()
        {
            for (int i = 0; i < _toasts.Count; i++)
            {
                var rt = (RectTransform)_toasts[i].go.transform;
                rt.anchoredPosition = new Vector2(0, -28 - i * 60);
            }
        }

        void ClearToasts()
        {
            foreach (var (go, _) in _toasts) Destroy(go);
            _toasts.Clear();
        }

        void Update()
        {
            bool changed = false;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime >= _toasts[i].until)
                {
                    Destroy(_toasts[i].go);
                    _toasts.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed) LayoutToasts();
        }
    }
}
