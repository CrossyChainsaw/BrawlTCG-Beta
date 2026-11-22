using System;
using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>Where a deck comes from: the player's starter decks, the bot decks, or the player's own files.</summary>
    public enum DeckKind { Starter, Bot, Mine }

    /// <summary>
    /// ALL DECKS: every deck at a glance as a grid of tiles (two key cards peeking out of a plate in the deck's element
    /// colour, its name across them, stars for the bot that unlocks it and a status ribbon). Used by the Deck Builder
    /// (every deck, locked ones too) and by the menu's deck pickers (only decks that can be played).
    /// </summary>
    public sealed class DeckOverview
    {
        const float TileW = 200, TileH = 214, TileGapX = 22, TileGapY = 26;
        const int TileColumns = 8;
        const float OverviewW = 1840, OverviewH = 1000;

        sealed class DeckTile
        {
            public string Name;
            public DeckKind Kind;
            public StarterDecks.Starter Premade; // null for the player's own decks
            public List<int> Cards;
            public bool Locked;
        }

        readonly RectTransform _layer;
        readonly bool _playableOnly;
        readonly string _selected;
        readonly Action<string, DeckKind> _onPick;
        InputField _search;
        RectTransform _content;
        ScrollRect _scroll;

        DeckOverview(RectTransform layer, bool playableOnly, string selected, Action<string, DeckKind> onPick)
        {
            _layer = layer;
            _playableOnly = playableOnly;
            _selected = selected;
            _onPick = onPick;
        }

        /// <summary>
        /// Opens the grid on <paramref name="layer"/> (cleared first). <paramref name="playableOnly"/> leaves out locked
        /// decks and makes unfinished ones unpickable; <paramref name="selected"/> gets a gold outline. A click closes the
        /// grid and calls <paramref name="onPick"/> with the deck's name and kind.
        /// </summary>
        public static void Open(RectTransform layer, bool playableOnly, string selected, Action<string, DeckKind> onPick)
            => new DeckOverview(layer, playableOnly, selected, onPick).Build();

        void Build()
        {
            UI.Clear(_layer);
            _layer.SetAsLastSibling();

            var dim = UI.FullPanel(_layer, new Color(Theme.Night.r, Theme.Night.g, Theme.Night.b, 0.86f), "Dim");
            UI.Clicks(dim.gameObject, e => Close());
            var box = Theme.GlassPanel(dim.rectTransform, (App.Width - OverviewW) / 2, (App.Height - OverviewH) / 2, OverviewW, OverviewH, 28, "Overview");
            box.color = new Color32(12, 16, 38, 250);
            UI.Clicks(box.gameObject, e => { });
            box.gameObject.AddComponent<SlideIn>().Offset = new Vector2(0, -24);
            Transform t = box.transform;

            UI.Panel(t, Theme.Gold, 40, 36, 6, 40, "Accent").raycastTarget = false;
            Theme.Label(t, _playableOnly ? "CHOOSE A DECK" : "ALL DECKS", 44, Theme.Text, 60, 26, 600, 60, display: true);
            string hint = _playableOnly
                ? "Click a deck to play with it. Win against the bot in VS BOT to unlock more decks."
                : "Click a deck to open it. Stars show how strong a deck is: four stars win 70%+ of games, three 60-70%, two 50-60%, one under 50%.";
            Theme.Label(t, hint, 17, Theme.TextDim, 62, 86, 1100, 26);
            _search = Theme.Field(t, null, "Search decks…", "", OverviewW - 520, 34, 380);
            _search.onValueChanged.AddListener(_ => Layout());
            var close = Theme.Ghost(t, "×", OverviewW - 104, 36, 56, 52, Close, 32);
            close.GetComponentInChildren<Text>().font = Theme.Body;

            // Scrolling content, laid out by hand: section headers and rows of tiles.
            var area = UI.Place(UI.Rect("Area", t), 20, 130, OverviewW - 40, OverviewH - 150);
            _scroll = area.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.scrollSensitivity = 60;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = UI.Stretch(UI.Rect("Viewport", area));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            _content = UI.Rect("Content", viewport);
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.offsetMin = _content.offsetMax = Vector2.zero;
            _scroll.viewport = viewport;
            _scroll.content = _content;
            Layout();
        }

        void Close() => UI.Clear(_layer);

        /// <summary>The sections in order, each with its decks (filtered by the search text).</summary>
        List<(string title, List<DeckTile> tiles)> Sections()
        {
            string query = (_search?.text ?? "").Trim().ToLowerInvariant();
            bool Match(string name) => query.Length == 0 || name.ToLowerInvariant().Contains(query);

            var sections = new List<(string, List<DeckTile>)>();
            sections.Add(("STARTER DECKS", Collection.Starters.Where(s => Match(s.Name))
                .Select(s => new DeckTile { Name = s.Name, Kind = DeckKind.Starter, Premade = s, Cards = s.Cards }).ToList()));
            sections.Add(("MY DECKS", DeckStore.List().Where(n => Match(n) && !DeckLibrary.IsReserved(n))
                .Select(n => new DeckTile { Name = n, Kind = DeckKind.Mine, Cards = DeckStore.Load(n) }).ToList()));
            foreach (Bot.Level level in new[] { Bot.Level.Easy, Bot.Level.Normal, Bot.Level.Hard })
            {
                List<StarterDecks.Starter> decks = Collection.BotDecks.Where(s => !s.IsStarterPool && s.UnlockLevel == level).ToList();
                int unlocked = decks.Count(s => Collection.IsUnlocked(s.Name));
                sections.Add(($"BOT DECKS  ·  {level.ToString().ToUpperInvariant()} BOT  ·  {unlocked} / {decks.Count} UNLOCKED",
                    decks.Where(s => Match(s.Name) && (!_playableOnly || Collection.IsUnlocked(s.Name))).Select(s => new DeckTile
                    {
                        Name = s.Name, Kind = DeckKind.Bot, Premade = s, Cards = s.Cards, Locked = !Collection.IsUnlocked(s.Name),
                    }).ToList()));
            }
            // Decks unlocked in an earlier version that are now starter-pool decks: still the player's.
            List<StarterDecks.Starter> kept = Collection.BotDecks.Where(s => s.IsStarterPool && Match(s.Name)).ToList();
            if (kept.Count > 0)
                sections.Add(("OTHER UNLOCKED DECKS", kept.Select(s => new DeckTile { Name = s.Name, Kind = DeckKind.Bot, Premade = s, Cards = s.Cards }).ToList()));
            return sections;
        }

        void Layout()
        {
            UI.Clear(_content);
            float width = OverviewW - 40;
            float rowW = TileColumns * TileW + (TileColumns - 1) * TileGapX;
            float left = (width - rowW) / 2;
            float y = 10;
            int shown = 0;
            bool searching = (_search.text ?? "").Trim().Length > 0;
            foreach (var (title, tiles) in Sections())
            {
                if (tiles.Count == 0 && (searching || title != "MY DECKS")) continue;
                Theme.Label(_content, title, 18, Theme.Gold, left, y, rowW, 26, display: true).raycastTarget = false;
                UI.Panel(_content, new Color(1, 1, 1, 0.08f), left, y + 32, rowW, 2, "Rule").raycastTarget = false;
                y += 50;
                if (tiles.Count == 0)
                {
                    Theme.Label(_content, "No decks of your own yet. Build one in the Deck Builder under MY DECKS.", 16, Theme.TextFaint, left, y, rowW, 30).raycastTarget = false;
                    y += 50;
                    continue;
                }
                for (int i = 0; i < tiles.Count; i++)
                {
                    float x = left + (i % TileColumns) * (TileW + TileGapX);
                    float ty = y + (i / TileColumns) * (TileH + TileGapY);
                    Tile(_content, tiles[i], x, ty);
                }
                y += ((tiles.Count + TileColumns - 1) / TileColumns) * (TileH + TileGapY) + 10;
                shown += tiles.Count;
            }
            if (shown == 0 && searching)
                Theme.Label(_content, "No deck matches that search.", 20, Theme.TextDim, 0, y, width, 40, TextAnchor.MiddleCenter);
            _content.sizeDelta = new Vector2(0, y + 20);
            _scroll.verticalNormalizedPosition = 1;
        }

        void Tile(Transform parent, DeckTile deck, float x, float y)
        {
            var root = UI.Place(UI.Rect(deck.Name, parent), x, y, TileW, TileH);
            bool ready = DeckRules.ValidateForPlay(deck.Cards) == null;
            bool pickable = !deck.Locked && (!_playableOnly || ready);
            Element element = MainElement(deck);
            Color frame = CardStyle.FrameColor(element);
            Color dark = Color.Lerp(frame, Theme.Night, 0.55f);
            if (deck.Locked)
            {
                frame = Color.Lerp(frame, new Color(0.25f, 0.27f, 0.35f), 0.7f);
                dark = Color.Lerp(dark, new Color(0.08f, 0.09f, 0.14f), 0.7f);
            }

            // The "book": a back plate, the two cards peeking out of it, then the front plate with the name.
            var hit = Theme.RoundedPanel(root, new Color(1, 1, 1, 0), -6, -6, TileW + 12, TileH + 12, 18, "Hit");
            if (deck.Name == _selected && !deck.Locked)
                Quiet(Theme.RoundedPanel(root, Theme.Gold, -6, -6, TileW + 12, TileH + 12, 18, "Selected", outline: true));
            Quiet(Theme.RoundedPanel(root, dark, 8, 34, TileW - 16, TileH - 44, 16, "Back"));

            List<int> faces = TileCards(deck);
            const float scale = 0.66f;
            float cw = CardView.W * scale;
            float[] xs = faces.Count == 1 ? new[] { (TileW - cw) / 2 } : new[] { TileW / 2 - cw + 10, TileW / 2 - 10 };
            float[] tilt = faces.Count == 1 ? new[] { 0f } : new[] { 6f, -6f };
            for (int i = 0; i < faces.Count; i++)
            {
                CardView view = CardView.Create(root, CardCatalogue.Create(faces[i]), true, xs[i], 4 + Mathf.Abs(tilt[i]), scale);
                view.transform.localRotation = Quaternion.Euler(0, 0, tilt[i]);
                if (deck.Locked) view.SetDimmed(true, 0.8f);
                foreach (Graphic g in view.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            }

            var front = Quiet(Theme.RoundedPanel(root, frame, 0, 98, TileW, 82, 16, "Front"));
            var shade = Quiet(UI.Panel(front.transform, Color.black, 0, 0, TileW, 82, "Shade"));
            shade.sprite = Theme.Gradient(false, 0.45f, 0.05f);
            Quiet(Theme.RoundedPanel(front.transform, new Color(1, 1, 1, 0.35f), 0, 0, TileW, 82, 16, "Edge", outline: true));
            var name = Theme.Label(front.transform, deck.Name, 21, Color.white, 10, 8, TileW - 20, 66, TextAnchor.MiddleCenter, display: true);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 13;
            name.resizeTextMaxSize = 21;
            name.raycastTarget = false;
            Theme.AddOutline(name, 2, 0.8f);

            // Stars on the front plate's top edge: how strong the deck is (1-4, from its win rate).
            if (deck.Premade != null)
            {
                int stars = deck.Premade.Stars;
                const float s = 24, gap = 2;
                float sx = (TileW - stars * s - (stars - 1) * gap) / 2;
                for (int i = 0; i < stars; i++)
                {
                    var star = Quiet(UI.Panel(root, deck.Locked ? new Color(0.6f, 0.6f, 0.68f) : Theme.Gold, sx + i * (s + gap), 98 - s / 2 - 2, s, s, "Star"));
                    star.sprite = StarSprite();
                    Theme.AddOutline(star, 1.5f, 0.7f);
                }
            }

            // Status ribbon.
            string status;
            Color ribbon, ink;
            if (deck.Locked) { status = $"BEAT {deck.Premade.UnlockLevel.ToString().ToUpperInvariant()} BOT"; ribbon = new Color32(28, 30, 48, 255); ink = Theme.Gold; }
            else if (deck.Kind == DeckKind.Starter) { status = "STARTER"; ribbon = Theme.Gold; ink = Theme.Ink; }
            else if (deck.Kind == DeckKind.Bot) { status = "UNLOCKED"; ribbon = Theme.Success; ink = Theme.Ink; }
            else
            {
                status = ready ? "MY DECK" : "UNFINISHED";
                ribbon = ready ? new Color(0.85f, 0.88f, 1f) : Theme.Danger;
                ink = Theme.Ink;
            }
            var tag = Quiet(Theme.RoundedPanel(root, ribbon, 22, TileH - 40, TileW - 44, 32, 10, "Ribbon"));
            Theme.Label(tag.transform, status, 15, ink, 0, 0, TileW - 44, 32, TextAnchor.MiddleCenter, display: true).raycastTarget = false;

            // Hover lift and click. In the Deck Builder every tile opens; in a picker only playable decks can be chosen.
            var button = hit.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            DeckTile picked = deck;
            button.onClick.AddListener(() =>
            {
                if (_playableOnly && !pickable) return;
                Close();
                _onPick(picked.Name, picked.Kind);
            });
            if (!_playableOnly || pickable)
            {
                hit.gameObject.AddComponent<HoverFx>().Init(hit, new Color(1, 1, 1, 0), new Color(1, 1, 1, 0.08f), 1f);
                root.gameObject.AddComponent<TileLift>();
            }
        }

        static Image Quiet(Image img)
        {
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Two cards for the tile: the deck's two main legends (or its key cards), most copies first.</summary>
        static List<int> TileCards(DeckTile deck)
        {
            var nonEssence = deck.Cards.Where(id => id != 0 && CardCatalogue.Exists(id)).ToList();
            var legends = nonEssence.Where(id => CardCatalogue.Get(id) is LegendCard).GroupBy(id => id)
                .OrderByDescending(g => g.Count()).ThenByDescending(g => CardCatalogue.Get(g.Key).Cost).Select(g => g.Key).ToList();
            var picks = legends.Take(2).ToList();
            if (picks.Count < 2)
                picks.AddRange(nonEssence.GroupBy(id => id).OrderByDescending(g => g.Count()).Select(g => g.Key).Where(id => !picks.Contains(id)).Take(2 - picks.Count));
            if (picks.Count == 0) picks.Add(0); // an empty deck shows an Essence card
            return picks;
        }

        static Element MainElement(DeckTile deck)
        {
            if (deck.Premade != null) return deck.Premade.Element;
            var counts = deck.Cards.Where(id => id != 0 && CardCatalogue.Exists(id)).GroupBy(id => CardCatalogue.Get(id).Element)
                .OrderByDescending(g => g.Count()).FirstOrDefault();
            return counts?.Key ?? Element.Magic;
        }

        static Sprite _starSprite;

        /// <summary>A five-pointed star, drawn once (white, tint it).</summary>
        static Sprite StarSprite()
        {
            if (_starSprite != null) return _starSprite;
            const int size = 64, ss = 4;
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? 0.48f : 0.2f;
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                pts[i] = new Vector2(0.5f + r * Mathf.Cos(a), 0.52f + r * Mathf.Sin(a));
            }
            bool Inside(float x, float y)
            {
                bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                    if ((pts[i].y > y) != (pts[j].y > y) && x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                        inside = !inside;
                return inside;
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
            for (int pxl = 0; pxl < size; pxl++)
            {
                int n = 0;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                    if (Inside((pxl + (sx + 0.5f) / ss) / size, (py + (sy + 0.5f) / ss) / size)) n++;
                px[py * size + pxl] = new Color32(255, 255, 255, (byte)(n * 255 / (ss * ss)));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _starSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _starSprite;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _starSprite = null; // runtime sprites die between Play sessions
    }

    /// <summary>Lifts a deck tile a little while the pointer is over it.</summary>
    sealed class TileLift : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        RectTransform _rt;
        Vector2 _base;
        bool _over;
        float _t;

        void Start()
        {
            _rt = (RectTransform)transform;
            _base = _rt.anchoredPosition;
        }

        public void OnPointerEnter(PointerEventData e) => _over = true;
        public void OnPointerExit(PointerEventData e) => _over = false;

        void Update()
        {
            if (_rt == null) return;
            _t = Mathf.MoveTowards(_t, _over ? 1 : 0, Time.unscaledDeltaTime * 8);
            _rt.anchoredPosition = _base + new Vector2(0, 8 * Theme.EaseOut(_t));
        }
    }
}
