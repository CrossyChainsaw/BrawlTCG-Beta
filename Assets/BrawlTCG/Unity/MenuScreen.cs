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
        /// <summary>Avatar (legend card id or -1) per player index; null = initials for everyone.</summary>
        public int[] Avatars;
        /// <summary>Showcase clip to play (Game is its staged position); null for real matches.</summary>
        public ShowcaseClip Showcase;
        /// <summary>Bot games: starts another match with the same settings (the result screen's PLAY AGAIN).</summary>
        public Action Rematch;
        /// <summary>Bot games: the premade deck the bot plays. Beating it unlocks that deck (see <see cref="Collection"/>).</summary>
        public string BotDeck;
        /// <summary>A coin flip decided who starts: the intro shows the coin being tossed (#33).</summary>
        public bool CoinFlip;
        /// <summary>Hot seat: the coin flip made Player 2 the Game's first player (index 0), so seat numbers are swapped.</summary>
        public bool SeatsSwapped;
        public bool HotSeat => Net == null && Showcase == null;
    }

    /// <summary>The home screen: animated backdrop, navigation, featured legends, Play Online and Local Match panels.</summary>
    public sealed class MenuScreen : MonoBehaviour
    {
        const string PrefName = "BrawlTCG.Name", PrefName2 = "BrawlTCG.Name2", PrefDeck = "BrawlTCG.Deck",
            PrefDeck2 = "BrawlTCG.Deck2", PrefIp = "BrawlTCG.Ip", PrefPort = "BrawlTCG.Port", PrefMode = "BrawlTCG.OnlineMode";

        enum Panel { None, Online, Bot, Local }
        const string PrefCoinFlip = "BrawlTCG.LocalCoinFlip", PrefOnlineCoinFlip = "BrawlTCG.OnlineCoinFlip";
        const string PrefBotMyDeck = "BrawlTCG.BotMyDeck", PrefBotLevel = "BrawlTCG.BotLevel", PrefBotDeck = "BrawlTCG.BotDeck";
        const string BotDeckRandom = "Random bot deck";
        enum OnlineMode { CreateRoom, JoinRoom, LanHost, LanJoin }

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
        Image _avatar;
        RectTransform _pickerLayer;
        RectTransform _deckPickerLayer;

        // Online panel
        GameObject _onlinePanel;
        ThemedSelector _deck;
        Text _deckInfo, _modeHint, _status;
        InputField _ip, _port, _code;
        RectTransform _ipBox, _portBox, _codeBox, _roomBox;
        Text _roomCode;
        readonly List<(OnlineMode mode, Image bg, Text label)> _tabs = new List<(OnlineMode, Image, Text)>();
        Button _cta;
        Text _ctaLabel;
        GameObject _spinner, _cancel;
        RectTransform _onlineCoinRow;
        bool _onlineCoinFlip;

        // Local panel
        GameObject _localPanel;
        InputField _localName1, _name2;
        ThemedSelector _localDeck1, _deck2;
        Text _localInfo1, _localInfo2;
        RectTransform _coinFlipRow;
        bool _coinFlip;

        // Bot panel
        GameObject _botPanel;
        ThemedSelector _botMyDeck, _botLevel, _botDeck;
        Text _botMyInfo, _botPool;
        GameObject _botMystery, _botChooser;
        NavItem _navOnline, _navBot, _navLocal;

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
            BuildBotPanel();
            BuildFooter();
            _deckPickerLayer = UI.Stretch(UI.Rect("DeckPickerLayer", transform)); // ALL DECKS grid of the deck pickers
            _pickerLayer = UI.Stretch(UI.Rect("AvatarPickerLayer", transform));
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
                ("VS BOT", "No one around? Battle the computer", () => TogglePanel(Panel.Bot)),
                ("LOCAL MATCH", "Two players, one screen", () => TogglePanel(Panel.Local)),
                ("DECK BUILDER", "Build and tune your decks", () => { if (!_busy) _app.ShowDeckBuilder(); }),
                ("QUIT", "See you next brawl", Quit),
            };
            float y = 310;
            for (int i = 0; i < items.Length; i++)
            {
                var item = NavItem.Create(transform, items[i].title, items[i].subtitle, 92, y, 500, 88, items[i].action, i * 0.07f);
                _nav.Add(item);
                y += 100;
            }
            _navOnline = _nav[0];
            _navBot = _nav[1];
            _navLocal = _nav[2];
        }

        void BuildProfile()
        {
            var chip = Theme.GlassPanel(transform, 1540, 40, 340, 80, 20, "Profile");
            // Click the avatar to pick a legend portrait (#44).
            _avatar = Theme.RoundedPanel(chip.transform, Theme.Gold, 14, 14, 52, 52, 26, "Avatar");
            var pick = _avatar.gameObject.AddComponent<Button>();
            pick.transition = Selectable.Transition.None;
            pick.onClick.AddListener(OpenAvatarPicker);
            _avatar.gameObject.AddComponent<HoverFx>().Init(_avatar, Theme.Gold, new Color32(255, 214, 120, 255), 1.1f);
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
            RefreshAvatar();
            if (_localName1 != null && _localName1.text != value) _localName1.text = value;
        }

        void RefreshAvatar() => Avatars.Paint(_avatar, 52, Avatars.Saved, _name.text, 28, Theme.Ink);

        void OpenAvatarPicker()
        {
            if (_busy) return;
            Avatars.OpenPicker(_pickerLayer, Avatars.Saved, _name.text, id =>
            {
                Avatars.Saved = id;
                RefreshAvatar();
            });
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
            Theme.Label(_showcase, $"{featured.Element}  ·  {featured.BaseHealth} HP  ·  {featured.Power} ATT  ·  {featured.WeaponText()}",
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
            _botPanel.SetActive(panel == Panel.Bot);
            _showcase.gameObject.SetActive(panel == Panel.None);
            _navOnline.Selected = panel == Panel.Online;
            _navBot.Selected = panel == Panel.Bot;
            _navLocal.Selected = panel == Panel.Local;
            if (panel == Panel.Bot)
            {
                RefreshDeckInfo(_botMyDeck, _botMyInfo);
                RefreshBotPool();
            }
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
            List<string> decks = DeckLibrary.Playable();
            RectTransform t = PanelShell("OnlinePanel", "PLAY ONLINE", "Battle a friend anywhere with a room code, or directly over LAN.", 790, out _onlinePanel);

            _deck = new ThemedSelector(t, "Your deck", 48, 156, 694, decks, PlayerPrefs.GetString(PrefDeck, null));
            _deckInfo = Theme.Label(t, "", 16, Theme.TextDim, 50, 238, 694, 24);
            _deck.Changed += _ => RefreshDeckInfo(_deck, _deckInfo);
            DeckPickerButton(t, _deck, _deckInfo, 612, 150, 130);

            // Segmented mode tabs
            var tabs = Theme.RoundedPanel(t, new Color(1, 1, 1, 0.06f), 48, 284, 694, 58, 14, "Tabs");
            var modes = new[]
            {
                (OnlineMode.CreateRoom, "CREATE ROOM"), (OnlineMode.JoinRoom, "JOIN ROOM"),
                (OnlineMode.LanHost, "LAN HOST"), (OnlineMode.LanJoin, "LAN JOIN"),
            };
            float segW = (694 - 12) / (float)modes.Length;
            for (int i = 0; i < modes.Length; i++)
            {
                OnlineMode m = modes[i].Item1;
                var seg = Theme.RoundedPanel(tabs.transform, new Color(0, 0, 0, 0), 6 + i * segW, 6, segW, 46, 10, "Tab");
                var b = seg.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => SetMode(m));
                var label = Theme.Label(seg.transform, modes[i].Item2, 18, Theme.TextDim, 0, 0, segW, 46, TextAnchor.MiddleCenter, display: true);
                _tabs.Add((m, seg, label));
            }
            _modeHint = Theme.Label(t, "", 17, Theme.TextDim, 50, 356, 694, 48);

            _ipBox = UI.Place(UI.Rect("IpBox", t), 48, 414, 450, 80);
            _ip = Theme.Field(_ipBox, "Address", "127.0.0.1", PlayerPrefs.GetString(PrefIp, "127.0.0.1"), 0, 0, 450);
            _portBox = UI.Place(UI.Rect("PortBox", t), 522, 414, 220, 80);
            _port = Theme.Field(_portBox, "Port", "5000",
                PlayerPrefs.GetInt(PrefPort, Protocol.DefaultPort).ToString(CultureInfo.InvariantCulture), 0, 0, 220, 5);
            _port.contentType = InputField.ContentType.IntegerNumber;

            _codeBox = UI.Place(UI.Rect("CodeBox", t), 48, 414, 300, 80);
            _code = Theme.Field(_codeBox, "Room code", "0000", "", 0, 0, 300, 4);
            _code.contentType = InputField.ContentType.IntegerNumber;
            // Enter in the room code (or LAN address) field joins right away (#38).
            _code.onSubmit.AddListener(_ => { if (_mode == OnlineMode.JoinRoom) OnlineCtaClicked(); });
            _ip.onSubmit.AddListener(_ => { if (_mode == OnlineMode.LanJoin) OnlineCtaClicked(); });

            _roomBox = UI.Place(UI.Rect("RoomBox", t), 48, 404, 694, 110);
            Theme.Label(_roomBox, "ROOM CODE  ·  SEND THIS TO YOUR FRIEND", 14, Theme.TextDim, 2, 0, 694, 20, display: true);
            _roomCode = Theme.Label(_roomBox, "", 72, Theme.Gold, 0, 22, 694, 86, TextAnchor.MiddleLeft, display: true);
            _roomBox.gameObject.SetActive(false);

            // Hosts only: a coin flip decides who goes first (#33). Right of the room code / port, so both stay visible.
            _onlineCoinFlip = PlayerPrefs.GetInt(PrefOnlineCoinFlip, 0) == 1;
            _onlineCoinRow = UI.Place(UI.Rect("CoinFlip", t), 400, 446, 342, 34);
            DrawOnlineCoinRow();

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

            Theme.Label(t, "Rooms work over the internet without opening ports. LAN modes need port 5000 reachable; " +
                "on one PC, LAN host in one window and LAN join 127.0.0.1 in the other.",
                15, Theme.TextFaint, 50, 718, 694, 44);

            SetMode((OnlineMode)Mathf.Clamp(PlayerPrefs.GetInt(PrefMode, 0), 0, 3));
            SetBusy(false, "Not connected");
        }

        void DrawOnlineCoinRow()
        {
            UI.Clear(_onlineCoinRow);
            MatchScreen.Switch(_onlineCoinRow, 0, 1, _onlineCoinFlip, () =>
            {
                if (_busy) return; // the guest has already been told (or is about to be)
                _onlineCoinFlip = !_onlineCoinFlip;
                PlayerPrefs.SetInt(PrefOnlineCoinFlip, _onlineCoinFlip ? 1 : 0);
                PlayerPrefs.Save();
                DrawOnlineCoinRow();
                SetMode(_mode); // the hint says who goes first
            });
            Theme.Label(_onlineCoinRow, "Coin flip for the first turn", 17, Theme.TextDim, 88, 0, 254, 34, TextAnchor.MiddleLeft);
        }

        string HostFirstHint => _onlineCoinFlip ? "A coin flip decides who takes the first turn." : "You take the first turn.";

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
            _ipBox.gameObject.SetActive(mode == OnlineMode.LanJoin);
            _portBox.gameObject.SetActive(mode == OnlineMode.LanHost || mode == OnlineMode.LanJoin);
            _portBox.anchoredPosition = new Vector2(mode == OnlineMode.LanHost ? 48 : 522, -414);
            _codeBox.gameObject.SetActive(mode == OnlineMode.JoinRoom);
            _onlineCoinRow.gameObject.SetActive(mode == OnlineMode.CreateRoom || mode == OnlineMode.LanHost);
            switch (mode)
            {
                case OnlineMode.CreateRoom:
                    _modeHint.text = "Get a 4-digit room code and send it to a friend. " + HostFirstHint;
                    _ctaLabel.text = "CREATE ROOM";
                    break;
                case OnlineMode.JoinRoom:
                    _modeHint.text = "Enter the 4-digit code your friend got when they created a room.";
                    _ctaLabel.text = "JOIN ROOM";
                    break;
                case OnlineMode.LanHost:
                    _modeHint.text = "Host directly on this PC. Your friend needs your IP address. " + HostFirstHint;
                    _ctaLabel.text = "HOST GAME";
                    break;
                default:
                    _modeHint.text = "Connect directly to a friend who is hosting with LAN HOST.";
                    _ctaLabel.text = "JOIN GAME";
                    break;
            }
        }

        void OnlineCtaClicked()
        {
            if (_busy) return;
            switch (_mode)
            {
                case OnlineMode.CreateRoom: HostRoom(); break;
                case OnlineMode.JoinRoom: JoinRoom(); break;
                case OnlineMode.LanHost: HostP2P(); break;
                default: JoinP2P(); break;
            }
        }

        void BuildLocalPanel()
        {
            List<string> decks = DeckLibrary.Playable();
            RectTransform t = PanelShell("LocalPanel", "LOCAL MATCH", "Two players, one screen. Hands are hidden between turns.", 650, out _localPanel);

            var p1 = Theme.RoundedPanel(t, Theme.GlassLight, 48, 156, 316, 290, 18, "Player1");
            Theme.Label(p1.transform, "PLAYER 1", 18, Theme.Gold, 24, 20, 260, 24, display: true);
            _localName1 = Theme.Field(p1.transform, "Name", "Player 1", _name.text, 24, 58, 268, 24);
            _localName1.onValueChanged.AddListener(v => { if (_name.text != v) _name.text = v; });
            _localDeck1 = new ThemedSelector(p1.transform, "Deck", 24, 150, 268, decks, PlayerPrefs.GetString(PrefDeck, null));
            _localInfo1 = Theme.Label(p1.transform, "", 15, Theme.TextDim, 26, 238, 268, 30);
            _localDeck1.Changed += _ => RefreshDeckInfo(_localDeck1, _localInfo1);
            DeckPickerButton(p1.transform, _localDeck1, _localInfo1, 172, 144, 120);

            var p2 = Theme.RoundedPanel(t, Theme.GlassLight, 426, 156, 316, 290, 18, "Player2");
            Theme.Label(p2.transform, "PLAYER 2", 18, Theme.Gold, 24, 20, 260, 24, display: true);
            _name2 = Theme.Field(p2.transform, "Name", "Player 2", PlayerPrefs.GetString(PrefName2, "Player 2"), 24, 58, 268, 24);
            _deck2 = new ThemedSelector(p2.transform, "Deck", 24, 150, 268, decks,
                PlayerPrefs.GetString(PrefDeck2, decks.Count > 1 ? decks[1] : null));
            _localInfo2 = Theme.Label(p2.transform, "", 15, Theme.TextDim, 26, 238, 268, 30);
            _deck2.Changed += _ => RefreshDeckInfo(_deck2, _localInfo2);
            DeckPickerButton(p2.transform, _deck2, _localInfo2, 172, 144, 120);

            var vs = Theme.RoundedPanel(t, Theme.Gold, 365, 271, 60, 60, 30, "VS");
            vs.raycastTarget = false;
            Theme.Label(vs.transform, "VS", 24, Theme.Ink, 0, 0, 60, 60, TextAnchor.MiddleCenter, display: true);

            Theme.Cta(t, "START MATCH", 48, 476, 694, 78, StartHotSeat, 30);
            _coinFlip = PlayerPrefs.GetInt(PrefCoinFlip, 0) == 1;
            _coinFlipRow = UI.Place(UI.Rect("CoinFlip", t), 48, 570, 694, 34);
            DrawCoinFlipRow();
            Theme.Label(t, "The screen is covered between turns so you can pass the device.", 16, Theme.TextFaint, 50, 612, 694, 24);
        }

        /// <summary>The Local Match coin-flip switch (#33): on, a coin flip decides who takes the first turn.</summary>
        void DrawCoinFlipRow()
        {
            UI.Clear(_coinFlipRow);
            MatchScreen.Switch(_coinFlipRow, 0, 1, _coinFlip, () =>
            {
                _coinFlip = !_coinFlip;
                PlayerPrefs.SetInt(PrefCoinFlip, _coinFlip ? 1 : 0);
                PlayerPrefs.Save();
                DrawCoinFlipRow();
            });
            Theme.Label(_coinFlipRow, _coinFlip ? "Coin flip: a random player takes the first turn." : "Coin flip off: Player 1 takes the first turn.",
                17, Theme.TextDim, 88, 0, 606, 34, TextAnchor.MiddleLeft);
        }

        void BuildBotPanel()
        {
            List<string> decks = DeckLibrary.Playable();
            RectTransform t = PanelShell("BotPanel", "VS BOT", "No one to play with? Take on the computer. It plays by the same rules and can't see your hand.", 700, out _botPanel);

            _botMyDeck = new ThemedSelector(t, "Your deck", 48, 156, 694, decks, PlayerPrefs.GetString(PrefBotMyDeck, PlayerPrefs.GetString(PrefDeck, null)));
            _botMyInfo = Theme.Label(t, "", 16, Theme.TextDim, 50, 238, 694, 24);
            _botMyDeck.Changed += _ => RefreshDeckInfo(_botMyDeck, _botMyInfo);
            DeckPickerButton(t, _botMyDeck, _botMyInfo, 612, 150, 130);

            // The bot's deck stays a surprise: it picks one you don't own yet (see Collection.PickBotDeck).
            _botMystery = UI.Stretch(UI.Rect("BotDeckMystery", t)).gameObject;
            Theme.Label(_botMystery.transform, "BOT'S DECK", 14, Theme.TextDim, 50, 276, 694, 20, display: true);
            var mystery = Theme.RoundedPanel(_botMystery.transform, new Color(1, 1, 1, 0.05f), 48, 300, 694, 70, 12, "BotDeck");
            mystery.raycastTarget = false;
            _botPool = Theme.Label(mystery.transform, "", 17, Theme.Text, 18, 0, 658, 70, TextAnchor.MiddleLeft);

            // Once every bot deck is unlocked, the player picks the bot's deck instead: random, or any deck they can play.
            _botChooser = UI.Stretch(UI.Rect("BotDeckChooser", t)).gameObject;
            _botDeck = new ThemedSelector(_botChooser.transform, "Bot's deck", 48, 276, 694, BotDeckOptions(), PlayerPrefs.GetString(PrefBotDeck, BotDeckRandom));
            Theme.Ghost(_botChooser.transform, "ALL DECKS", 612, 270, 130, 30, () =>
                DeckOverview.Open(_deckPickerLayer, playableOnly: true, _botDeck.Value, (name, kind) => _botDeck.SetOptions(BotDeckOptions(), name)), 14);

            var levels = new List<string> { "Easy", "Normal", "Hard" };
            _botLevel = new ThemedSelector(t, "Difficulty", 48, 396, 694, levels, PlayerPrefs.GetString(PrefBotLevel, "Normal"));
            _botLevel.Changed += _ => RefreshBotPool(); // each difficulty has its own decks to unlock
            Theme.Label(t, "Easy plays its best move. Normal also thinks a move ahead. Hard also plans for your reply.", 16, Theme.TextDim, 50, 478, 694, 24);

            Theme.Cta(t, "START MATCH", 48, 526, 694, 78, StartBotMatch, 30);
            Theme.Label(t, "A coin flip decides who takes the first turn.", 16, Theme.TextFaint, 50, 624, 694, 30);
            RefreshBotPool();
        }

        /// <summary>The mystery-deck line of the VS BOT panel: how many of this difficulty's decks are still to unlock.</summary>
        void RefreshBotPool()
        {
            bool choose = Collection.AllBotDecksUnlocked;
            _botMystery.SetActive(!choose);
            _botChooser.SetActive(choose);
            if (choose) _botDeck.SetOptions(BotDeckOptions(), _botDeck.Value ?? PlayerPrefs.GetString(PrefBotDeck, BotDeckRandom));

            Bot.Level level = SelectedLevel;
            var (locked, total) = Collection.Progress(level);
            _botPool.text = locked > 0
                ? $"A secret deck you don't have yet. Beat the {level} bot to unlock it.\n{locked} of {total} {level} decks left to unlock."
                : $"You unlocked all {total} {level} decks! The bot plays a random one of them.";
            _botPool.color = locked > 0 ? Theme.Text : Theme.Success;
        }

        /// <summary>The bot-deck chooser's options: a random bot deck first, then every deck the player can play.</summary>
        static List<string> BotDeckOptions()
        {
            var options = new List<string> { BotDeckRandom };
            options.AddRange(DeckLibrary.Playable());
            return options;
        }

        void StartBotMatch()
        {
            if (_busy) return;
            var me = PrepareLocalPlayer(_name.text, _botMyDeck);
            if (me == null) return;

            // The bot's deck: chosen by the player once every bot deck is unlocked, else a surprise (null).
            string botDeck = null;
            if (Collection.AllBotDecksUnlocked && _botDeck.Index > 0)
            {
                botDeck = _botDeck.Value;
                string invalid = DeckRules.ValidateForPlay(DeckLibrary.Load(botDeck));
                if (invalid != null)
                {
                    _app.Toast($"The bot can't play {botDeck}: {invalid}", 5);
                    return;
                }
            }

            SavePrefs();
            PlayerPrefs.SetString(PrefBotMyDeck, _botMyDeck.Value);
            PlayerPrefs.SetString(PrefBotLevel, _botLevel.Value);
            if (_botDeck.Value != null) PlayerPrefs.SetString(PrefBotDeck, _botDeck.Value);
            PlayerPrefs.Save();
            LaunchBotMatch(_app, me.Value.name, DeckLibrary.Load(_botMyDeck.Value), SelectedLevel, botDeck);
        }

        Bot.Level SelectedLevel => (Bot.Level)Enum.Parse(typeof(Bot.Level), _botLevel.Value ?? "Normal");

        /// <summary>
        /// Starts a match against the bot with a deck the player doesn't own yet (from the difficulty's pool). Static so
        /// PLAY AGAIN can call it after the menu is gone; every match picks again, so a win moves on to the next deck.
        /// <paramref name="botDeck"/> is the deck the player chose for the bot (all bot decks unlocked), or null to pick one.
        /// </summary>
        static void LaunchBotMatch(App app, string myName, List<int> myDeck, Bot.Level level, string botDeck = null)
        {
            var rng = new Random();
            // A picked bot deck is usually one the player doesn't own yet, so it comes straight from the premade list:
            // DeckLibrary.Load only returns premade decks the player owns. A chosen deck is one they can play, so Load works.
            string botDeckName;
            List<int> botCards;
            if (botDeck == null)
            {
                StarterDecks.Starter premade = Collection.PickBotDeck(level, rng);
                botDeckName = premade.Name;
                botCards = premade.Cards.ToList();
            }
            else
            {
                botDeckName = botDeck;
                botCards = DeckLibrary.Load(botDeck);
            }

            bool botFirst = rng.Next(2) == 0; // coin flip
            string botName = $"Bot ({level})";
            List<int> mine = DeckRules.Shuffle(myDeck, rng), theirs = DeckRules.Shuffle(botCards, rng);
            Game game = botFirst ? new Game(botName, theirs, myName, mine, rng.Next()) : new Game(myName, mine, botName, theirs, rng.Next());
            int botIndex = botFirst ? 0 : 1;

            var avatars = new int[2];
            avatars[botIndex] = BotAvatar(botCards);
            avatars[1 - botIndex] = Avatars.Saved;
            app.StartMatch(new MatchSetup
            {
                Game = game,
                Net = new BotSession(game, level, botIndex),
                LocalIndex = 1 - botIndex,
                Avatars = avatars,
                BotDeck = botDeckName,
                CoinFlip = true,
                Rematch = () => LaunchBotMatch(app, myName, myDeck, level, botDeck),
            });
        }

        /// <summary>The bot wears the portrait of the legend its deck runs most.</summary>
        static int BotAvatar(List<int> deck)
        {
            var withArt = new HashSet<int>(Avatars.Choices().Select(l => l.Id));
            var top = deck.Where(withArt.Contains).GroupBy(id => id).OrderByDescending(g => g.Count()).FirstOrDefault();
            return top?.Key ?? Avatars.None;
        }

        /// <summary>A small ALL DECKS button above a deck picker: opens the deck grid and selects the deck clicked there.</summary>
        void DeckPickerButton(Transform parent, ThemedSelector selector, Text info, float x, float y, float w)
        {
            Theme.Ghost(parent, "ALL DECKS", x, y, w, 30, () =>
                DeckOverview.Open(_deckPickerLayer, playableOnly: true, selector.Value, (name, kind) =>
                {
                    selector.SetOptions(DeckLibrary.Playable(), name);
                    RefreshDeckInfo(selector, info);
                }), 14);
        }

        void RefreshDeckInfo(ThemedSelector selector, Text info)
        {
            if (selector.Value == null)
            {
                info.text = "No decks yet. Create one in the Deck Builder.";
                info.color = Theme.Danger;
                return;
            }
            List<int> deck = DeckLibrary.Load(selector.Value);
            int essence = deck.Count(id => id == 0);
            string error = DeckRules.ValidateForPlay(deck);
            string kind = DeckLibrary.KindOf(selector.Value);
            string prefix = kind != null ? kind + "  ·  " : "";
            info.text = error == null ? $"{prefix}{deck.Count} cards  ·  {essence} Essence  ·  Ready to play" : $"{prefix}{deck.Count} cards  ·  {error}";
            info.color = error == null ? Theme.Success : Theme.Danger;
        }

        void BuildFooter()
        {
            Theme.Label(transform, $"v{Application.version} BETA  ·  UNITY EDITION", 16, Theme.TextDim, 96, 1018, 600, 22, display: true);
            Theme.Label(transform, "Fan-made project. Not affiliated with Blue Mammoth Games. Art credits in CREDITS.txt.", 14, Theme.TextFaint, 96, 1042, 900, 20);
            Theme.Ghost(transform, "DECKS FOLDER", 1660, 1016, 220, 44, OpenDecksFolder, 17);
        }

        // ───────────────────────────── Shared helpers ─────────────────────────────

        /// <summary>Esc steps back (#74): closes the avatar or deck picker, then the open panel. It never quits the game.</summary>
        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (_pickerLayer.childCount > 0) UI.Clear(_pickerLayer);
            else if (_deckPickerLayer.childCount > 0) UI.Clear(_deckPickerLayer);
            else if (_panel != Panel.None && !_busy) ShowPanel(Panel.None);
        }

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
            List<int> deck = DeckLibrary.Load(deckSelector.Value, errors);
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
            var rng = new Random();
            bool p2First = _coinFlip && rng.Next(2) == 0; // the first player in a Game takes the first turn
            var game = p2First
                ? new Game(p2.Value.name, p2.Value.deck, p1.Value.name, p1.Value.deck, rng.Next())
                : new Game(p1.Value.name, p1.Value.deck, p2.Value.name, p2.Value.deck, rng.Next());
            int[] avatars = p2First ? new[] { Avatars.None, Avatars.Saved } : new[] { Avatars.Saved, Avatars.None };
            _app.StartMatch(new MatchSetup { Game = game, Net = null, LocalIndex = 0, Avatars = avatars, CoinFlip = _coinFlip, SeatsSwapped = p2First });
        }

        // ───────────────────────────── Networking ─────────────────────────────

        /// <summary>
        /// Shared wrapper for every online flow: validates the player, shows the busy state, and on failure or
        /// cancel disposes whatever connection was pending. Flows store each new connection in <see cref="_pending"/>
        /// right away and call <see cref="CheckAlive"/> after every await.
        /// </summary>
        async void RunOnline(string busyText, string failText, Func<(string name, List<int> deck), Task> flow)
        {
            var me = PrepareLocalPlayer(_name.text, _deck);
            if (me == null) return;
            SavePrefs();
            SetBusy(true, busyText);
            try
            {
                await flow(me.Value);
            }
            catch (Exception e)
            {
                _pending?.Dispose();
                if (this != null)
                {
                    if (_cancelled || e is OperationCanceledException) SetStatus("Not connected");
                    else if (e.Message.StartsWith(Protocol.VersionMismatchPrefix, StringComparison.Ordinal))
                    {
                        // Shown as is (no "Could not join:" prefix) and as a toast, so it is hard to miss (#34).
                        Error(e.Message);
                        _app.Toast(e.Message, 8);
                    }
                    else
                    {
                        Debug.LogException(e);
                        Error($"{failText}: {e.Message}");
                    }
                }
            }
            finally
            {
                _cancelled = false;
                _pending = null;
                _listener?.Dispose();
                _listener = null;
                if (this != null)
                {
                    ShowRoomCode(null);
                    SetBusy(false);
                }
            }
        }

        void CheckAlive()
        {
            if (this == null || _cancelled) throw new OperationCanceledException();
        }

        void ShowRoomCode(string code)
        {
            _roomBox.gameObject.SetActive(code != null);
            _roomCode.text = code == null ? "" : string.Join("  ", code.ToCharArray());
        }

        void HostRoom()
        {
            RunOnline("Creating a room...", "Could not create a room", async me =>
            {
                RelaySession session = await RelaySession.CreateRoomAsync();
                _pending = session;
                CheckAlive();
                ShowRoomCode(session.RoomCode);
                SetStatus("Waiting for a friend to join...", Theme.Text);

                await session.WaitForOpponentAsync();
                CheckAlive();
                ShowRoomCode(null);
                SetStatus("Someone is joining...", Theme.Text);
                await HandshakeAsHost(session, me);
            });
        }

        void JoinRoom()
        {
            string code = _code.text.Trim();
            if (code.Length != 4 || !code.All(char.IsDigit)) { _app.Toast("Enter the 4-digit room code."); return; }
            RunOnline($"Joining room {code}...", "Could not join", async me =>
            {
                RelaySession session = await RelaySession.JoinRoomAsync(code);
                _pending = session;
                CheckAlive();
                SetStatus("Connected! Exchanging decks...", Theme.Text);
                await HandshakeAsGuest(session, me);
            });
        }

        void HostP2P()
        {
            int port = ParsePort();
            RunOnline($"Waiting for a second player on port {port}...", "Could not host", async me =>
            {
                _listener = new NetSession.Listener(port);
                NetSession session = await _listener.AcceptAsync();
                _listener = null;
                _pending = session;
                CheckAlive();
                SetStatus("Someone is joining...", Theme.Text);
                await HandshakeAsHost(session, me);
            });
        }

        void JoinP2P()
        {
            string ip = _ip.text.Trim();
            if (string.IsNullOrWhiteSpace(ip)) { _app.Toast("Enter the host's address."); return; }
            RunOnline($"Joining {ip}...", "Could not join", async me =>
            {
                NetSession session = await NetSession.ConnectAsync(ip, ParsePort());
                _pending = session;
                CheckAlive();
                SetStatus("Connected! Exchanging decks...", Theme.Text);
                await HandshakeAsGuest(session, me);
            });
        }

        /// <summary>
        /// The host is player 0 and takes the first turn, unless the host turned on the coin flip: then the coin can
        /// make the guest player 0 (see <see cref="Protocol.GuestStarts"/>).
        /// </summary>
        async Task HandshakeAsHost(NetSession session, (string name, List<int> deck) me)
        {
            int mySeed = new Random().Next();
            bool asks = _onlineCoinFlip;
            session.Send(Protocol.EncodeHello(me.name, mySeed, me.deck, Avatars.Saved, asks));
            Protocol.Hello peer = await ReadHello(session);
            CheckAlive();
            int seed = mySeed ^ peer.Seed;
            bool flip = Protocol.CoinFlipActive(asks, peer.CoinFlip != null);
            if (Protocol.GuestStarts(asks, peer.CoinFlip != null, seed))
                Launch(new Game(peer.Name, peer.Deck, me.name, me.deck, seed), session, 1, new[] { peer.Avatar, Avatars.Saved }, flip);
            else
                Launch(new Game(me.name, me.deck, peer.Name, peer.Deck, seed), session, 0, new[] { Avatars.Saved, peer.Avatar }, flip);
            if (asks && !flip) _app.Toast($"{peer.Name}'s game is too old for the coin flip, so you go first.", 5);
        }

        async Task HandshakeAsGuest(NetSession session, (string name, List<int> deck) me)
        {
            int mySeed = new Random().Next();
            session.Send(Protocol.EncodeHello(me.name, mySeed, me.deck, Avatars.Saved));
            Protocol.Hello host = await ReadHello(session);
            CheckAlive();
            int seed = host.Seed ^ mySeed;
            bool asks = host.CoinFlip == true, flip = Protocol.CoinFlipActive(asks, true);
            if (Protocol.GuestStarts(asks, true, seed))
                Launch(new Game(me.name, me.deck, host.Name, host.Deck, seed), session, 0, new[] { Avatars.Saved, host.Avatar }, flip);
            else
                Launch(new Game(host.Name, host.Deck, me.name, me.deck, seed), session, 1, new[] { host.Avatar, Avatars.Saved }, flip);
        }

        static async Task<Protocol.Hello> ReadHello(NetSession session, int timeoutMs = 20000)
        {
            string line = await session.ReadLineAsync(timeoutMs);
            if (!Protocol.TryDecodeHello(line, out Protocol.Hello hello, out string error))
                throw new Exception(error);
            return hello;
        }

        void Launch(Game game, NetSession session, int localIndex, int[] avatars, bool coinFlip)
        {
            _pending = null; // ownership moves to the match
            _app.StartMatch(new MatchSetup { Game = game, Net = session, LocalIndex = localIndex, Avatars = avatars, CoinFlip = coinFlip });
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
