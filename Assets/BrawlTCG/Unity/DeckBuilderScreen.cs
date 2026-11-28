using System;
using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>The deck builder (WinForms FRM_DeckBuilder): browse the collection, preview, add/remove copies, save decks.</summary>
    public sealed partial class DeckBuilderScreen : MonoBehaviour
    {
        // Layout
        static readonly Rect CollectionRect = new Rect(24, 96, 984, 960);
        static readonly Rect PreviewRect = new Rect(1022, 96, 360, 960);
        static readonly Rect DeckRect = new Rect(1396, 96, 500, 960);
        const float GridScale = 0.78f;
        const float CellW = CardView.W * GridScale, CellH = CardView.H * GridScale;

        static readonly string[] TypeFilters = { "ALL", "LEGEND", "WEAPON", "BATTLE", "STAGE", "ESSENCE" };

        App _app;

        // Collection
        InputField _search;
        string _typeFilter = "ALL";
        Element? _elementFilter;
        Weapon? _weaponFilter;

        // Collection order: catalogue order, or by cost (ties keep catalogue order).
        enum SortMode { Default, CostLow, CostHigh }
        static readonly (SortMode mode, string text)[] SortOptions = { (SortMode.Default, "DEFAULT"), (SortMode.CostLow, "COST LOW"), (SortMode.CostHigh, "COST HIGH") };
        SortMode _sort;
        readonly List<(SortMode mode, Image bg, Text label)> _sortChips = new List<(SortMode, Image, Text)>();
        const string PrefSort = "BrawlTCG.BuilderSort";

        // Cost filter: show only cards of exactly this cost (null = any cost). Click the same button again to clear it.
        int? _costFilter;
        readonly List<(int cost, Image bg, Text label)> _costChips = new List<(int, Image, Text)>();
        readonly List<(string key, Image bg, Text label)> _typeChips = new List<(string, Image, Text)>();
        readonly List<(Element? element, Image bg, Text label)> _elementChips = new List<(Element?, Image, Text)>();
        readonly List<(Weapon weapon, Image bg, RawImage icon)> _weaponChips = new List<(Weapon, Image, RawImage)>();
        readonly List<GridCell> _cells = new List<GridCell>();
        Text _resultCount;
        ScrollRect _gridScroll;

        // Preview
        RectTransform _preview;
        Card _selected;
        float _lastClickTime;
        int _lastClickId = -1;

        // Deck
        ThemedSelector _deckSelector;
        Button _save, _addButton, _newDeckButton, _renameButton;
        Text _deleteLabel, _deckTitle, _status;
        RectTransform _stats;
        ScrollList _deckList;
        List<int> _deck = new List<int>();
        string _deckName;
        bool _dirty, _confirmDelete;
        RectTransform _modal, _peekLayer;
        const string PrefLastDeck = "BrawlTCG.BuilderDeck", PrefLastCategory = "BrawlTCG.BuilderCategory";

        // Deck categories: premade decks the player owns can be looked at and played, never changed.
        enum DeckCategory { Starter, Bot, Mine }
        DeckCategory _category = DeckCategory.Mine;
        readonly List<(DeckCategory category, Image bg, Text label)> _categoryTabs = new List<(DeckCategory, Image, Text)>();
        Text _selectorCaption, _readOnlyNote;
        GameObject _editButtons;
        /// <summary>The open premade deck (starter or bot deck), or null for the player's own decks.</summary>
        StarterDecks.Starter _premade;
        /// <summary>The open bot deck isn't unlocked yet: only a teaser is shown.</summary>
        bool _locked;
        bool ReadOnly => _premade != null;
        const string LockedSuffix = "  ·  LOCKED";

        sealed class GridCell
        {
            public Card Card;
            public GameObject Root;
            public CardView View;
            public GameObject Badge;
            public Text BadgeText;
        }

        // ───────────────────────────── Build ─────────────────────────────

        public void Build(App app)
        {
            _app = app;
            BuildBackdrop();

            Theme.Label(transform, "DECK BUILDER", 46, Theme.Text, 28, 20, 600, 60, display: true);
            Theme.Label(transform, "Build decks of at least 40 cards with 10 or more Essence. Up to 4 copies of each card.", 17, Theme.TextDim, 380, 40, 900, 30);
            Theme.Ghost(transform, "‹  MENU", 1736, 26, 160, 50, BackToMenu, 22);
            if (_showcase == null) Theme.Cta(transform, "ALL DECKS", 1540, 26, 180, 50, OpenOverview, 22);

            BuildCollection();
            BuildPreview();
            BuildDeckPanel();
            _peekLayer = UI.Stretch(UI.Rect("PeekLayer", transform)); // full-size previews of mentioned cards
            _overviewLayer = UI.Stretch(UI.Rect("OverviewLayer", transform)); // ALL DECKS grid
            _modal = UI.Stretch(UI.Rect("Modal", transform)); // top layer for the unsaved-changes dialog

            ApplyFilters();
            if (_showcase != null) { SetCategory(DeckCategory.Mine, null); SwitchDeck(null); return; } // the showcase makes its own deck
            // Reopens on the category and deck that were open last time (#40); new players start on their starter decks.
            var category = (DeckCategory)Mathf.Clamp(PlayerPrefs.GetInt(PrefLastCategory, DeckStore.List().Count > 0 ? (int)DeckCategory.Mine : 0), 0, 2);
            SetCategory(category, PlayerPrefs.GetString(PrefLastDeck, null));
            Select(CardCatalogue.All.OfType<LegendCard>().First());
        }

        void BuildBackdrop()
        {
            RectTransform backdrop = _app.Backdrop;
            Texture2D tex = Resources.Load<Texture2D>("BrawlTCG/UI/MenuBackdrop");
            var pic = UI.Rect("Backdrop", backdrop);
            pic.anchorMin = pic.anchorMax = new Vector2(0.5f, 0.5f);
            pic.pivot = new Vector2(0.5f, 0.5f);
            var raw = pic.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = new Color(0.3f, 0.31f, 0.42f, 1f);
            raw.raycastTarget = false;
            if (tex != null)
            {
                var fitter = pic.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = (float)tex.width / tex.height;
            }
        }

        // ───────────────────────────── Collection ─────────────────────────────

        void BuildCollection()
        {
            var panel = Theme.GlassPanel(transform, CollectionRect.x, CollectionRect.y, CollectionRect.width, CollectionRect.height, 24, "Collection");
            panel.color = new Color32(12, 16, 38, 238);
            Transform t = panel.transform;
            float w = CollectionRect.width;

            // Search
            _search = Theme.Field(t, null, "Search cards…", "", 20, 20, 300);
            _search.onValueChanged.AddListener(_ => ApplyFilters());

            // Type chips
            float x = 336;
            foreach (string type in TypeFilters)
            {
                float cw = 22 + type.Length * 12;
                string key = type;
                var (bg, label) = Chip(t, type, x, 24, cw, 44, () => { _typeFilter = key; ApplyFilters(); });
                _typeChips.Add((key, bg, label));
                x += cw + 8;
            }

            // Element chips
            x = 20;
            var elements = new List<Element?> { null };
            elements.AddRange(((Element[])Enum.GetValues(typeof(Element))).Select(e => (Element?)e));
            foreach (Element? element in elements)
            {
                string text = element?.ToString().ToUpperInvariant() ?? "ALL ELEMENTS";
                float cw = 40 + text.Length * 11;
                Element? e = element;
                var (bg, label) = Chip(t, text, x, 84, cw, 40, () => { _elementFilter = e; ApplyFilters(); }, element);
                _elementChips.Add((element, bg, label));
                x += cw + 8;
            }

            // Weapon filters (icons)
            x = 20;
            Theme.Label(t, "WEAPON", 14, Theme.TextDim, x, 146, 80, 40, TextAnchor.MiddleLeft, display: true);
            x += 84;
            foreach (Weapon weapon in ((Weapon[])Enum.GetValues(typeof(Weapon))).Where(wp => wp != Weapon.Any))
            {
                Weapon wp = weapon;
                var bg = Theme.RoundedPanel(t, new Color(1, 1, 1, 0.06f), x, 140, 52, 52, 12, "Weapon " + weapon);
                var b = bg.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { _weaponFilter = _weaponFilter == wp ? (Weapon?)null : wp; ApplyFilters(); });
                var icon = UI.Picture(bg.transform, Art.Weapon(weapon), 8, 8, 36, 36, cover: false);
                _weaponChips.Add((weapon, bg, icon));
                x += 58;
            }

            _resultCount = Theme.Label(t, "", 15, Theme.TextDim, 20, 204, 110, 22, display: true);

            // Sort chips
            _sort = (SortMode)Mathf.Clamp(PlayerPrefs.GetInt(PrefSort, 0), 0, SortOptions.Length - 1);
            Theme.Label(t, "SORT", 14, Theme.TextDim, 136, 200, 50, 30, TextAnchor.MiddleLeft, display: true);
            x = 184;
            foreach (var (mode, text) in SortOptions)
            {
                float cw = 20 + text.Length * 9;
                SortMode m = mode;
                var (bg, label) = Chip(t, text, x, 200, cw, 30, () => { _sort = m; PlayerPrefs.SetInt(PrefSort, (int)m); ApplySort(); ApplyFilters(); });
                _sortChips.Add((mode, bg, label));
                x += cw + 6;
            }

            // Cost filter chips 1-9
            Theme.Label(t, "COST", 14, Theme.TextDim, x + 8, 200, 50, 30, TextAnchor.MiddleLeft, display: true);
            x += 56;
            for (int cost = 1; cost <= 9; cost++)
            {
                int c = cost;
                var (bg, label) = Chip(t, cost.ToString(), x, 200, 28, 30, () => { _costFilter = _costFilter == c ? (int?)null : c; ApplyFilters(); });
                _costChips.Add((cost, bg, label));
                x += 32;
            }
            Theme.Label(t, "Double-click to add", 14, Theme.TextFaint, x + 4, 204, w - 20 - (x + 4), 22, TextAnchor.MiddleRight);

            // Grid
            var area = UI.Place(UI.Rect("GridArea", t), 8, 234, w - 16, CollectionRect.height - 244);
            _gridScroll = area.gameObject.AddComponent<ScrollRect>();
            _gridScroll.horizontal = false;
            _gridScroll.scrollSensitivity = 60;
            _gridScroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = UI.Stretch(UI.Rect("Viewport", area));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var content = UI.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CellW, CellH + 32);
            grid.spacing = new Vector2(16, 12);
            grid.padding = new RectOffset(26, 16, 18, 24);
            grid.childAlignment = TextAnchor.UpperLeft;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _gridScroll.viewport = viewport;
            _gridScroll.content = content;

            foreach (Card proto in CardCatalogue.All)
            {
                var cell = UI.Rect(proto.Name, content);
                var view = CardView.Create(cell, proto.Clone(), true, 0, 0, GridScale);
                Card card = proto;
                view.Clicked = _ => OnGridClicked(card);

                // "In deck" ribbon under the card
                var badge = Theme.RoundedPanel(cell, Theme.Gold, (CellW - 100) / 2, CellH + 6, 100, 24, 12, "Copies");
                badge.raycastTarget = false;
                var badgeText = Theme.Label(badge.transform, "", 14, Theme.Ink, 0, 0, 100, 24, TextAnchor.MiddleCenter, display: true);
                _cells.Add(new GridCell { Card = proto, Root = cell.gameObject, View = view, Badge = badge.gameObject, BadgeText = badgeText });
            }
            ApplySort();
        }

        /// <summary>Reorders the grid cells for the current sort mode (the grid lays children out in sibling order).</summary>
        void ApplySort()
        {
            IEnumerable<GridCell> order = _sort switch
            {
                SortMode.CostLow => _cells.OrderBy(c => c.Card.Cost),
                SortMode.CostHigh => _cells.OrderByDescending(c => c.Card.Cost),
                _ => _cells,
            };
            int i = 0;
            foreach (GridCell cell in order)
                cell.Root.transform.SetSiblingIndex(i++);
        }

        (Image bg, Text label) Chip(Transform parent, string text, float x, float y, float w, float h, Action onClick, Element? dot = null)
        {
            var bg = Theme.RoundedPanel(parent, new Color(1, 1, 1, 0.06f), x, y, w, h, h / 2, "Chip " + text);
            var b = bg.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => onClick());
            float tx = 0;
            if (dot != null)
            {
                Quiet(Theme.RoundedPanel(bg.transform, CardStyle.FrameColor(dot.Value), 14, h / 2 - 6, 12, 12, 6, "Dot"));
                tx = 14;
            }
            var label = Theme.Label(bg.transform, text, 15, Theme.TextDim, tx, 0, w - tx, h, TextAnchor.MiddleCenter, display: true);
            return (bg, label);
        }

        void ApplyFilters()
        {
            string query = (_search.text ?? "").Trim().ToLowerInvariant();
            int shown = 0;
            foreach (GridCell cell in _cells)
            {
                Card c = cell.Card;
                bool visible =
                    (_typeFilter == "ALL" || c.Type.ToString().ToUpperInvariant() == _typeFilter) &&
                    (_elementFilter == null || c.Element == _elementFilter) &&
                    (_weaponFilter == null || HasWeapon(c, _weaponFilter.Value)) &&
                    (_costFilter == null || c.Cost == _costFilter) &&
                    (query.Length == 0 || c.Name.ToLowerInvariant().Contains(query) || c.Description.ToLowerInvariant().Contains(query));
                cell.Root.SetActive(visible);
                if (visible) shown++;
            }
            _resultCount.text = $"{shown} CARD{(shown == 1 ? "" : "S")}";

            foreach (var (key, bg, label) in _typeChips) SetChip(bg, label, key == _typeFilter);
            foreach (var (element, bg, label) in _elementChips) SetChip(bg, label, element == _elementFilter);
            foreach (var (mode, bg, label) in _sortChips) SetChip(bg, label, mode == _sort);
            foreach (var (cost, bg, label) in _costChips) SetChip(bg, label, cost == _costFilter);
            foreach (var (weapon, bg, icon) in _weaponChips)
            {
                bool on = weapon == _weaponFilter;
                bg.color = on ? Theme.Gold : new Color(1, 1, 1, 0.06f);
                icon.color = on ? Theme.Ink : Theme.TextDim;
            }
            _gridScroll.verticalNormalizedPosition = 1;
        }

        static void SetChip(Image bg, Text label, bool on)
        {
            bg.color = on ? Theme.Gold : new Color(1, 1, 1, 0.06f);
            label.color = on ? Theme.Ink : Theme.TextDim;
        }

        static bool HasWeapon(Card card, Weapon weapon)
        {
            if (card is LegendCard legend) return legend.HasWeapon(weapon);
            if (card is WeaponCard w) return w.Weapon == weapon;
            return false;
        }

        void OnGridClicked(Card card)
        {
            bool doubleClick = _lastClickId == card.Id && Time.unscaledTime - _lastClickTime < 0.35f;
            _lastClickId = card.Id;
            _lastClickTime = Time.unscaledTime;
            Select(card);
            if (doubleClick) AddSelected();
        }

        void RefreshCopyBadges()
        {
            foreach (GridCell cell in _cells)
            {
                int copies = _deck.Count(id => id == cell.Card.Id);
                cell.Badge.SetActive(copies > 0);
                bool essence = cell.Card is EssenceCard;
                cell.BadgeText.text = essence ? $"IN DECK ×{copies}" : $"IN DECK {copies}/{DeckRules.MaxCopies}";
                cell.View.SetGlow(cell.Card == _selected ? Theme.Gold : (Color?)null, 0.85f);
            }
            foreach (var (card, view) in _deckViews)
                if (view != null) view.SetGlow(card == _selected ? Theme.Gold : (Color?)null, 0.85f);
        }

        // ───────────────────────────── Preview ─────────────────────────────

        void BuildPreview()
        {
            var panel = Theme.GlassPanel(transform, PreviewRect.x, PreviewRect.y, PreviewRect.width, PreviewRect.height, 24, "PreviewPanel");
            panel.color = new Color32(12, 16, 38, 238);
            _preview = panel.rectTransform;
        }

        void Select(Card card)
        {
            _selected = card;
            RefreshCopyBadges();
            ShowPreview();
        }

        void ShowPreview()
        {
            UI.Clear(_preview);
            UI.Clear(_peekLayer);
            Card card = _selected;
            if (card == null) return;
            Transform t = _preview;
            float w = PreviewRect.width;

            Theme.Label(t, "PREVIEW", 15, Theme.Gold, 20, 16, 200, 20, display: true);
            var big = CardView.Create(t, card.Clone(), true, (w - 300) / 2, 44, 2f);
            foreach (Graphic g in big.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;

            int copies = _deck.Count(id => id == card.Id);
            string copyText = card is EssenceCard ? $"In deck: {copies}" : $"In deck: {copies} / {DeckRules.MaxCopies}";
            Theme.Label(t, copyText, 18, copies > 0 ? Theme.Text : Theme.TextDim, 20, 456, w - 40, 26, TextAnchor.MiddleCenter, display: true);

            var add = _addButton = Theme.Cta(t, "ADD", 20, 492, (w - 52) / 2, 56, AddSelected, 24);
            add.interactable = _deckName != null && !ReadOnly && DeckRules.CanAddCard(_deck, card.Id) == null;
            var remove = Theme.Ghost(t, "REMOVE", 32 + (w - 52) / 2, 492, (w - 52) / 2, 56, RemoveSelected, 20);
            remove.interactable = copies > 0 && !ReadOnly;

            float y = 568;
            if (card is LegendCard legend)
            {
                Theme.Label(t, $"{legend.BaseHealth} HP  ·  {legend.Power} ATK  ·  {legend.WeaponText(Pretty)}", 15, Theme.TextDim, 20, y, w - 40, 22, TextAnchor.MiddleLeft, display: true);
                y += 30;
                Theme.Label(t, "ATTACKS", 14, Theme.Gold, 20, y, 200, 20, display: true);
                y += 24;
                if (legend.Attacks.Count == 0)
                {
                    Theme.Label(t, "No attacks.", 16, Theme.TextDim, 20, y, w - 40, 24);
                    y += 30;
                }
                foreach (Attack attack in legend.Attacks)
                {
                    PreviewAttack(t, legend, attack, 16, y, w - 32);
                    y += 62;
                }
                string effects = TriggerIcons.SplitCardText(legend).abilities;
                if (effects.Length > 0)
                {
                    Theme.Label(t, "ABILITIES", 14, Theme.Gold, 20, y + 4, 200, 20, display: true);
                    y += 28 + TriggerIcons.PaintAbilities(t, effects, 20, y + 28, w - 40, PreviewRect.height - y - 40 - MentionsRoom(card), 15, 10, Theme.Text);
                }
            }
            else
            {
                // Same layout as legends: a details line first, then ABILITIES with one icon per trigger.
                var (abilities, info) = TriggerIcons.SplitCardText(card);
                float room = PreviewRect.height - y - 24 - MentionsRoom(card);
                y += TriggerIcons.PaintAbilities(t, info, 20, y, w - 40, abilities.Length > 0 ? Mathf.Min(110, room) : room, 15, 11, Theme.TextDim) + 10;
                if (abilities.Length > 0)
                {
                    Theme.Label(t, "ABILITIES", 14, Theme.Gold, 20, y + 4, 200, 20, display: true);
                    y += 28 + TriggerIcons.PaintAbilities(t, abilities, 20, y + 28, w - 40, PreviewRect.height - y - 40 - MentionsRoom(card), 16, 10, Theme.Text);
                }
            }

            // Cards the text mentions ("Craft Fait, ...", "the stage becomes Matrix"): hover to see, click to select (#36).
            int[] mentioned = CardReferences.For(card);
            if (mentioned != null)
            {
                y += 12;
                Theme.Label(t, "MENTIONED CARDS", 14, Theme.Gold, 20, y, 300, 20, display: true);
                y += 26;
                float tw = CardView.W * MentionScale, th = CardView.H * MentionScale, gap = 10;
                int perRow = Mathf.Max(1, (int)((w - 40 + gap) / (tw + gap)));
                for (int i = 0; i < mentioned.Length; i++)
                {
                    float px = 20 + (i % perRow) * (tw + gap), py = y + (i / perRow) * (th + gap);
                    MentionThumb(t, mentioned[i], px, py, MentionScale, py);
                }
            }
        }

        const float MentionScale = 0.4f, AttackMentionScale = 0.24f;

        /// <summary>Height kept free at the bottom of the preview for the MENTIONED CARDS row.</summary>
        static float MentionsRoom(Card card) => CardReferences.For(card) == null ? 0 : 12 + 26 + CardView.H * MentionScale + 10;

        /// <summary>
        /// A hoverable thumbnail of a mentioned card; the full card shows left of the preview panel, lined up with
        /// <paramref name="panelY"/> (the thumbnail's y inside the panel). A click selects the card.
        /// </summary>
        void MentionThumb(Transform parent, int cardId, float x, float y, float scale, float panelY)
        {
            float bigW = CardView.W * 1.9f, bigH = CardView.H * 1.9f;
            var at = new Vector2(PreviewRect.x - bigW - 16, Mathf.Clamp(PreviewRect.y + panelY - bigH / 2, 20, App.Height - bigH - 20));
            CardView thumb = CardPeek.Create(parent, cardId, x, y, scale, _peekLayer, at);
            Card target = CardCatalogue.All.FirstOrDefault(k => k.Id == cardId);
            if (target != null) UI.Clicks(thumb.gameObject, e => Select(target));
        }

        void PreviewAttack(Transform t, LegendCard legend, Attack attack, float x, float y, float w)
        {
            var row = Theme.RoundedPanel(t, new Color(1, 1, 1, 0.06f), x, y, w, 54, 12, "Attack");
            row.raycastTarget = false;
            Transform rt = row.transform;
            float ix = 8;
            ix = ReqIcon(rt, attack.WeaponOne, attack.WeaponOneAmount, attack.WeaponOneBurnAmount, ix);
            if (attack.WeaponTwo != null) ix = ReqIcon(rt, attack.WeaponTwo.Value, attack.WeaponTwoAmount, attack.WeaponTwoBurnAmount, ix);
            int[] mentioned = CardReferences.For(attack);
            float tw = CardView.W * AttackMentionScale + 6;
            float right = 70 + (mentioned == null ? 0 : mentioned.Length * tw);
            var name = Theme.Label(rt, attack.Name, 18, Theme.Text, ix + 4, 4, w - ix - right, 26, TextAnchor.MiddleLeft, display: true);
            name.verticalOverflow = VerticalWrapMode.Truncate;
            var tags = new List<string>();
            if (attack.EffectText != null) tags.Add(attack.EffectText);
            if (attack.Kind != AttackKind.Strike) tags.Add(attack.Kind.ToString().ToUpperInvariant());
            if (attack.InstaEffect) tags.Add("INSTANT");
            if (attack.MultiHit) tags.Add("HITS ALL");
            if (attack.FriendlyFire) tags.Add("ALLY");
            int burns = attack.WeaponOneBurnAmount + attack.WeaponTwoBurnAmount;
            if (burns > 0) tags.Add($"BURNS {burns} WEAPON{(burns > 1 ? "S" : "")}");
            var tagText = Theme.Label(rt, tags.Count > 0 ? string.Join(" · ", tags) : "", 12, Theme.TextDim, ix + 4, 30, w - ix - right, 18, TextAnchor.MiddleLeft, display: true);
            tagText.resizeTextForBestFit = true; // long effect texts shrink instead of being cut off
            tagText.resizeTextMinSize = 8;
            tagText.resizeTextMaxSize = 12;
            if (mentioned != null)
                for (int m = 0; m < mentioned.Length; m++)
                    MentionThumb(rt, mentioned[m], w - 70 - (m + 1) * tw, 3, AttackMentionScale, y + 3);
            string damage = attack.OneHitKO ? "KO" : attack.HealBonus != null ? "+" + attack.HealAmount(legend)
                : attack.AttackModifier <= -1000 ? "—" : Attack.CalculateBaseDamage(legend, attack).ToString();
            Theme.Label(rt, damage, 28, Theme.Gold, w - 70, 0, 60, 54, TextAnchor.MiddleRight, display: true);
        }

        static float ReqIcon(Transform parent, Weapon weapon, int amount, int burn, float x)
        {
            var chip = Theme.RoundedPanel(parent, new Color(0, 0, 0, 0.35f), x, 7, 58, 40, 10, "Req");
            chip.raycastTarget = false;
            Texture2D icon = Art.Weapon(weapon);
            if (icon != null) UI.Picture(chip.transform, icon, 5, 5, 30, 30, cover: false);
            else Theme.Label(chip.transform, "ANY", 11, Theme.Text, 0, 0, 36, 40, TextAnchor.MiddleCenter, display: true);
            Theme.Label(chip.transform, amount == 0 ? "—" : $"×{amount}", 14, Theme.Gold, 32, 0, 24, 40, TextAnchor.MiddleCenter, display: true);
            if (burn > 0) TriggerIcons.BurnCount(chip.transform, burn, 38, -10, 24); // flame with the number of weapons burned
            return x + 64;
        }

        static string Pretty(Weapon w) => w == Weapon.RocketLance ? "Rocket Lance" : w == Weapon.BattleBoots ? "Battle Boots" : w.ToString();

        // ───────────────────────────── Deck panel ─────────────────────────────

        void BuildDeckPanel()
        {
            var panel = Theme.GlassPanel(transform, DeckRect.x, DeckRect.y, DeckRect.width, DeckRect.height, 24, "DeckPanel");
            panel.color = new Color32(12, 16, 38, 238);
            Transform t = panel.transform;
            float w = DeckRect.width;

            // Category tabs: STARTER and BOT DECKS are premade and read-only, MY DECKS are the player's own files.
            var tabs = Theme.RoundedPanel(t, new Color(1, 1, 1, 0.06f), 18, 16, w - 36, 50, 12, "Categories");
            var categories = new[] { (DeckCategory.Starter, "STARTER"), (DeckCategory.Bot, "BOT DECKS"), (DeckCategory.Mine, "MY DECKS") };
            float segW = (w - 36 - 8) / categories.Length;
            for (int i = 0; i < categories.Length; i++)
            {
                DeckCategory c = categories[i].Item1;
                var seg = Theme.RoundedPanel(tabs.transform, new Color(0, 0, 0, 0), 4 + i * segW, 4, segW, 42, 10, "Tab");
                var b = seg.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() =>
                {
                    if (_showcase != null || c == _category) return;
                    ConfirmUnsaved("SWITCH", () => SetCategory(c, null));
                });
                var label = Theme.Label(seg.transform, categories[i].Item2, 16, Theme.TextDim, 0, 0, segW, 42, TextAnchor.MiddleCenter, display: true);
                _categoryTabs.Add((c, seg, label));
            }

            _selectorCaption = Theme.Label(t, "", 14, Theme.TextDim, 20, 78, w - 40, 20, display: true);
            _deckSelector = new ThemedSelector(t, null, 18, 100, w - 36, new string[0]);
            _deckSelector.Changed += option =>
            {
                string previous = OptionFor(_deckName);
                ConfirmUnsaved("SWITCH", () => SwitchDeck(option), () => _deckSelector.SetOptions(CategoryOptions(), previous));
            };

            // Four equal buttons in a 2x2 grid (#41). NEW DECK and RENAME ask for the name in a pop-up. MY DECKS only.
            const float bh = 54, gap = 10, by = 164;
            float half = (w - 36 - gap) / 2;
            _editButtons = UI.Place(UI.Rect("EditButtons", t), 0, 0, w, by + 2 * bh + gap).gameObject;
            Transform eb = _editButtons.transform;
            _newDeckButton = Theme.Ghost(eb, "NEW DECK", 18, by, half, bh, CreateDeck, 18);
            _renameButton = Theme.Ghost(eb, "RENAME", 18 + half + gap, by, half, bh, RenameDeck, 18);

            _save = Theme.Cta(eb, "SAVE", 18, by + bh + gap, half, bh, SaveDeck, 18);
            var delete = Theme.Ghost(eb, "DELETE", 18 + half + gap, by + bh + gap, half, bh, DeleteDeck, 18);
            _deleteLabel = delete.GetComponentInChildren<Text>();
            _readOnlyNote = Theme.Label(t, "", 16, Theme.TextDim, 22, by + 4, w - 44, 2 * bh + gap - 8, TextAnchor.MiddleLeft);

            _deckTitle = Theme.Label(t, "", 22, Theme.Text, 18, 298, w - 36, 30, display: true);
            _deckTitle.verticalOverflow = VerticalWrapMode.Truncate;
            _stats = UI.Place(UI.Rect("Stats", t), 18, 332, w - 36, 160);
            _status = Theme.Label(t, "", 15, Theme.TextDim, 18, 494, w - 36, 40);

            _deckList = new ScrollList(t, 10, 538, w - 20, DeckRect.height - 548, new Color(0, 0, 0, 0));
            _deckList.Scroll.scrollSensitivity = 40;
        }

        /// <summary>Shows a category: its tab, its decks in the selector, and the edit buttons only for MY DECKS.</summary>
        void SetCategory(DeckCategory category, string select)
        {
            _category = category;
            foreach (var (c, bg, label) in _categoryTabs)
            {
                bool on = c == category;
                bg.color = on ? Theme.Gold : new Color(0, 0, 0, 0);
                label.color = on ? Theme.Ink : Theme.TextDim;
            }
            _editButtons.SetActive(category == DeckCategory.Mine);
            _readOnlyNote.gameObject.SetActive(category != DeckCategory.Mine);
            switch (category)
            {
                case DeckCategory.Starter:
                    _selectorCaption.text = "YOUR STARTER DECKS";
                    _readOnlyNote.text = "The decks you started with. You can play them in every mode, but not change them. " +
                        "Build your own under MY DECKS.";
                    break;
                case DeckCategory.Bot:
                    int unlocked = Collection.UnlockedBotDecks.Count, total = Collection.BotDecks.Count;
                    _selectorCaption.text = $"BOT DECKS  ·  {unlocked} / {total} UNLOCKED";
                    _readOnlyNote.text = "Beat the bot in VS BOT to unlock the deck it played. Unlocked decks can be played " +
                        "in every mode, but not changed.";
                    break;
                default:
                    _selectorCaption.text = "YOUR DECKS";
                    break;
            }
            List<string> options = _showcase != null ? new List<string>() : CategoryOptions();
            _deckSelector.SetOptions(options, OptionFor(select));
            SwitchDeck(_deckSelector.Value);
        }

        /// <summary>The selector's entries for the open category. Locked bot decks are listed too, marked LOCKED.</summary>
        List<string> CategoryOptions()
        {
            switch (_category)
            {
                case DeckCategory.Starter: return Collection.Starters.Select(s => s.Name).ToList();
                case DeckCategory.Bot: return Collection.BotDecks.Select(s => OptionFor(s.Name)).ToList();
                default: return DeckStore.List();
            }
        }

        string OptionFor(string name) =>
            name != null && _category == DeckCategory.Bot && !Collection.IsUnlocked(name) ? name + LockedSuffix : name;

        static string NameOf(string option) =>
            option != null && option.EndsWith(LockedSuffix) ? option.Substring(0, option.Length - LockedSuffix.Length) : option;

        void SwitchDeck(string option)
        {
            string name = NameOf(option);
            _deckName = name;
            _confirmDelete = false;
            _premade = _category == DeckCategory.Mine || name == null ? null : StarterDecks.Find(name);
            _locked = _premade != null && !Collection.Owns(name);
            var errors = new List<string>();
            if (_premade != null) _deck = _locked ? new List<int>() : _premade.Cards.ToList(); // a locked deck's list stays secret
            else _deck = name != null ? DeckStore.Load(name, errors) : new List<int>();
            foreach (string e in errors) _app.Toast(e);
            _dirty = false;
            RememberDeck();
            RefreshDeck();
        }

        void RememberDeck()
        {
            if (_showcase != null) return;
            PlayerPrefs.SetInt(PrefLastCategory, (int)_category);
            if (_deckName != null) PlayerPrefs.SetString(PrefLastDeck, _deckName);
            PlayerPrefs.Save();
        }

        void RefreshDeck()
        {
            RefreshCopyBadges();
            if (_selected != null) ShowPreview();

            string tag = _premade == null ? "" : _locked ? "  ·  LOCKED" : Collection.IsStarter(_deckName) ? "  ·  STARTER" : "  ·  BOT DECK";
            _deckTitle.text = _deckName == null ? "NO DECK SELECTED" : _deckName.ToUpperInvariant() + tag + (_dirty ? "  ·  UNSAVED" : "");
            _deckTitle.color = _dirty || _locked ? Theme.Gold : Theme.Text;

            string saveError = _deckName == null ? "Create a deck first." : DeckRules.ValidateForSave(_deck);
            _save.interactable = saveError == null && _dirty;
            _deleteLabel.text = _confirmDelete ? "CONFIRM DELETE?" : "DELETE";
            _deleteLabel.color = _confirmDelete ? Theme.Danger : Theme.Text;

            string playError = DeckRules.ValidateForPlay(_deck);
            if (_locked) { _status.text = $"Beat the {_premade.UnlockLevel} bot in VS BOT to unlock this deck."; _status.color = Theme.Gold; }
            else if (_premade != null) { _status.text = _premade.Description; _status.color = Theme.TextDim; }
            else if (_deckName == null)
            {
                _status.text = _category == DeckCategory.Mine ? "Create a deck with NEW DECK to start building." : "No decks here yet.";
                _status.color = Theme.TextDim;
            }
            else if (playError == null) { _status.text = "Ready to play!"; _status.color = Theme.Success; }
            else { _status.text = saveError ?? playError; _status.color = saveError != null ? Theme.Danger : Theme.Gold; }

            DrawStats();
            DrawDeckList();
        }

        void DrawStats()
        {
            UI.Clear(_stats);
            Transform t = _stats;
            float w = DeckRect.width - 36;
            if (_locked)
            {
                // A locked deck shows how to get it, not what is in it.
                var plate = Quiet(Theme.RoundedPanel(t, new Color(1, 1, 1, 0.05f), 0, 0, w, 156, 16, "Locked"));
                Theme.Label(plate.transform, "LOCKED", 40, Theme.Gold, 0, 18, w, 50, TextAnchor.MiddleCenter, display: true);
                Theme.Label(plate.transform, $"BEAT THE {_premade.UnlockLevel.ToString().ToUpperInvariant()} BOT", 22, Theme.Text, 0, 72, w, 30, TextAnchor.MiddleCenter, display: true);
                Theme.Label(plate.transform, $"{_premade.Element}  ·  {_premade.Stars}-star deck  ·  the bot plays decks you don't have yet", 14, Theme.TextDim, 0, 110, w, 24, TextAnchor.MiddleCenter);
                return;
            }
            int count = _deck.Count;
            int essence = _deck.Count(id => id == 0);
            int legends = _deck.Count(id => CardCatalogue.Get(id) is LegendCard);

            // Card count towards the 40 needed to play
            Theme.Label(t, $"{count}", 34, Theme.Text, 0, 0, 80, 40, TextAnchor.MiddleLeft, display: true);
            Theme.Label(t, $"/ {DeckRules.MinimumCardsToPlay} CARDS", 15, Theme.TextDim, 58, 8, 160, 30, TextAnchor.MiddleLeft, display: true);
            Theme.Label(t, $"{essence} ESSENCE  ·  {legends} LEGENDS", 15, essence >= DeckRules.MinimumEssenceToSave ? Theme.TextDim : Theme.Danger, w - 260, 8, 260, 30, TextAnchor.MiddleRight, display: true);
            float ratio = Mathf.Clamp01(count / (float)DeckRules.MinimumCardsToPlay);
            Quiet(Theme.RoundedPanel(t, new Color(1, 1, 1, 0.1f), 0, 44, w, 10, 5, "Track"));
            if (ratio > 0)
                Quiet(Theme.RoundedPanel(t, count >= DeckRules.MinimumCardsToPlay ? Theme.Success : Theme.Gold, 0, 44, Mathf.Max(10, w * ratio), 10, 5, "Fill"));

            // Cost curve (non-Essence cards)
            Theme.Label(t, "COST CURVE", 13, Theme.TextDim, 0, 66, 200, 18, display: true);
            var buckets = new int[8];
            foreach (int id in _deck)
            {
                if (id == 0) continue;
                buckets[Mathf.Min(7, CardCatalogue.Get(id).Cost)]++;
            }
            int max = Mathf.Max(1, buckets.Max());
            float bw = (w - 7 * 8) / 8;
            for (int i = 0; i < 8; i++)
            {
                float h = 46 * buckets[i] / (float)max;
                float bx = i * (bw + 8);
                Quiet(Theme.RoundedPanel(t, new Color(1, 1, 1, 0.06f), bx, 88, bw, 46, 6, "Slot"));
                if (buckets[i] > 0)
                    Quiet(Theme.RoundedPanel(t, new Color32(150, 110, 255, 255), bx, 88 + 46 - h, bw, Mathf.Max(6, h), 6, "Bar"));
                Theme.Label(t, i == 7 ? "7+" : i.ToString(), 13, Theme.TextDim, bx, 136, bw, 18, TextAnchor.MiddleCenter, display: true);
                if (buckets[i] > 0)
                    Theme.Label(t, buckets[i].ToString(), 12, Theme.Text, bx, 88 + 46 - h - 16, bw, 16, TextAnchor.MiddleCenter, display: true);
            }
        }

        void DrawDeckList()
        {
            _deckList.Clear();
            if (_locked)
            {
                // The teaser: what the deck is about and three of its key cards, darkened.
                float w = DeckRect.width - 44;
                var about = _deckList.AddRow(96, new Color(1, 1, 1, 0.05f), null);
                about.sprite = Theme.Rounded;
                about.type = Image.Type.Sliced;
                about.pixelsPerUnitMultiplier = 2;
                Theme.Label(about.transform, _premade.Description, 16, Theme.Text, 14, 0, w - 28, 96, TextAnchor.MiddleLeft).raycastTarget = false;

                List<int> keys = DeckLibrary.KeyCards(_premade);
                const float scale = 0.86f, gap = 14;
                float cw = CardView.W * scale, total = keys.Count * cw + (keys.Count - 1) * gap;
                var row = _deckList.AddRow(CardView.H * scale + 24, new Color(0, 0, 0, 0), null);
                for (int i = 0; i < keys.Count; i++)
                {
                    CardView view = CardView.Create(row.transform, CardCatalogue.Create(keys[i]), true, (w - total) / 2 + i * (cw + gap), 12, scale);
                    view.SetDimmed(true, 0.86f);
                    view.AddCenterBanner("?", Theme.Gold);
                    foreach (Graphic g in view.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
                }
                return;
            }
            // One grid of the cards themselves (#73): left to right, DeckColumns per row, then the next row.
            // Ordered by type (legends, weapons, battle cards, stages, essence), then cost and name.
            _deckViews.Clear();
            List<Card> cards = _deck.Distinct()
                .Select(id => CardCatalogue.Get(id))
                .OrderBy(c => TypeOrder(c.Type)).ThenBy(c => c.Cost).ThenBy(c => c.Name)
                .ToList();
            if (cards.Count > 0) _deckList.AddRow(6, new Color(0, 0, 0, 0), null).raycastTarget = false; // top margin
            for (int i = 0; i < cards.Count; i += DeckColumns)
            {
                var row = _deckList.AddRow(DeckCellH, new Color(0, 0, 0, 0), null);
                row.raycastTarget = false; // scrolling still works: the cards pass drags on to the list
                for (int col = 0; col < DeckColumns && i + col < cards.Count; col++)
                    DeckCell(row.transform, cards[i + col], _deck.Count(id => id == cards[i + col].Id), DeckGridX + col * (DeckCardW + DeckGap));
            }
            if (_deck.Count == 0)
            {
                var empty = _deckList.AddRow(80, new Color(0, 0, 0, 0), null);
                Theme.Label(empty.transform, "This deck is empty. Add cards from the collection.", 16, Theme.TextFaint, 10, 0, DeckRect.width - 60, 80, TextAnchor.MiddleCenter);
            }
        }

        // Deck grid (#73): four cards per row in the 480px wide list.
        const int DeckColumns = 4;
        const float DeckGap = 10, DeckScale = 0.71f;
        const float DeckCardW = CardView.W * DeckScale, DeckCardH = CardView.H * DeckScale;
        const float DeckCellH = DeckCardH + 44;
        static readonly float DeckGridX = (DeckRect.width - 20 - DeckColumns * DeckCardW - (DeckColumns - 1) * DeckGap) / 2;
        readonly List<(Card card, CardView view)> _deckViews = new List<(Card, CardView)>();

        /// <summary>One card of the deck: the card itself (click to select it), and under it the copies, with − and + on your own decks.</summary>
        void DeckCell(Transform row, Card card, int copies, float x)
        {
            Card c = card;
            CardView view = CardView.Create(row, c.Clone(), true, x, 4, DeckScale);
            view.Clicked = _ => Select(c);
            if (c == _selected) view.SetGlow(Theme.Gold, 0.85f);
            _deckViews.Add((c, view));

            float y = 4 + DeckCardH + 6;
            if (ReadOnly)
            {
                var pill = Quiet(Theme.RoundedPanel(row, new Color(1, 1, 1, 0.08f), x + (DeckCardW - 56) / 2, y, 56, 28, 14, "Copies"));
                Theme.Label(pill.transform, $"×{copies}", 17, Theme.Gold, 0, 0, 56, 28, TextAnchor.MiddleCenter, display: true).raycastTarget = false;
                return; // premade decks can't be changed
            }
            SmallButton(row, "−", x, y - 2, () => { Select(c); RemoveSelected(); });
            Theme.Label(row, $"×{copies}", 17, Theme.Gold, x + 32, y, DeckCardW - 64, 28, TextAnchor.MiddleCenter, display: true).raycastTarget = false;
            SmallButton(row, "+", x + DeckCardW - 32, y - 2, () => { Select(c); AddSelected(); });
        }

        static void SmallButton(Transform parent, string text, float x, float y, Action onClick)
        {
            var bg = Theme.RoundedPanel(parent, new Color(1, 1, 1, 0.1f), x, y, 32, 32, 9, "Btn");
            var b = bg.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => onClick());
            bg.gameObject.AddComponent<HoverFx>().Init(bg, new Color(1, 1, 1, 0.1f), new Color(1, 1, 1, 0.25f), 1.08f);
            var label = Theme.Label(bg.transform, text, 22, Theme.Text, 0, -2, 32, 32, TextAnchor.MiddleCenter);
            label.font = Theme.Body;
        }

        static int TypeOrder(CardType t)
        {
            switch (t)
            {
                case CardType.Legend: return 0;
                case CardType.Weapon: return 1;
                case CardType.Battle: return 2;
                case CardType.Stage: return 3;
                default: return 4;
            }
        }

        static Image Quiet(Image img)
        {
            img.raycastTarget = false;
            return img;
        }

        // ───────────────────────────── Actions ─────────────────────────────

        /// <summary>Premade decks are never changed: says so and returns true.</summary>
        bool RefuseReadOnly()
        {
            if (!ReadOnly) return false;
            _app.Toast(_locked ? "This deck is locked. Beat the bot to unlock it." : "Premade decks can't be changed. Build your own under MY DECKS.");
            return true;
        }

        void AddSelected()
        {
            if (RefuseReadOnly()) return;
            if (_selected == null) { _app.Toast("Select a card first."); return; }
            if (_deckName == null) { _app.Toast("Create a deck first (NEW DECK)."); return; }
            string error = DeckRules.CanAddCard(_deck, _selected.Id);
            if (error != null) { _app.Toast(error); return; }
            _deck.Add(_selected.Id);
            _dirty = true;
            _confirmDelete = false;
            RefreshDeck();
        }

        void RemoveSelected()
        {
            if (RefuseReadOnly()) return;
            if (_selected == null || !_deck.Contains(_selected.Id)) { _app.Toast("That card is not in your deck."); return; }
            _deck.RemoveAt(_deck.LastIndexOf(_selected.Id));
            _dirty = true;
            _confirmDelete = false;
            RefreshDeck();
        }

        void SaveDeck()
        {
            if (_deckName == null) return;
            string error = DeckRules.ValidateForSave(_deck);
            if (error != null) { _app.Toast(error); return; }
            try
            {
                DeckStore.Save(_deckName, _deck);
                _dirty = false;
                _app.Toast($"{_deckName} saved!");
            }
            catch (Exception e)
            {
                _app.Toast($"Error saving deck: {e.Message}");
            }
            RefreshDeck();
        }

        void CreateDeck()
        {
            NamePrompt("NEW DECK", "NAME YOUR DECK", "", "CREATE", typed => CheckNewName(typed), typed =>
            {
                string name = DeckStore.SanitizeName(typed);
                ConfirmUnsaved("CREATE", () =>
                {
                    DeckStore.Save(name, Enumerable.Empty<int>());
                    _deckSelector.SetOptions(DeckStore.List(), name);
                    SwitchDeck(name);
                    _app.Toast($"Created {name}. Add at least {DeckRules.MinimumCardsToSave} cards, including {DeckRules.MinimumEssenceToSave} Essence.");
                });
            });
        }

        /// <summary>Asks for a new name for the open deck. Unsaved changes stay unsaved (only the file moves).</summary>
        void RenameDeck()
        {
            if (_deckName == null) { _app.Toast("Select a deck to rename."); return; }
            string old = _deckName;
            NamePrompt("RENAME DECK", old.ToUpperInvariant(), old, "RENAME", typed =>
            {
                string error = CheckNewName(typed, old);
                return error ?? DeckStore.Rename(old, DeckStore.SanitizeName(typed));
            }, typed =>
            {
                string name = DeckStore.SanitizeName(typed);
                _app.Toast($"Renamed {old} to {name}.");
                _deckName = name;
                _confirmDelete = false;
                _deckSelector.SetOptions(DeckStore.List(), name);
                RememberDeck();
                RefreshDeck();
            });
        }

        void DeleteDeck()
        {
            if (_deckName == null) return;
            if (!_confirmDelete)
            {
                _confirmDelete = true;
                RefreshDeck();
                return;
            }
            DeckStore.Delete(_deckName);
            _app.Toast($"{_deckName} deleted.");
            _deckSelector.SetOptions(DeckStore.List());
            SwitchDeck(_deckSelector.Value);
        }

        void BackToMenu() => ConfirmUnsaved("LEAVE", () => _app.ShowMenu());

        /// <summary>What Esc does while the unsaved-changes dialog is open: the same as KEEP EDITING.</summary>
        Action _cancelModal;

        /// <summary>
        /// Runs <paramref name="proceed"/> straight away when nothing is unsaved. Otherwise asks first: save and go on
        /// (only when the deck can be saved), go on without saving, or keep editing (runs <paramref name="cancel"/>).
        /// </summary>
        void ConfirmUnsaved(string verb, Action proceed, Action cancel = null)
        {
            if (!_dirty || _deckName == null) { proceed(); return; }

            UI.Clear(_modal);
            _modal.SetAsLastSibling();
            void Close() => UI.Clear(_modal);
            void Cancel() { Close(); cancel?.Invoke(); }
            _cancelModal = Cancel;

            var dim = UI.FullPanel(_modal, new Color(Theme.Night.r, Theme.Night.g, Theme.Night.b, 0.72f), "Dim");
            UI.Clicks(dim.gameObject, e => Cancel()); // clicking outside = keep editing

            string saveError = DeckRules.ValidateForSave(_deck);
            const float w = 620;
            float h = saveError == null ? 440 : 380;
            var box = Theme.GlassPanel(dim.rectTransform, 960 - w / 2, 540 - h / 2, w, h, 28, "Unsaved");
            box.color = new Color32(14, 18, 42, 255);
            UI.Clicks(box.gameObject, e => { });
            box.gameObject.AddComponent<SlideIn>().Offset = new Vector2(0, -24);
            Transform t = box.transform;
            var title = Theme.Label(t, "UNSAVED CHANGES", 38, Theme.Text, 0, 26, w, 54, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(title, 3, 0.6f);
            string body = $"{_deckName} has changes that aren't saved yet.";
            if (saveError != null) body += $"\nIt can't be saved yet: {saveError}";
            Theme.Label(t, body, 18, Theme.TextDim, 40, 92, w - 80, 70, TextAnchor.MiddleCenter);

            float y = 178;
            if (saveError == null)
            {
                Theme.Cta(t, $"SAVE AND {verb}", 40, y, w - 80, 62, () =>
                {
                    Close();
                    SaveDeck();
                    if (!_dirty) proceed();
                }, 24);
                y += 76;
            }
            var discard = Theme.Ghost(t, $"{verb} WITHOUT SAVING", 40, y, w - 80, 62, () =>
            {
                Close();
                _dirty = false;
                proceed();
            }, 22);
            discard.GetComponentInChildren<Text>().color = Theme.Danger;
            Theme.Ghost(t, "KEEP EDITING", 40, y + 76, w - 80, 62, Cancel, 22);
        }
    }
}
