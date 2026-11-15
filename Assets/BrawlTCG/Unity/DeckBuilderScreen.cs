using System;
using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>The deck builder (WinForms FRM_DeckBuilder): browse the collection, preview, add/remove copies, save decks.</summary>
    public sealed class DeckBuilderScreen : MonoBehaviour
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
        InputField _newDeckName;
        Button _save;
        Text _deleteLabel, _deckTitle, _status;
        RectTransform _stats;
        ScrollList _deckList;
        List<int> _deck = new List<int>();
        string _deckName;
        bool _dirty, _confirmDelete;

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

            BuildCollection();
            BuildPreview();
            BuildDeckPanel();

            ApplyFilters();
            SwitchDeck(_deckSelector.Value);
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

            _resultCount = Theme.Label(t, "", 15, Theme.TextDim, 20, 204, 600, 22, display: true);
            Theme.Label(t, "Click to preview  ·  double-click to add", 15, Theme.TextFaint, w - 420, 204, 400, 22, TextAnchor.MiddleRight);

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
                    (query.Length == 0 || c.Name.ToLowerInvariant().Contains(query) || c.Description.ToLowerInvariant().Contains(query));
                cell.Root.SetActive(visible);
                if (visible) shown++;
            }
            _resultCount.text = $"{shown} CARD{(shown == 1 ? "" : "S")}";

            foreach (var (key, bg, label) in _typeChips) SetChip(bg, label, key == _typeFilter);
            foreach (var (element, bg, label) in _elementChips) SetChip(bg, label, element == _elementFilter);
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

            var add = Theme.Cta(t, "ADD", 20, 492, (w - 52) / 2, 56, AddSelected, 24);
            add.interactable = _deckName != null && DeckRules.CanAddCard(_deck, card.Id) == null;
            var remove = Theme.Ghost(t, "REMOVE", 32 + (w - 52) / 2, 492, (w - 52) / 2, 56, RemoveSelected, 20);
            remove.interactable = copies > 0;

            float y = 568;
            if (card is LegendCard legend)
            {
                Theme.Label(t, $"{legend.BaseHealth} HP  ·  {legend.Power} ATK  ·  {Pretty(legend.PrimaryWeapon)} / {Pretty(legend.SecondaryWeapon)}", 15, Theme.TextDim, 20, y, w - 40, 22, TextAnchor.MiddleLeft, display: true);
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
                string effects = CleanEffects(legend.EffectDescription);
                if (effects.Length > 0)
                {
                    Theme.Label(t, "ABILITIES", 14, Theme.Gold, 20, y + 4, 200, 20, display: true);
                    var fx = Theme.Label(t, effects, 15, Theme.Text, 20, y + 28, w - 40, PreviewRect.height - y - 40);
                    fx.verticalOverflow = VerticalWrapMode.Truncate;
                    fx.resizeTextForBestFit = true;
                    fx.resizeTextMinSize = 10;
                    fx.resizeTextMaxSize = 15;
                }
            }
            else
            {
                string text = card is EssenceCard
                    ? "Each Essence card in play gives you 1 Essence at the start of your turn. Play one per turn. Decks need at least 10."
                    : CleanEffects(card.Description);
                if (card is WeaponCard weapon) text += $"\n\nUnlocks attacks that need a {Pretty(weapon.Weapon)}. Matching the legend's element adds bonus damage.";
                if (card is BattleCard battle)
                {
                    string target = !battle.TargetRequired ? "No target needed." : battle.FriendlyFire ? "Target: one of your legends." : "Target: an enemy legend.";
                    text += $"\n\n{target}  {(battle.Stackable ? "Stays attached to the legend." : "Discarded after use.")}";
                }
                var desc = Theme.Label(t, text, 17, Theme.Text, 20, y, w - 40, PreviewRect.height - y - 24);
                desc.verticalOverflow = VerticalWrapMode.Truncate;
                desc.resizeTextForBestFit = true;
                desc.resizeTextMinSize = 11;
                desc.resizeTextMaxSize = 17;
            }
        }

        static void PreviewAttack(Transform t, LegendCard legend, Attack attack, float x, float y, float w)
        {
            var row = Theme.RoundedPanel(t, new Color(1, 1, 1, 0.06f), x, y, w, 54, 12, "Attack");
            row.raycastTarget = false;
            Transform rt = row.transform;
            float ix = 8;
            ix = ReqIcon(rt, attack.WeaponOne, attack.WeaponOneAmount, attack.WeaponOneBurnAmount, ix);
            if (attack.WeaponTwo != null) ix = ReqIcon(rt, attack.WeaponTwo.Value, attack.WeaponTwoAmount, attack.WeaponTwoBurnAmount, ix);
            var name = Theme.Label(rt, attack.Name, 18, Theme.Text, ix + 4, 4, w - ix - 70, 26, TextAnchor.MiddleLeft, display: true);
            name.verticalOverflow = VerticalWrapMode.Truncate;
            var tags = new List<string>();
            if (attack.Kind != AttackKind.Strike) tags.Add(attack.Kind.ToString().ToUpperInvariant());
            if (attack.InstaEffect) tags.Add("INSTANT");
            if (attack.MultiHit) tags.Add("HITS ALL");
            if (attack.FriendlyFire) tags.Add("ALLY");
            int burns = attack.WeaponOneBurnAmount + attack.WeaponTwoBurnAmount;
            if (burns > 0) tags.Add($"BURNS {burns}");
            Theme.Label(rt, tags.Count > 0 ? string.Join(" · ", tags) : "", 12, Theme.TextDim, ix + 4, 30, w - ix - 70, 18, TextAnchor.MiddleLeft, display: true);
            string damage = attack.OneHitKO ? "KO" : attack.AttackModifier <= -1000 ? "—" : Attack.CalculateBaseDamage(legend, attack).ToString();
            Theme.Label(rt, damage, 28, Theme.Gold, w - 70, 0, 60, 54, TextAnchor.MiddleRight, display: true);
        }

        static float ReqIcon(Transform parent, Weapon weapon, int amount, int burn, float x)
        {
            var chip = Theme.RoundedPanel(parent, new Color(0, 0, 0, 0.35f), x, 7, 58, 40, 10, "Req");
            chip.raycastTarget = false;
            Texture2D icon = Art.Weapon(weapon);
            if (icon != null) UI.Picture(chip.transform, icon, 5, 5, 30, 30, cover: false);
            else Theme.Label(chip.transform, "ANY", 11, Theme.Text, 0, 0, 36, 40, TextAnchor.MiddleCenter, display: true);
            Theme.Label(chip.transform, amount == 0 ? "—" : $"×{amount}", 14, burn > 0 ? new Color32(255, 150, 70, 255) : Theme.Gold, 32, 0, 24, 40, TextAnchor.MiddleCenter, display: true);
            return x + 64;
        }

        static string CleanEffects(string description)
        {
            return string.Join("\n", (description ?? "").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.EndsWith("N/A")));
        }

        static string Pretty(Weapon w) => w == Weapon.RocketLance ? "Rocket Lance" : w == Weapon.BattleBoots ? "Battle Boots" : w.ToString();

        // ───────────────────────────── Deck panel ─────────────────────────────

        void BuildDeckPanel()
        {
            var panel = Theme.GlassPanel(transform, DeckRect.x, DeckRect.y, DeckRect.width, DeckRect.height, 24, "DeckPanel");
            panel.color = new Color32(12, 16, 38, 238);
            Transform t = panel.transform;
            float w = DeckRect.width;

            _deckSelector = new ThemedSelector(t, "Your deck", 18, 16, w - 36, DeckStore.List());
            _deckSelector.Changed += name =>
            {
                if (_dirty) _app.Toast($"Unsaved changes to {_deckName} were discarded.");
                SwitchDeck(name);
            };

            _newDeckName = Theme.Field(t, null, "New deck name", "", 18, 104, w - 36 - 140, 32);
            Theme.Ghost(t, "NEW DECK", w - 18 - 130, 104, 130, 52, CreateDeck, 18);

            _save = Theme.Cta(t, "SAVE", 18, 170, 220, 56, SaveDeck, 24);
            var delete = Theme.Ghost(t, "DELETE", 248, 170, w - 266, 56, DeleteDeck, 18);
            _deleteLabel = delete.GetComponentInChildren<Text>();

            _deckTitle = Theme.Label(t, "", 22, Theme.Text, 18, 240, w - 36, 30, display: true);
            _deckTitle.verticalOverflow = VerticalWrapMode.Truncate;
            _stats = UI.Place(UI.Rect("Stats", t), 18, 274, w - 36, 160);
            _status = Theme.Label(t, "", 15, Theme.TextDim, 18, 436, w - 36, 40);

            _deckList = new ScrollList(t, 10, 480, w - 20, DeckRect.height - 490, new Color(0, 0, 0, 0));
            _deckList.Scroll.scrollSensitivity = 40;
        }

        void SwitchDeck(string name)
        {
            _deckName = name;
            _confirmDelete = false;
            var errors = new List<string>();
            _deck = name != null ? DeckStore.Load(name, errors) : new List<int>();
            foreach (string e in errors) _app.Toast(e);
            _dirty = false;
            RefreshDeck();
        }

        void RefreshDeck()
        {
            RefreshCopyBadges();
            if (_selected != null) ShowPreview();

            _deckTitle.text = _deckName == null ? "NO DECK SELECTED" : _deckName.ToUpperInvariant() + (_dirty ? "  ·  UNSAVED" : "");
            _deckTitle.color = _dirty ? Theme.Gold : Theme.Text;

            string saveError = _deckName == null ? "Create a deck first." : DeckRules.ValidateForSave(_deck);
            _save.interactable = saveError == null && _dirty;
            _deleteLabel.text = _confirmDelete ? "CONFIRM DELETE?" : "DELETE";
            _deleteLabel.color = _confirmDelete ? Theme.Danger : Theme.Text;

            string playError = DeckRules.ValidateForPlay(_deck);
            if (_deckName == null) { _status.text = "Create a deck with NEW DECK to start building."; _status.color = Theme.TextDim; }
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
            var groups = _deck.Distinct()
                .Select(id => CardCatalogue.Get(id))
                .GroupBy(c => c.Type)
                .OrderBy(g => TypeOrder(g.Key));
            foreach (var group in groups)
            {
                int total = group.Sum(c => _deck.Count(id => id == c.Id));
                var header = _deckList.AddRow(34, new Color(0, 0, 0, 0), null);
                Theme.Label(header.transform, $"{TypeName(group.Key)}  ({total})", 14, Theme.Gold, 10, 0, 300, 34, TextAnchor.MiddleLeft, display: true).raycastTarget = false;
                foreach (Card card in group.OrderBy(c => c.Cost).ThenBy(c => c.Name))
                    DeckRow(card, _deck.Count(id => id == card.Id));
            }
            if (_deck.Count == 0)
            {
                var empty = _deckList.AddRow(80, new Color(0, 0, 0, 0), null);
                Theme.Label(empty.transform, "This deck is empty. Add cards from the collection.", 16, Theme.TextFaint, 10, 0, DeckRect.width - 60, 80, TextAnchor.MiddleCenter);
            }
        }

        void DeckRow(Card card, int copies)
        {
            Card c = card;
            float w = DeckRect.width - 44;
            var row = _deckList.AddRow(46, c == _selected ? new Color(1, 1, 1, 0.14f) : new Color(1, 1, 1, 0.05f), () => Select(c));
            row.sprite = Theme.Rounded;
            row.type = Image.Type.Sliced;
            row.pixelsPerUnitMultiplier = 2;
            Transform t = row.transform;
            Quiet(Theme.RoundedPanel(t, CardStyle.FrameColor(card.Element), 0, 0, 6, 46, 3, "Element"));
            var gem = Quiet(Theme.RoundedPanel(t, new Color32(58, 46, 150, 255), 14, 9, 28, 28, 14, "Cost"));
            Theme.Label(gem.transform, card.Cost.ToString(), 15, Color.white, 0, 0, 28, 28, TextAnchor.MiddleCenter, display: true);
            var name = Theme.Label(t, card.Name, 17, Theme.Text, 52, 0, w - 52 - 130, 46, TextAnchor.MiddleLeft, display: true);
            name.verticalOverflow = VerticalWrapMode.Truncate;
            Theme.Label(t, $"×{copies}", 18, Theme.Gold, w - 130, 0, 44, 46, TextAnchor.MiddleRight, display: true);
            SmallButton(t, "−", w - 78, 7, () => { Select(c); RemoveSelected(); });
            SmallButton(t, "+", w - 40, 7, () => { Select(c); AddSelected(); });
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

        static string TypeName(CardType t)
        {
            switch (t)
            {
                case CardType.Legend: return "LEGENDS";
                case CardType.Weapon: return "WEAPONS";
                case CardType.Battle: return "BATTLE CARDS";
                case CardType.Stage: return "STAGES";
                default: return "ESSENCE";
            }
        }

        static Image Quiet(Image img)
        {
            img.raycastTarget = false;
            return img;
        }

        // ───────────────────────────── Actions ─────────────────────────────

        void AddSelected()
        {
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
            if (string.IsNullOrWhiteSpace(_newDeckName.text)) { _app.Toast("Enter a name for the new deck."); return; }
            string name = DeckStore.SanitizeName(_newDeckName.text);
            if (DeckStore.Exists(name)) { _app.Toast("A deck with that name already exists."); return; }
            DeckStore.Save(name, Enumerable.Empty<int>());
            _newDeckName.text = "";
            _deckSelector.SetOptions(DeckStore.List(), name);
            SwitchDeck(name);
            _app.Toast($"Created {name}. Add at least 40 cards, including 10 Essence.");
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

        void BackToMenu()
        {
            if (_dirty) _app.Toast("Unsaved changes were discarded.");
            _app.ShowMenu();
        }
    }
}
