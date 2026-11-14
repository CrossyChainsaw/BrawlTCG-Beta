using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace BrawlTCG.UnityApp
{
    public sealed class MatchSetup
    {
        public Game Game;
        /// <summary>Null for local hot-seat games.</summary>
        public NetSession Net;
        /// <summary>The player controlled on this machine (network games).</summary>
        public int LocalIndex;
        public bool HotSeat => Net == null;
    }

    /// <summary>The home screen: animated backdrop, navigation, featured legends, Play Online and Local Match panels.</summary>
    public sealed class MenuScreen : MonoBehaviour
    {
        const string PrefName = "BrawlTCG.Name", PrefName2 = "BrawlTCG.Name2", PrefDeck = "BrawlTCG.Deck",
            PrefDeck2 = "BrawlTCG.Deck2", PrefIp = "BrawlTCG.Ip", PrefPort = "BrawlTCG.Port", PrefMode = "BrawlTCG.OnlineMode";

        enum Panel { None, Online, Local }
        enum OnlineMode { Host, Join, Server }

        App _app;
        Panel _panel = Panel.None;
        OnlineMode _mode;

        // Navigation & showcase
        readonly List<NavItem> _nav = new List<NavItem>();
        RectTransform _showcase;
        CanvasGroup _showcaseGroup;
        Coroutine _showcaseLoop;

        // Profile
        InputField _name;
        Text _avatarInitial;

        // Online panel
        GameObject _onlinePanel;
        ThemedSelector _deck;
        Text _deckInfo, _modeHint, _status;
        InputField _ip, _port;
        RectTransform _ipBox, _portBox;
        readonly List<(OnlineMode mode, Image bg, Text label)> _tabs = new List<(OnlineMode, Image, Text)>();
        Button _cta;
        Text _ctaLabel;
        GameObject _spinner, _cancel;

        // Local panel
        GameObject _localPanel;
        InputField _localName1, _name2;
        ThemedSelector _localDeck1, _deck2;
        Text _localInfo1, _localInfo2;

        // Networking state
        NetSession.Listener _listener;
        NetSession _pending;
        bool _busy, _cancelled;

        // ───────────────────────────── Build ─────────────────────────────

        public void Build(App app)
        {
            _app = app;
            BuildBackdrop();

            var particles = UI.Stretch(UI.Rect("Particles", transform)).gameObject.AddComponent<Particles>();
            particles.Init(46, new Rect(0, 0, App.Width, App.Height), new Color(1f, 0.92f, 0.75f, 1f));

            BuildLogo();
            BuildNav();
            BuildProfile();
            BuildShowcase();
            BuildOnlinePanel();
            BuildLocalPanel();
            BuildFooter();
            ShowPanel(Panel.None);
        }

        void BuildBackdrop()
        {
            RectTransform backdrop = _app.Backdrop;
            Texture2D tex = Resources.Load<Texture2D>("BrawlTCG/UI/MenuBackdrop");
            var pic = UI.Rect("MenuBackdrop", backdrop);
            pic.anchorMin = pic.anchorMax = new Vector2(0.5f, 0.5f);
            pic.pivot = new Vector2(0.5f, 0.5f);
            var raw = pic.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = new Color(0.78f, 0.78f, 0.88f, 1f);
            raw.raycastTarget = false;
            if (tex != null)
            {
                var fitter = pic.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = (float)tex.width / tex.height;
            }
            pic.gameObject.AddComponent<KenBurns>();

            // Darken the left (menu) side, the bottom (footer) and the top edge for legibility.
            var left = UI.Rect("ShadeLeft", backdrop);
            left.anchorMin = new Vector2(0, 0);
            left.anchorMax = new Vector2(0.7f, 1);
            left.offsetMin = left.offsetMax = Vector2.zero;
            var li = left.gameObject.AddComponent<Image>();
            li.sprite = Theme.Gradient(true, 0.96f, 0f);
            li.color = Theme.Night;
            li.raycastTarget = false;

            var bottom = UI.Rect("ShadeBottom", backdrop);
            bottom.anchorMin = new Vector2(0, 0);
            bottom.anchorMax = new Vector2(1, 0.3f);
            bottom.offsetMin = bottom.offsetMax = Vector2.zero;
            var bi = bottom.gameObject.AddComponent<Image>();
            bi.sprite = Theme.Gradient(false, 0.85f, 0f);
            bi.color = Theme.Night;
            bi.raycastTarget = false;

            var top = UI.Rect("ShadeTop", backdrop);
            top.anchorMin = new Vector2(0, 0.82f);
            top.anchorMax = new Vector2(1, 1);
            top.offsetMin = top.offsetMax = Vector2.zero;
            var ti = top.gameObject.AddComponent<Image>();
            ti.sprite = Theme.Gradient(false, 0f, 0.55f);
            ti.color = Theme.Night;
            ti.raycastTarget = false;
        }

        void BuildLogo()
        {
            var logo = Theme.Label(transform, "BRAWL<color=#F5C04A>TCG</color>", 124, Theme.Text, 88, 64, 900, 140, display: true);
            logo.supportRichText = true;
            logo.horizontalOverflow = HorizontalWrapMode.Overflow;
            Theme.AddShadow(logo, 5, 0.55f);
            float width = logo.preferredWidth;

            var beta = Theme.RoundedPanel(transform, new Color(0, 0, 0, 0), 88 + width + 18, 92, 92, 36, 10, "Beta", outline: true);
            beta.color = Theme.Gold;
            Theme.Label(beta.transform, "BETA", 20, Theme.Gold, 0, 0, 92, 36, TextAnchor.MiddleCenter, display: true);

            UI.Panel(transform, Theme.Gold, 96, 214, 56, 4, "Rule").raycastTarget = false;
            Theme.Label(transform, "THE BRAWLHALLA TRADING CARD GAME", 19, Theme.TextDim, 168, 203, 700, 30, display: true);
        }

        void BuildNav()
        {
            var items = new (string title, string subtitle, Action action)[]
            {
                ("PLAY ONLINE", "Host or join a match over the network", () => TogglePanel(Panel.Online)),
                ("LOCAL MATCH", "Two players, one screen", () => TogglePanel(Panel.Local)),
                ("DECK BUILDER", "Build and tune your decks", () => { if (!_busy) _app.ShowDeckBuilder(); }),
                ("QUIT", "See you next brawl", Quit),
            };
            float y = 330;
            for (int i = 0; i < items.Length; i++)
            {
                var item = NavItem.Create(transform, items[i].title, items[i].subtitle, 92, y, 500, 88, items[i].action, i * 0.07f);
                _nav.Add(item);
                y += 100;
            }
        }

        void BuildProfile()
        {
            var chip = Theme.GlassPanel(transform, 1540, 40, 340, 80, 20, "Profile");
            var avatar = Theme.RoundedPanel(chip.transform, Theme.Gold, 14, 14, 52, 52, 26, "Avatar");
            avatar.raycastTarget = false;
            _avatarInitial = Theme.Label(avatar.transform, "?", 28, Theme.Ink, 0, 0, 52, 52, TextAnchor.MiddleCenter, display: true);
            Theme.Label(chip.transform, "PLAYER", 13, Theme.TextDim, 82, 12, 200, 18, display: true);

            var field = UI.Rect("Name", chip.transform);
            UI.Place(field, 78, 28, 250, 40);
            var hit = field.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0);
            var text = Theme.Label(field, "", 26, Theme.Text, 4, 0, 300, 40, TextAnchor.MiddleLeft, display: true);
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ph = Theme.Label(field, "Enter your name", 22, Theme.TextFaint, 4, 0, 300, 40, TextAnchor.MiddleLeft, display: false, style: FontStyle.Italic);
            _name = field.gameObject.AddComponent<InputField>();
            _name.transition = Selectable.Transition.None;
            _name.textComponent = text;
            _name.placeholder = ph;
            _name.caretColor = Theme.Gold;
            _name.customCaretColor = true;
            _name.characterLimit = 24;
            _name.text = PlayerPrefs.GetString(PrefName, "");
            _name.onValueChanged.AddListener(OnProfileNameChanged);
            OnProfileNameChanged(_name.text);
        }

        void OnProfileNameChanged(string value)
        {
            string trimmed = (value ?? "").Trim();
            _avatarInitial.text = trimmed.Length > 0 ? trimmed.Substring(0, 1).ToUpperInvariant() : "?";
            if (_localName1 != null && _localName1.text != value) _localName1.text = value;
        }

        // ───────────────────────────── Featured legend showcase ─────────────────────────────

        void BuildShowcase()
        {
            _showcase = UI.Rect("Showcase", transform);
            UI.Place(_showcase, 960, 120, 900, 860);
            _showcaseGroup = _showcase.gameObject.AddComponent<CanvasGroup>();
            _showcaseGroup.blocksRaycasts = false;
            _showcaseLoop = StartCoroutine(ShowcaseLoop());
        }

        IEnumerator ShowcaseLoop()
        {
            List<LegendCard> legends = CardCatalogue.All.OfType<LegendCard>().ToList();
            var rng = new Random();
            while (true)
            {
                var picks = legends.OrderBy(_ => rng.Next()).Take(3).ToList();
                FillShowcase(picks[0], picks[1], picks[2]);
                for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.6f)
                {
                    _showcaseGroup.alpha = Theme.EaseOut(t);
                    yield return null;
                }
                _showcaseGroup.alpha = 1;
                yield return new WaitForSecondsRealtime(6.5f);
                for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.45f)
                {
                    _showcaseGroup.alpha = 1 - t;
                    yield return null;
                }
                _showcaseGroup.alpha = 0;
            }
        }

        void FillShowcase(LegendCard featured, LegendCard left, LegendCard right)
        {
            UI.Clear(_showcase);
            Color element = CardStyle.FrameColor(featured.Element);

            var glow = UI.Panel(_showcase, new Color(element.r, element.g, element.b, 0.55f), 90, 20, 720, 720, "Glow");
            glow.sprite = Theme.Soft;
            glow.raycastTarget = false;
            var gold = UI.Panel(_showcase, new Color(1f, 0.8f, 0.4f, 0.25f), 230, 150, 440, 440, "GlowGold");
            gold.sprite = Theme.Soft;
            gold.raycastTarget = false;

            ShowcaseCard(left, -230, 50, 1.85f, 11, 0.4f);
            ShowcaseCard(right, 230, 50, 1.85f, -11, 1.9f);
            ShowcaseCard(featured, 0, 0, 2.25f, 0, 0f);

            var scrim = UI.Panel(_showcase, new Color(Theme.Night.r, Theme.Night.g, Theme.Night.b, 0.75f), 50, 600, 800, 290, "Scrim");
            scrim.sprite = Theme.Soft;
            scrim.raycastTarget = false;
            scrim.transform.SetAsFirstSibling();
            Theme.Label(_showcase, "FEATURED LEGEND", 18, Theme.Gold, 0, 668, 900, 26, TextAnchor.MiddleCenter, display: true);
            var name = Theme.Label(_showcase, featured.Name.ToUpperInvariant(), 52, Theme.Text, 0, 694, 900, 64, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(name, 3, 0.6f);

            string attacks = featured.Attacks.Count == 0 ? "No attacks" : string.Join("  ·  ", featured.Attacks.Select(a => a.Name));
            Theme.Label(_showcase, $"{featured.Element}  ·  {featured.BaseHealth} HP  ·  {featured.Power} ATT  ·  {featured.PrimaryWeapon} / {featured.SecondaryWeapon}",
                20, Theme.TextDim, 0, 762, 900, 30, TextAnchor.MiddleCenter);
            Theme.Label(_showcase, attacks, 17, Theme.TextFaint, 0, 792, 900, 28, TextAnchor.MiddleCenter);
        }

        void ShowcaseCard(LegendCard card, float dx, float dy, float scale, float rotation, float phase)
        {
            var wrapper = UI.Rect(card.Name, _showcase);
            wrapper.anchorMin = wrapper.anchorMax = new Vector2(0.5f, 1);
            wrapper.pivot = new Vector2(0.5f, 0.5f);
            wrapper.sizeDelta = new Vector2(CardView.W, CardView.H);
            wrapper.anchoredPosition = new Vector2(dx, -340 - dy);
            wrapper.localScale = new Vector3(scale, scale, 1);

            var shadow = UI.Panel(wrapper, new Color(0, 0, 0, 0.55f), -30, -10, CardView.W + 60, CardView.H + 60, "Shadow");
            shadow.sprite = Theme.Soft;
            shadow.raycastTarget = false;
            shadow.rectTransform.anchoredPosition += new Vector2(8, -14);

            CardView view = CardView.Create(wrapper, card.Clone(), true, 0, 0);
            foreach (Graphic g in view.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;

            var bob = wrapper.gameObject.AddComponent<Bob>();
            bob.BaseRotation = rotation;
            bob.Phase = phase;
            bob.Amplitude = 7;
        }

        // ───────────────────────────── Panels ─────────────────────────────

        void TogglePanel(Panel panel)
        {
            if (_busy && panel != Panel.Online) return;
            ShowPanel(_panel == panel ? Panel.None : panel);
        }

        void ShowPanel(Panel panel)
        {
            _panel = panel;
            _onlinePanel.SetActive(panel == Panel.Online);
            _localPanel.SetActive(panel == Panel.Local);
            _showcase.gameObject.SetActive(panel == Panel.None);
            _nav[0].Selected = panel == Panel.Online;
            _nav[1].Selected = panel == Panel.Local;
            if (panel == Panel.Online) RefreshDeckInfo(_deck, _deckInfo);
            if (panel == Panel.Local)
            {
                RefreshDeckInfo(_localDeck1, _localInfo1);
                RefreshDeckInfo(_deck2, _localInfo2);
            }
        }

        RectTransform PanelShell(string name, string title, string subtitle, float height, out GameObject go)
        {
            var glass = Theme.GlassPanel(transform, 1010, 160, 790, height, 26, name);
            go = glass.gameObject;
            go.AddComponent<SlideIn>();
            Transform t = glass.transform;
            UI.Panel(t, Theme.Gold, 48, 48, 6, 40, "Accent").raycastTarget = false;
            Theme.Label(t, title, 46, Theme.Text, 70, 38, 600, 60, display: true);
            Theme.Label(t, subtitle, 18, Theme.TextDim, 50, 104, 690, 26);
            var close = Theme.Ghost(t, "×", 722, 36, 44, 44, () => ShowPanel(Panel.None), 30);
            close.GetComponentInChildren<Text>().font = Theme.Body; // the display font has no multiplication sign
            return glass.rectTransform;
        }

        void BuildOnlinePanel()
        {
            List<string> decks = DeckStore.List();
            RectTransform t = PanelShell("OnlinePanel", "PLAY ONLINE", "Battle a friend over LAN, the internet or a relay server.", 790, out _onlinePanel);

            _deck = new ThemedSelector(t, "Your deck", 48, 156, 694, decks, PlayerPrefs.GetString(PrefDeck, null));
            _deckInfo = Theme.Label(t, "", 16, Theme.TextDim, 50, 238, 694, 24);
            _deck.Changed += _ => RefreshDeckInfo(_deck, _deckInfo);

            // Segmented mode tabs
            var tabs = Theme.RoundedPanel(t, new Color(1, 1, 1, 0.06f), 48, 284, 694, 58, 14, "Tabs");
            var modes = new[] { (OnlineMode.Host, "HOST"), (OnlineMode.Join, "JOIN"), (OnlineMode.Server, "SERVER") };
            float segW = (694 - 12) / 3f;
            for (int i = 0; i < modes.Length; i++)
            {
                OnlineMode m = modes[i].Item1;
                var seg = Theme.RoundedPanel(tabs.transform, new Color(0, 0, 0, 0), 6 + i * segW, 6, segW, 46, 10, "Tab");
                var b = seg.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => SetMode(m));
                var label = Theme.Label(seg.transform, modes[i].Item2, 22, Theme.TextDim, 0, 0, segW, 46, TextAnchor.MiddleCenter, display: true);
                _tabs.Add((m, seg, label));
            }
            _modeHint = Theme.Label(t, "", 17, Theme.TextDim, 50, 356, 694, 48);

            _ipBox = UI.Place(UI.Rect("IpBox", t), 48, 414, 450, 80);
            _ip = Theme.Field(_ipBox, "Address", "127.0.0.1", PlayerPrefs.GetString(PrefIp, "127.0.0.1"), 0, 0, 450);
            _portBox = UI.Place(UI.Rect("PortBox", t), 522, 414, 220, 80);
            _port = Theme.Field(_portBox, "Port", "5000",
                PlayerPrefs.GetInt(PrefPort, Protocol.DefaultPort).ToString(CultureInfo.InvariantCulture), 0, 0, 220, 5);
            _port.contentType = InputField.ContentType.IntegerNumber;

            _cta = Theme.Cta(t, "HOST GAME", 48, 530, 694, 78, OnlineCtaClicked, 30);
            _ctaLabel = _cta.GetComponentInChildren<Text>();

            _spinner = UI.Panel(t, Theme.Gold, 54, 652, 30, 30, "Spinner").gameObject;
            _spinner.GetComponent<Image>().sprite = Theme.Ring;
            _spinner.GetComponent<Image>().raycastTarget = false;
            _spinner.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            _spinner.GetComponent<RectTransform>().anchoredPosition = new Vector2(69, -667);
            _spinner.AddComponent<Spinner>();
            _status = Theme.Label(t, "", 18, Theme.TextDim, 96, 640, 480, 56, TextAnchor.MiddleLeft);
            _status.verticalOverflow = VerticalWrapMode.Truncate;
            _cancel = Theme.Ghost(t, "CANCEL", 592, 646, 150, 44, CancelConnect, 18).gameObject;

            Theme.Label(t, "Tip: hosting needs port 5000 reachable. On one PC, host in one window and join 127.0.0.1 in the other.",
                15, Theme.TextFaint, 50, 718, 694, 44);

            SetMode((OnlineMode)Mathf.Clamp(PlayerPrefs.GetInt(PrefMode, 0), 0, 2));
            SetBusy(false, "Not connected");
        }

        void SetMode(OnlineMode mode)
        {
            if (_busy) return;
            _mode = mode;
            foreach (var (m, bg, label) in _tabs)
            {
                bool on = m == mode;
                bg.color = on ? Theme.Gold : new Color(0, 0, 0, 0);
                label.color = on ? Theme.Ink : Theme.TextDim;
            }
            _ipBox.gameObject.SetActive(mode != OnlineMode.Host);
            _portBox.anchoredPosition = new Vector2(mode == OnlineMode.Host ? 48 : 522, -414);
            switch (mode)
            {
                case OnlineMode.Host:
                    _modeHint.text = "Wait for a friend to join you. You take the first turn.";
                    _ctaLabel.text = "HOST GAME";
                    break;
                case OnlineMode.Join:
                    _modeHint.text = "Connect directly to a friend who is hosting.";
                    _ctaLabel.text = "JOIN GAME";
                    break;
                default:
                    _modeHint.text = "Both players connect to a running BrawlTCG relay server.";
                    _ctaLabel.text = "CONNECT TO SERVER";
                    break;
            }
        }

        void OnlineCtaClicked()
        {
            if (_busy) return;
            switch (_mode)
            {
                case OnlineMode.Host: HostP2P(); break;
                case OnlineMode.Join: JoinP2P(); break;
                default: JoinServer(); break;
            }
        }

        void BuildLocalPanel()
        {
            List<string> decks = DeckStore.List();
            RectTransform t = PanelShell("LocalPanel", "LOCAL MATCH", "Two players, one screen. Hands are hidden between turns.", 650, out _localPanel);

            var p1 = Theme.RoundedPanel(t, Theme.GlassLight, 48, 156, 316, 290, 18, "Player1");
            Theme.Label(p1.transform, "PLAYER 1", 18, Theme.Gold, 24, 20, 260, 24, display: true);
            _localName1 = Theme.Field(p1.transform, "Name", "Player 1", _name.text, 24, 58, 268, 24);
            _localName1.onValueChanged.AddListener(v => { if (_name.text != v) _name.text = v; });
            _localDeck1 = new ThemedSelector(p1.transform, "Deck", 24, 150, 268, decks, PlayerPrefs.GetString(PrefDeck, null));
            _localInfo1 = Theme.Label(p1.transform, "", 15, Theme.TextDim, 26, 238, 268, 30);
            _localDeck1.Changed += _ => RefreshDeckInfo(_localDeck1, _localInfo1);

            var p2 = Theme.RoundedPanel(t, Theme.GlassLight, 426, 156, 316, 290, 18, "Player2");
            Theme.Label(p2.transform, "PLAYER 2", 18, Theme.Gold, 24, 20, 260, 24, display: true);
            _name2 = Theme.Field(p2.transform, "Name", "Player 2", PlayerPrefs.GetString(PrefName2, "Player 2"), 24, 58, 268, 24);
            _deck2 = new ThemedSelector(p2.transform, "Deck", 24, 150, 268, decks,
                PlayerPrefs.GetString(PrefDeck2, decks.Count > 1 ? decks[1] : null));
            _localInfo2 = Theme.Label(p2.transform, "", 15, Theme.TextDim, 26, 238, 268, 30);
            _deck2.Changed += _ => RefreshDeckInfo(_deck2, _localInfo2);

            var vs = Theme.RoundedPanel(t, Theme.Gold, 365, 271, 60, 60, 30, "VS");
            vs.raycastTarget = false;
            Theme.Label(vs.transform, "VS", 24, Theme.Ink, 0, 0, 60, 60, TextAnchor.MiddleCenter, display: true);

            Theme.Cta(t, "START MATCH", 48, 476, 694, 78, StartHotSeat, 30);
            Theme.Label(t, "Player 1 takes the first turn. The screen is covered between turns so you can pass the device.",
                16, Theme.TextFaint, 50, 574, 694, 48);
        }

        void RefreshDeckInfo(ThemedSelector selector, Text info)
        {
            if (selector.Value == null)
            {
                info.text = "No decks yet. Create one in the Deck Builder.";
                info.color = Theme.Danger;
                return;
            }
            List<int> deck = DeckStore.Load(selector.Value);
            int essence = deck.Count(id => id == 0);
            string error = DeckRules.ValidateForPlay(deck);
            info.text = error == null ? $"{deck.Count} cards  ·  {essence} Essence  ·  Ready to play" : $"{deck.Count} cards  ·  {error}";
            info.color = error == null ? Theme.Success : Theme.Danger;
        }

        void BuildFooter()
        {
            Theme.Label(transform, "v0.1 BETA  ·  UNITY EDITION", 16, Theme.TextDim, 96, 1018, 600, 22, display: true);
            Theme.Label(transform, "Fan-made project. Not affiliated with Blue Mammoth Games. Art credits in CREDITS.txt.", 14, Theme.TextFaint, 96, 1042, 900, 20);
            Theme.Ghost(transform, "DECKS FOLDER", 1660, 1016, 220, 44, OpenDecksFolder, 17);
        }

        // ───────────────────────────── Shared helpers ─────────────────────────────

        void OnDestroy()
        {
            _listener?.Dispose();
            _pending?.Dispose();
        }

        void SetBusy(bool busy, string status = null)
        {
            _busy = busy;
            _spinner.SetActive(busy);
            _cancel.SetActive(busy);
            _cta.interactable = !busy;
            if (status != null) SetStatus(status, busy ? Theme.Text : Theme.TextDim);
        }

        void SetStatus(string s, Color? color = null)
        {
            _status.text = s;
            _status.color = color ?? Theme.TextDim;
        }

        void Error(string s) => SetStatus(s, Theme.Danger);

        void SavePrefs()
        {
            PlayerPrefs.SetString(PrefName, _name.text);
            PlayerPrefs.SetString(PrefName2, _name2.text);
            string d1 = _panel == Panel.Local ? _localDeck1.Value : _deck.Value;
            if (d1 != null) PlayerPrefs.SetString(PrefDeck, d1);
            if (_deck2.Value != null) PlayerPrefs.SetString(PrefDeck2, _deck2.Value);
            PlayerPrefs.SetString(PrefIp, _ip.text);
            PlayerPrefs.SetInt(PrefPort, ParsePort());
            PlayerPrefs.SetInt(PrefMode, (int)_mode);
            PlayerPrefs.Save();
        }

        int ParsePort()
        {
            return int.TryParse(_port.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int p) && p > 0 && p < 65536
                ? p : Protocol.DefaultPort;
        }

        /// <summary>Name + shuffled deck, or null (with a toast) when invalid.</summary>
        (string name, List<int> deck)? PrepareLocalPlayer(string name, ThemedSelector deckSelector)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _app.Toast("Please enter your name.");
                return null;
            }
            if (deckSelector.Value == null)
            {
                _app.Toast("No deck selected. Build one in the Deck Builder first.");
                return null;
            }
            var errors = new List<string>();
            List<int> deck = DeckStore.Load(deckSelector.Value, errors);
            foreach (string e in errors) _app.Toast(e);
            string invalid = DeckRules.ValidateForPlay(deck);
            if (invalid != null)
            {
                _app.Toast(invalid, 5);
                return null;
            }
            return (Protocol.SanitizeName(name), DeckRules.Shuffle(deck, new Random()));
        }

        // ───────────────────────────── Hot-seat ─────────────────────────────

        void StartHotSeat()
        {
            if (_busy) return;
            var p1 = PrepareLocalPlayer(_name.text, _localDeck1);
            if (p1 == null) return;
            var p2 = PrepareLocalPlayer(_name2.text, _deck2);
            if (p2 == null) return;
            SavePrefs();
            var game = new Game(p1.Value.name, p1.Value.deck, p2.Value.name, p2.Value.deck, new Random().Next());
            _app.StartMatch(new MatchSetup { Game = game, Net = null, LocalIndex = 0 });
        }

        // ───────────────────────────── Networking ─────────────────────────────

        async void HostP2P()
        {
            var me = PrepareLocalPlayer(_name.text, _deck);
            if (me == null) return;
            SavePrefs();
            int port = ParsePort();
            SetBusy(true, $"Waiting for a second player on port {port}...");
            NetSession session = null;
            try
            {
                _listener = new NetSession.Listener(port);
                session = await _listener.AcceptAsync();
                _listener = null;
                if (this == null) { session.Dispose(); return; }
                _pending = session;
                SetStatus("Someone is joining...", Theme.Text);

                int mySeed = new Random().Next();
                session.Send(Protocol.EncodeHello(me.Value.name, mySeed, me.Value.deck));
                Protocol.Hello peer = await ReadHello(session);
                if (this == null) { session.Dispose(); return; }

                // Host always starts: host is player 0.
                var game = new Game(me.Value.name, me.Value.deck, peer.Name, peer.Deck, mySeed ^ peer.Seed);
                Launch(game, session, 0);
            }
            catch (Exception e)
            {
                session?.Dispose();
                if (this != null)
                {
                    if (_cancelled) SetStatus("Not connected"); else Error($"Could not host: {e.Message}");
                }
            }
            finally
            {
                _cancelled = false;
                _pending = null;
                _listener?.Dispose();
                _listener = null;
                if (this != null) SetBusy(false);
            }
        }

        async void JoinP2P()
        {
            var me = PrepareLocalPlayer(_name.text, _deck);
            if (me == null) return;
            if (string.IsNullOrWhiteSpace(_ip.text)) { _app.Toast("Enter the host's address."); return; }
            SavePrefs();
            SetBusy(true, $"Joining {_ip.text.Trim()}...");
            NetSession session = null;
            try
            {
                session = await NetSession.ConnectAsync(_ip.text.Trim(), ParsePort());
                if (this == null) { session.Dispose(); return; }
                _pending = session;
                SetStatus("Connected! Exchanging decks...", Theme.Text);

                int mySeed = new Random().Next();
                session.Send(Protocol.EncodeHello(me.Value.name, mySeed, me.Value.deck));
                Protocol.Hello host = await ReadHello(session);
                if (this == null) { session.Dispose(); return; }

                var game = new Game(host.Name, host.Deck, me.Value.name, me.Value.deck, host.Seed ^ mySeed);
                Launch(game, session, 1);
            }
            catch (Exception e)
            {
                session?.Dispose();
                if (this != null)
                {
                    if (_cancelled) SetStatus("Not connected"); else Error($"Could not join: {e.Message}");
                }
            }
            finally
            {
                _cancelled = false;
                _pending = null;
                if (this != null) SetBusy(false);
            }
        }

        async void JoinServer()
        {
            var me = PrepareLocalPlayer(_name.text, _deck);
            if (me == null) return;
            if (string.IsNullOrWhiteSpace(_ip.text)) { _app.Toast("Enter the server's address."); return; }
            SavePrefs();
            SetBusy(true, "Connecting to server...");
            NetSession session = null;
            try
            {
                session = await NetSession.ConnectAsync(_ip.text.Trim(), ParsePort());
                if (this == null) { session.Dispose(); return; }
                _pending = session;
                SetStatus("Connected! Waiting for an opponent...", Theme.Text);

                int mySeed = new Random().Next();
                session.Send(Protocol.EncodeHello(me.Value.name, mySeed, me.Value.deck));
                Protocol.Hello opponent = await ReadHello(session, timeoutMs: 10 * 60 * 1000);
                if (this == null) { session.Dispose(); return; }

                session.Send(Protocol.DistTurnsRequest);
                string dist = await session.ReadLineAsync();
                if (this == null) { session.Dispose(); return; }
                bool amIFirst = dist == Protocol.DistTurnsFirst;
                if (!amIFirst && dist != Protocol.DistTurnsSecond)
                    throw new Exception("Unexpected answer from server: " + dist);

                int seed = mySeed ^ opponent.Seed;
                Game game = amIFirst
                    ? new Game(me.Value.name, me.Value.deck, opponent.Name, opponent.Deck, seed)
                    : new Game(opponent.Name, opponent.Deck, me.Value.name, me.Value.deck, seed);
                Launch(game, session, amIFirst ? 0 : 1);
            }
            catch (Exception e)
            {
                session?.Dispose();
                if (this != null)
                {
                    if (_cancelled) SetStatus("Not connected"); else Error($"Could not connect: {e.Message}");
                }
            }
            finally
            {
                _cancelled = false;
                _pending = null;
                if (this != null) SetBusy(false);
            }
        }

        static async Task<Protocol.Hello> ReadHello(NetSession session, int timeoutMs = 20000)
        {
            string line = await session.ReadLineAsync(timeoutMs);
            if (!Protocol.TryDecodeHello(line, out Protocol.Hello hello, out string error))
                throw new Exception(error);
            return hello;
        }

        void Launch(Game game, NetSession session, int localIndex)
        {
            _pending = null; // ownership moves to the match
            _app.StartMatch(new MatchSetup { Game = game, Net = session, LocalIndex = localIndex });
        }

        void CancelConnect()
        {
            _cancelled = true;
            _listener?.Dispose();
            _pending?.Dispose();
            SetStatus("Not connected");
        }

        void OpenDecksFolder()
        {
            System.IO.Directory.CreateDirectory(DeckStore.Folder);
            Application.OpenURL("file:///" + DeckStore.Folder.Replace('\\', '/'));
            _app.Toast("Decks folder: " + DeckStore.Folder, 6);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>A large animated navigation entry: accent bar, title, subtitle, hover slide.</summary>
    public sealed class NavItem : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerClickHandler
    {
        Image _bg, _accent;
        RectTransform _content;
        Text _title, _arrow;
        CanvasGroup _group;
        Action _onClick;
        bool _over;
        float _t, _intro, _delay;

        public bool Selected { get; set; }

        public static NavItem Create(Transform parent, string title, string subtitle, float x, float y, float w, float h, Action onClick, float delay)
        {
            var bg = Theme.RoundedPanel(parent, new Color(1, 1, 1, 0), x, y, w, h, 16, "Nav " + title);
            var item = bg.gameObject.AddComponent<NavItem>();
            item._bg = bg;
            item._onClick = onClick;
            item._delay = Time.unscaledTime + delay;
            item._group = bg.gameObject.AddComponent<CanvasGroup>();

            item._accent = UI.Panel(bg.transform, Theme.Gold, 0, h * 0.5f - 2, 5, 4, "Accent");
            item._accent.raycastTarget = false;

            item._content = UI.Place(UI.Rect("Content", bg.transform), 0, 0, w, h);
            item._title = Theme.Label(item._content, title, 40, Theme.Text, 30, 10, w - 80, 48, TextAnchor.MiddleLeft, display: true);
            Theme.AddShadow(item._title, 2, 0.5f);
            Theme.Label(item._content, subtitle, 17, Theme.TextDim, 32, 54, w - 80, 24, TextAnchor.MiddleLeft);
            item._arrow = Theme.Label(item._content, "›", 56, Theme.Gold, w - 64, 8, 50, 70, TextAnchor.MiddleCenter);
            return item;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) => _over = true;
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) => _over = false;
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e)
        {
            if (e.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left) _onClick?.Invoke();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _intro = Mathf.Min(1, _intro + dt / 0.5f * (Time.unscaledTime > _delay ? 1 : 0));
            float ie = Theme.EaseOut(_intro);
            _group.alpha = ie;

            float goal = _over || Selected ? 1 : 0;
            _t = Mathf.MoveTowards(_t, goal, dt * 7);
            float e = Theme.EaseOut(_t);

            _bg.color = new Color(1, 1, 1, 0.09f * e);
            float h = _bg.rectTransform.sizeDelta.y;
            float barH = Mathf.Lerp(4, h - 24, e);
            _accent.rectTransform.sizeDelta = new Vector2(5, barH);
            _accent.color = new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, e);
            _accent.rectTransform.anchoredPosition = new Vector2(0, -(h - barH) / 2);
            _content.anchoredPosition = new Vector2(Mathf.Lerp(-30, 0, ie) + 14 * e, 0);
            _title.color = Color.Lerp(Theme.Text, Theme.Gold, Selected ? 1 : e * 0.35f);
            _arrow.color = new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, e);
        }
    }
}
