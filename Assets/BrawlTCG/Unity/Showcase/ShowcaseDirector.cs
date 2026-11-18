using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Plays showcase items (match clips and deck-building clips) one after another. Started from the editor menu
    /// (BrawlTCG ▸ Showcase), or with F8 on the main menu in the editor and development builds. The editor recorder
    /// listens to <see cref="ClipStarted"/> / <see cref="ClipEnded"/> to cut one video per item.
    /// </summary>
    public static class ShowcaseDirector
    {
        /// <summary>SessionState key the editor menu sets before entering Play Mode: "all" or comma-separated ids.</summary>
        public const string RequestKey = "BrawlTCG.Showcase.Request";

        public static event Action<ShowcaseItem> ClipStarted;
        public static event Action<ShowcaseItem> ClipEnded;
        public static event Action AllDone;

        public static bool Running { get; private set; }

        /// <summary>Every item: the match clips, then the deck-building clips.</summary>
        public static IReadOnlyList<ShowcaseItem> All { get; } =
            ShowcaseClips.All.Cast<ShowcaseItem>().Concat(ShowcaseDecks.All).ToList();

        static App _app;
        static ShowcaseItem _current;
        static readonly Queue<ShowcaseItem> Queue = new Queue<ShowcaseItem>();

        /// <summary>The project enters Play Mode without a domain reload, so statics survive a stopped run: start clean.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Running = false;
            _app = null;
            _current = null;
            Queue.Clear();
        }

        /// <summary>"all", "matches", "decks" or comma-separated ids.</summary>
        public static IEnumerable<ShowcaseItem> Select(string ids)
        {
            switch (ids)
            {
                case null:
                case "":
                case "all": return All;
                case "matches": return ShowcaseClips.All;
                case "decks": return ShowcaseDecks.All;
            }
            string[] wanted = ids.Split(',');
            return All.Where(c => wanted.Contains(c.Id));
        }

        public static void StartRequestedFromEditor(App app)
        {
            app.gameObject.AddComponent<ShowcaseHotkey>();
#if UNITY_EDITOR
            string request = UnityEditor.SessionState.GetString(RequestKey, "");
            if (string.IsNullOrEmpty(request)) return;
            UnityEditor.SessionState.EraseString(RequestKey);
            Play(app, Select(request));
#endif
        }

        public static void Play(App app, IEnumerable<ShowcaseItem> items)
        {
            _app = app;
            Queue.Clear();
            foreach (ShowcaseItem item in items) Queue.Enqueue(item);
            Running = true;
            app.StartCoroutine(Next(0.4f));
        }

        /// <summary>Called by the screen playing <paramref name="item"/> once it has played out.</summary>
        public static void Finished(ShowcaseItem item)
        {
            if (item != _current) return;
            _current = null;
            ClipEnded?.Invoke(item);
            if (_app != null) _app.StartCoroutine(Next(0.6f));
        }

        static IEnumerator Next(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (_app == null) yield break;
            if (Queue.Count == 0)
            {
                Running = false;
                _app.ShowMenu();
                AllDone?.Invoke();
                yield break;
            }
            ShowcaseItem item = Queue.Dequeue();
            _current = item;
            Debug.Log($"[Showcase] {item.Id}: {item.Title} ({item.Subtitle})");
            switch (item)
            {
                case ShowcaseClip clip: _app.StartShowcase(clip); break;
                case DeckClip deck: _app.StartDeckShowcase(deck); break;
            }
            ClipStarted?.Invoke(item);
        }
    }

    /// <summary>F8 on the main menu plays every showcase item (editor and development builds only).</summary>
    sealed class ShowcaseHotkey : MonoBehaviour
    {
        void Update()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            var kb = Keyboard.current;
            if (kb == null || !kb.f8Key.wasPressedThisFrame || ShowcaseDirector.Running) return;
            if (FindAnyObjectByType<MenuScreen>() == null) return;
            ShowcaseDirector.Play(App.Instance, ShowcaseDirector.All);
        }
    }
}
