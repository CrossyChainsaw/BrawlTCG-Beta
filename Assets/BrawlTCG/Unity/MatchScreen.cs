using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>The match board (WinForms FRM_Game + UIManager + DetailedCardControl). All rules live in <see cref="Game"/>.</summary>
    public sealed partial class MatchScreen : MonoBehaviour
    {
        // ───────────── Layout (1920x1080 design space) ─────────────
        // Tall enough for 44 essence crystals: rows of 15, 14 and 15, the middle one tucked into the gaps, rows 16 apart.
        static readonly Rect TopPanel = new Rect(24, 20, 400, 208);
        static readonly Rect BottomPanel = new Rect(24, 852, 400, 208);
        const int MaxGems = 15 + 14 + 15;

        /// <summary>Centre of essence crystal <paramref name="n"/> inside a player panel (y down), filling top row first.</summary>
        static Vector2 GemSlot(int n)
        {
            if (n < 15) return new Vector2(27 + n * 24, 154);
            if (n < 29) return new Vector2(39 + (n - 15) * 24, 170);
            return new Vector2(27 + (n - 29) * 24, 186);
        }
        static readonly Rect TopLane = new Rect(440, 214, 1040, 248);
        static readonly Rect BottomLane = new Rect(440, 578, 1040, 248);
        static readonly Rect Strip = new Rect(440, 474, 1040, 92);
        static readonly Rect StageSlot = new Rect(40, 330, 165, 220);
        static readonly Rect StageInfo = new Rect(218, 330, 206, 300);
        static readonly Rect BottomHand = new Rect(440, 846, 1040, 234);
        const float SlotSpacing = 58;
        const float PileScale = 0.95f;
        static readonly Vector2 TopDeck = new Vector2(1522, 224), TopDiscard = new Vector2(1700, 224);
        static readonly Vector2 BottomDeck = new Vector2(1522, 588), BottomDiscard = new Vector2(1700, 588);

        static readonly Color ReadyGreen = new Color32(110, 240, 150, 255);
        static readonly Color PlayableGlow = new Color32(255, 226, 140, 255);
        static readonly Color DropZoneGlow = new Color32(245, 192, 74, 255);

        App _app;
        Game _game;
        NetSession _net;
        int _localIndex;
        int[] _avatars;
        bool _hotSeat, _coinFlip, _seatsSwapped;

        /// <summary>"Player 1" / "Player 2" as chosen in the menu, even when a coin flip let Player 2 start.</summary>
        int SeatNumber(int index) => (_seatsSwapped ? 1 - index : index) + 1;
        /// <summary>Bot games: starts another match with the same settings (null otherwise).</summary>
        Action _rematch;
        bool VsBot => _net is BotSession;

        /// <summary>Player shown at the bottom (whose hand is visible).</summary>
        int _viewer;
        bool _handoffPending;
        bool _ended;

        RectTransform _board, _cards, _detail, _pileView, _drag, _overlay;
        readonly Dictionary<(BoardZone zone, int player), RectTransform> _zones = new Dictionary<(BoardZone, int), RectTransform>();
        readonly Dictionary<(BoardZone zone, int player), Image> _zoneOutlines = new Dictionary<(BoardZone, int), Image>();
        readonly List<CardView> _legendViews = new List<CardView>();

        // Attack targeting
        LegendCard _targetAttacker;
        int _targetAttackIndex = -1;
        bool _targetOwnSide;
        bool Targeting => _targetAttacker != null;

        Card _detailCard;

        // ───────────────────────────── Setup ─────────────────────────────

        public void Build(App app, MatchSetup setup)
        {
            _app = app;
            _game = setup.Game;
            _net = setup.Net;
            _hotSeat = setup.HotSeat;
            _coinFlip = setup.CoinFlip;
            _seatsSwapped = setup.HotSeat && setup.SeatsSwapped;
            _localIndex = setup.LocalIndex;
            _rematch = setup.Rematch;
            _botDeck = setup.BotDeck;
            _avatars = setup.Avatars ?? new[] { Avatars.None, Avatars.None };

            BuildBackdrop();
            var background = UI.FullPanel(transform, new Color(0, 0, 0, 0), "ClickCatcher");
            UI.Clicks(background.gameObject, e => OnBackgroundClicked());
            _board = UI.Stretch(UI.Rect("Board", transform));
            _cards = UI.Stretch(UI.Rect("Cards", transform));
            _fx = UI.Stretch(UI.Rect("Fx", transform));
            _top = UI.Stretch(UI.Rect("FxTop", transform));
            _shaker = gameObject.AddComponent<Shaker>();
            _detail = UI.Stretch(UI.Rect("Detail", transform));
            _pileView = UI.Stretch(UI.Rect("PileView", transform));
            _drag = UI.Stretch(UI.Rect("Drag", transform));
            _overlay = UI.Stretch(UI.Rect("Overlay", transform));
            _menu = UI.Stretch(UI.Rect("OptionsMenu", transform));

            _game.Notification += OnNotification;
            if (setup.Showcase != null)
            {
                BeginShowcase(setup.Showcase); // staged position, scripted moves
                return;
            }
            _game.StartingHandRedraw = true; // both clients of a network game do this, so they stay in step
            _game.Start();
            _net?.StartReceiving();

            _viewer = _hotSeat ? _game.ActiveIndex : _localIndex;
            _handoffPending = _hotSeat; // hot seat: the hand stays face down behind the intro until it's handed over
            Refresh();
            PlayVersusIntro(() =>
            {
                if (_hotSeat) ShowHandoff();
                else ShowStartingHand();
            });
        }

        void BuildBackdrop()
        {
            RectTransform backdrop = _app.Backdrop;
            Texture2D tex = Resources.Load<Texture2D>("BrawlTCG/UI/BattleBackdrop");
            var pic = UI.Rect("BattleBackdrop", backdrop);
            pic.anchorMin = pic.anchorMax = new Vector2(0.5f, 0.5f);
            pic.pivot = new Vector2(0.5f, 0.5f);
            var raw = pic.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = BackdropTint;
            raw.raycastTarget = false;
            if (tex != null)
            {
                var fitter = pic.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = (float)tex.width / tex.height;
            }
            var kb = pic.gameObject.AddComponent<KenBurns>();
            kb.Amplitude = 0.02f;
            kb.Speed = 0.03f;

            // The stage in play is shown over the battle backdrop (#60), under the vignette.
            _stageBackdrops = UI.Stretch(UI.Rect("StageBackdrops", backdrop));

            // Vignette: darker edges keep the eye on the lanes.
            foreach (var (min, max, horizontal, from, to) in new[]
            {
                (new Vector2(0, 0), new Vector2(0.28f, 1), true, 0.8f, 0f),
                (new Vector2(0.72f, 0), new Vector2(1, 1), true, 0f, 0.8f),
                (new Vector2(0, 0), new Vector2(1, 0.25f), false, 0.75f, 0f),
                (new Vector2(0, 0.8f), new Vector2(1, 1), false, 0f, 0.7f),
            })
            {
                var shade = UI.Rect("Shade", backdrop);
                shade.anchorMin = min;
                shade.anchorMax = max;
                shade.offsetMin = shade.offsetMax = Vector2.zero;
                var img = shade.gameObject.AddComponent<Image>();
                img.sprite = Theme.Gradient(horizontal, from, to);
                img.color = Theme.Night;
                img.raycastTarget = false;
                // Same darkness as without a stage, with a hint of the stage colour.
                Accent(img, -1, backdrop: true, mix: 0.25f);
            }
        }

        // ── Stage backdrop (#60) ──

        const float StageFade = 0.9f;
        static readonly Color BackdropTint = new Color(0.36f, 0.38f, 0.52f, 1f);
        RectTransform _stageBackdrops;
        string _stageBackdropArt;
        const string Leaving = "Leaving";

        /// <summary>
        /// Crossfades the background to the art of the stage in play, or back to the battle backdrop when there is none.
        /// Darkened like the battle backdrop so the board stays readable. Called on every board refresh; only a change of
        /// stage starts a fade.
        /// </summary>
        void UpdateStageBackdrop()
        {
            if (_stageBackdrops == null) return;
            string art = StageVisuals ? _game.ActiveStage?.Art : null;
            Texture2D tex = art != null ? Art.Get(art) : null;
            if (tex == null) art = null;
            if (art == _stageBackdropArt) return;
            _stageBackdropArt = art;

            // Everything already there fades out; the new stage fades in on top.
            foreach (Transform old in _stageBackdrops)
            {
                var oldRaw = old.GetComponent<RawImage>();
                if (oldRaw == null || old.name == Leaving) continue;
                old.name = Leaving; // stops its fade-in, if it is still fading in
                StartCoroutine(FadeBackdrop(oldRaw, 0, destroy: true));
            }
            if (tex == null) return;

            var pic = UI.Rect("Stage " + art, _stageBackdrops);
            pic.anchorMin = pic.anchorMax = new Vector2(0.5f, 0.5f);
            pic.pivot = new Vector2(0.5f, 0.5f);
            var raw = pic.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = new Color(BackdropTint.r, BackdropTint.g, BackdropTint.b, 0);
            raw.raycastTarget = false;
            var fitter = pic.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)tex.width / tex.height;
            var kb = pic.gameObject.AddComponent<KenBurns>();
            kb.Amplitude = 0.02f;
            kb.Speed = 0.03f;
            StartCoroutine(FadeBackdrop(raw, 1, destroy: false));
        }

        System.Collections.IEnumerator FadeBackdrop(RawImage raw, float to, bool destroy)
        {
            float from = raw.color.a;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / StageFade)
            {
                if (raw == null || (!destroy && raw.name == Leaving)) yield break;
                Color c = raw.color;
                c.a = Mathf.Lerp(from, to, Theme.EaseOut(t));
                raw.color = c;
                yield return null;
            }
            if (raw == null) yield break;
            if (destroy) Destroy(raw.gameObject);
            else { Color c = raw.color; c.a = to; raw.color = c; }
        }

        void OnDestroy()
        {
            if (_game != null) _game.Notification -= OnNotification;
            _net?.Dispose();
        }

        void OnNotification(string message) => _app.Toast(message);

        Player Bottom => _game.Players[_viewer];
        Player Top => _game.Players[1 - _viewer];

        bool IsLocallyControlled(int playerIndex) => _hotSeat ? playerIndex == _viewer : playerIndex == _localIndex;

        /// <summary>Whether the person at this screen may act right now.</summary>
        bool CanAct => !_ended && !_game.IsOver && !_handoffPending && !_game.ChoosingStartingHands && IsLocallyControlled(_game.ActiveIndex);

        /// <summary>Online, during the opponent's turn: the viewer's own hand and field are darkened.</summary>
        bool WaitingForOpponent => !_ended && !_game.IsOver && !_hotSeat && !InShowcase && !_game.ChoosingStartingHands
            && !IsLocallyControlled(_game.ActiveIndex);

        // ───────────────────────────── Frame loop ─────────────────────────────

        void Update()
        {
            PumpNetwork();
            UpdateAccent();

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool typing = UI.IsTyping();

            if (InShowcase) return; // scripted: no keyboard either
            UpdateTimers();
            if (kb != null && kb.spaceKey.wasPressedThisFrame && !typing && _overlay.childCount == 0 && !PileViewOpen && !OptionsOpen && CanAct && !TurnIntroPlaying)
                Submit(GameCommand.EndTurn(_game.ActiveIndex));

            // Esc steps back: closes the options menu, cancels targeting, closes the card detail, or opens the options.
            // Right-click does the same except opening the options.
            bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
            bool right = mouse != null && mouse.rightButton.wasPressedThisFrame;
            if (esc || right)
            {
                if (OptionsOpen) { if (esc) CloseOptions(); }
                else if (PileViewOpen) CloseDiscardPile();
                else if (_burn != null) CancelBurn();
                else if (Targeting) { CancelTargeting(); Refresh(); }
                else if (_detailCard != null) CloseDetail();
                else if (esc) OpenOptions();
            }
        }

        void PumpNetwork()
        {
            if (_net == null || _ended) return;
            _net.Poll();
            while (_net.TryReceive(out string line))
            {
                if (line == Protocol.Quit)
                {
                    EndMatch("Opponent left", $"{_game.Players[1 - _localIndex].Name} left the game.");
                    return;
                }
                if (!line.StartsWith(Protocol.CommandPrefix, StringComparison.Ordinal))
                    continue; // ignore anything unknown (e.g. relay chatter)
                if (!GameCommand.TryParse(line.Substring(Protocol.CommandPrefix.Length), out GameCommand cmd) || cmd.PlayerIndex == _localIndex)
                {
                    Desync($"Invalid message from the other player: {line}");
                    return;
                }
                Snapshot before = TakeSnapshot(cmd);
                bool wasChoosing = _game.ChoosingStartingHands;
                string error = _game.TryApply(cmd);
                if (error != null)
                {
                    Desync($"The other player's move was rejected ({error}).");
                    return;
                }
                CancelTargeting();
                Refresh();
                RefreshDetail();
                if (_game.IsOver) ShowGameOver();
                Animate(before);
                if (wasChoosing && !_game.ChoosingStartingHands) BothHandsChosen(); // turn 1 begins
            }
            if (_net.IsClosed && !_net.HasPending && !_ended)
                EndMatch("Connection lost", "The other player disconnected.");
        }

        // ───────────────────────────── Commands ─────────────────────────────

        bool Submit(GameCommand cmd)
        {
            string error = _game.Validate(cmd);
            if (error != null)
            {
                _app.Toast(error);
                return false;
            }
            Snapshot before = TakeSnapshot(cmd);
            _game.TryApply(cmd);
            _net?.Send(Protocol.CommandPrefix + cmd.Serialize());

            CancelTargeting();
            CloseDetail();
            if (_hotSeat && !_game.IsOver && _game.ActiveIndex != _viewer)
            {
                RememberHand(_viewer); // what the player who just ended had in hand: turned over, not re-dealt, next time
                _viewer = _game.ActiveIndex;
                ShowHandoff();
            }
            Refresh();
            if (_game.IsOver) ShowGameOver();
            Animate(before);
            return true;
        }

        /// <summary>
        /// The turn just started: ending it is blocked for <see cref="EndTurnLock"/> seconds so a turn cannot be handed
        /// back by accident (#39). The banner itself plays on; END TURN works again before it is gone.
        /// </summary>
        bool TurnIntroPlaying => _turnBanner != null && Time.unscaledTime - _turnIntroAt < EndTurnLock;
        const float EndTurnLock = 0.75f;
        float _turnIntroAt;

        void EndTurnClicked()
        {
            if (!CanAct || TurnIntroPlaying) return;
            Submit(GameCommand.EndTurn(_game.ActiveIndex));
        }

        // ───────────────────────────── Board ─────────────────────────────

        void Refresh()
        {
            UI.Clear(_board);
            UI.Clear(_cards);
            _zones.Clear();
            _zoneOutlines.Clear();
            _legendViews.Clear();
            _layout.Clear();
            _views.Clear();

            _accentBoard.Clear();
            UpdateAccentTarget();
            UpdateStageBackdrop();
            DrawPlayerPanel(Top, TopPanel, false);
            DrawPlayerPanel(Bottom, BottomPanel, true);
            DrawLane(Top, TopLane, false);
            DrawLane(Bottom, BottomLane, true);
            DrawStrip();
            DrawStage();
            DrawPiles(Top, TopDeck, TopDiscard, false);
            DrawPiles(Bottom, BottomDeck, BottomDiscard, true);
            DrawTopHand();
            DrawBottomHand();
            DrawCorner();
            if (PileViewOpen) ShowDiscardPile(_pileViewPlayer); // keep an open discard pile up to date
        }

        // ── Player panels ──

        void DrawPlayerPanel(Player p, Rect r, bool bottom)
        {
            bool active = p == _game.ActivePlayer && !_game.IsOver;
            if (active)
            {
                var glow = UI.Panel(_board, new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.3f), r.x - 50, r.y - 50, r.width + 100, r.height + 100, "ActiveGlow");
                glow.sprite = Theme.Soft;
                glow.raycastTarget = false;
            }
            var panel = Theme.GlassPanel(_board, r.x, r.y, r.width, r.height, 20, bottom ? "MyPanel" : "EnemyPanel");
            panel.raycastTarget = false;
            if (panel.transform.Find("Outline") is RectTransform panelLine) Accent(panelLine.GetComponent<Image>(), 0.4f);
            Transform t = panel.transform;
            if (active) Quiet(Theme.RoundedPanel(t, Theme.Gold, 0, 0, r.width, r.height, 20, "ActiveLine", outline: true));

            // Avatar
            var avatar = Quiet(Theme.RoundedPanel(t, active ? Theme.Gold : new Color32(70, 80, 120, 255), 16, 16, 62, 62, 31, "Avatar"));
            Avatars.Paint(avatar, 62, _avatars[p.Index], p.Name, 30, active ? Theme.Ink : Theme.Text);

            var name = Theme.Label(t, p.Name, 26, Theme.Text, 92, 12, 220, 32, TextAnchor.MiddleLeft, display: true);
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 14;
            name.resizeTextMaxSize = 26;
            string role = _hotSeat || InShowcase ? $"PLAYER {SeatNumber(p.Index)}" : IsLocallyControlled(p.Index) ? "YOU" : "OPPONENT";
            Theme.Label(t, active ? role + "  ·  ACTIVE" : role, 13, active ? Theme.Gold : Theme.TextDim, 92, 44, 220, 18, display: true);

            // Counters (top-right)
            Theme.Label(t, $"DECK {p.Deck.Count}\nHAND {p.Hand.Count}\nDISCARD {p.DiscardPile.Count}", 13, Theme.TextDim, r.width - 110, 12, 96, 60, TextAnchor.UpperRight, display: true);

            // Health
            var heart = UI.Panel(t, new Color32(230, 54, 74, 255), 92, 72, 26, 24, "Heart");
            heart.sprite = Theme.Heart;
            heart.raycastTarget = false;
            Color hpColor = p.Health <= 10 ? new Color32(255, 140, 140, 255) : Theme.Text;
            Theme.Label(t, p.Health.ToString(), 30, hpColor, 124, 62, 70, 40, TextAnchor.MiddleLeft, display: true);
            Theme.Label(t, $"/ {p.MaxHealth} HP", 16, Theme.TextDim, 180, 72, 120, 24, TextAnchor.MiddleLeft, display: true);
            float ratio = p.MaxHealth > 0 ? Mathf.Clamp01((float)p.Health / p.MaxHealth) : 0;
            Quiet(Theme.RoundedPanel(t, new Color(1, 1, 1, 0.1f), 92, 104, r.width - 108, 10, 5, "HpTrack"));
            if (ratio > 0)
            {
                var fill = Quiet(Theme.RoundedPanel(t, new Color32(222, 48, 70, 255), 92, 104, Mathf.Max(10, (r.width - 108) * ratio), 10, 5, "HpFill"));
                fill.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var gloss = UI.Panel(fill.transform, Color.white, 0, 0, r.width, 5, "Gloss");
                gloss.color = new Color(1, 1, 1, 0.25f);
                gloss.raycastTarget = false;
            }

            // Essence crystals
            int total = p.EssenceField.Count;
            int available = p.Essence;
            // Past 44, each extra Essence makes a crystal gold (two in one slot), from the first slot on. Spending uses up
            // the gold first (gold turns back to purple), then the purple crystals go dim.
            int shown = Mathf.Min(Mathf.Max(total, available), MaxGems);
            int goldLit = Mathf.Max(0, available - MaxGems), lit = Mathf.Min(available, MaxGems);
            Theme.Label(t, "ESSENCE", 13, Theme.TextDim, 16, 124, 90, 18, display: true);
            for (int n = 0; n < shown; n++)
            {
                bool filled = n < lit, gold = n < goldLit;
                Color color = gold ? Theme.Gold : filled ? (Color)new Color32(176, 120, 255, 255) : new Color(1, 1, 1, 0.14f);
                Vector2 c = GemSlot(n);
                var gem = UI.Panel(t, color, c.x - 7, c.y - 6, 15, 15, "Gem");
                gem.sprite = Theme.Rounded;
                gem.type = Image.Type.Sliced;
                gem.pixelsPerUnitMultiplier = 8;
                gem.raycastTarget = false;
                gem.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                gem.rectTransform.anchoredPosition = new Vector2(c.x, -c.y);
                gem.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                if (filled)
                {
                    Color glowColor = gold ? new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.5f) : new Color(0.7f, 0.5f, 1f, 0.5f);
                    var glow = UI.Panel(t, glowColor, c.x - 15, c.y - 14, 30, 30, "GemGlow");
                    glow.sprite = Theme.Soft;
                    glow.raycastTarget = false;
                    glow.transform.SetSiblingIndex(gem.transform.GetSiblingIndex());
                }
            }
            string essenceText = $"{available} / {total}";
            if (active && p.PlayedEssenceCardThisTurn) essenceText += "  ·  played";
            Theme.Label(t, essenceText, 16, Theme.Text, 110, 122, r.width - 126, 22, TextAnchor.MiddleRight, display: true);

            // The bottom panel doubles as the drop zone for Essence cards.
            _zones[(BoardZone.EssenceField, p.Index)] = panel.rectTransform;
            _zoneOutlines[(BoardZone.EssenceField, p.Index)] = DropOutline(panel.transform, r.width, r.height, 20);
        }

        // ── Lanes (fields) ──

        static float SlotX(Rect lane, int slot)
        {
            float total = CardView.W * Game.MaxLegendsOnField + SlotSpacing * (Game.MaxLegendsOnField - 1);
            return lane.x + (lane.width - total) / 2 + slot * (CardView.W + SlotSpacing);
        }

        void DrawLane(Player p, Rect lane, bool bottom)
        {
            var bg = Theme.RoundedPanel(_board, new Color(0.03f, 0.04f, 0.12f, 0.55f), lane.x, lane.y, lane.width, lane.height, 22, bottom ? "MyLane" : "EnemyLane");
            bg.raycastTarget = false;
            Accent(Quiet(Theme.RoundedPanel(bg.transform, new Color(1, 1, 1, 0.08f), 0, 0, lane.width, lane.height, 22, "Line", outline: true)), 0.45f);
            Accent(Quiet(Theme.RoundedPanel(bg.transform, new Color(1, 1, 1, 0), 0, 0, lane.width, lane.height, 22, "Tint")), 0.06f);
            string label = bottom && !InShowcase && (_hotSeat || IsLocallyControlled(p.Index)) ? "YOUR FIELD" : $"{p.Name.ToUpperInvariant()}'S FIELD";
            Theme.Label(bg.transform, $"{label}  ·  {p.PlayingField.Count}/{Game.MaxLegendsOnField}", 14, Theme.TextFaint, 18, 8, 400, 20, display: true);
            _zones[(BoardZone.PlayingField, p.Index)] = bg.rectTransform;
            _zoneOutlines[(BoardZone.PlayingField, p.Index)] = DropOutline(bg.transform, lane.width, lane.height, 22);

            float y = lane.y + 34;
            for (int slot = 0; slot < Game.MaxLegendsOnField; slot++)
            {
                float x = SlotX(lane, slot);
                if (slot >= p.PlayingField.Count)
                {
                    var empty = Quiet(Theme.RoundedPanel(_board, new Color(1, 1, 1, 0.07f), x, y, CardView.W, CardView.H, 12, "EmptySlot", outline: true));
                    Quiet(Theme.RoundedPanel(_board, new Color(1, 1, 1, 0.025f), x, y, CardView.W, CardView.H, 12, "EmptySlotFill"));
                    continue;
                }

                LegendCard legend = p.PlayingField[slot];
                // Stacked weapon / battle cards peek out above the legend, first card closest to it.
                int shown = Mathf.Min(legend.StackedCards.Count, 4);
                // Darkened: used up this turn, or (your side) waiting for the opponent's turn to end.
                bool idle = bottom && WaitingForOpponent;
                bool spent = idle || legend.AttackedThisTurn && p == _game.ActivePlayer && !legend.IsStunned;
                for (int k = shown - 1; k >= 0; k--)
                {
                    CardView stacked = AddCard(legend.StackedCards[k], true, x, y - 14 * (k + 1), p, BoardZone.Stacked, k);
                    if (spent) stacked.SetDimmed(true); // darkened together with their legend
                }

                CardView view = AddCard(legend, true, x, y, p, BoardZone.PlayingField, slot, stunned: legend.IsStunned);
                _legendViews.Add(view);

                if (idle) view.SetDimmed(true);
                if (legend.IsStunned) { /* ribbon is painted on the card */ }
                else if (legend.AttackedThisTurn && p == _game.ActivePlayer)
                {
                    // Used up for this turn: darkened so it reads at a glance (no label needed).
                    view.SetDimmed(true);
                }
                else if (!legend.CanAttack) view.AddNoStrikes(); // Silent Galaxy: red "not allowed" sword badge

                if (Targeting && IsValidAttackTarget(p, legend)) view.SetHighlight(true);
                else if (legend == _targetAttacker) view.SetGlow(Color.white, 0.6f);
                else if (!Targeting && CanAct && p == _game.ActivePlayer && CanUseAnyAttack(legend))
                {
                    view.SetGlow(ReadyGreen, 0.8f);
                    view.gameObject.AddComponent<GlowPulse>();
                }
            }
        }

        bool CanUseAnyAttack(LegendCard legend) => legend.Attacks.Any(a => _game.CanUseAttack(legend, a) == null);

        // ── Center strip: turn state, prompts, end turn ──

        void DrawStrip()
        {
            var strip = Theme.RoundedPanel(_board, new Color(0.03f, 0.04f, 0.12f, 0.78f), Strip.x, Strip.y, Strip.width, Strip.height, 20, "Strip");
            strip.raycastTarget = false;
            Accent(Quiet(Theme.RoundedPanel(strip.transform, new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.25f), 0, 0, Strip.width, Strip.height, 20, "Line", outline: true)), 0.5f);
            Transform t = strip.transform;

            if (Targeting)
            {
                Attack attack = _targetAttacker.Attacks[_targetAttackIndex];
                string side = _targetOwnSide ? "one of your legends" : "an enemy legend";
                Theme.Label(t, attack.Name.ToUpperInvariant(), 28, Theme.Gold, 28, 10, 600, 40, TextAnchor.MiddleLeft, display: true);
                Theme.Label(t, $"Click {side} to target it.  Right-click or Esc to cancel.", 17, Theme.Text, 28, 48, 700, 30, TextAnchor.MiddleLeft);
                Theme.Ghost(t, "CANCEL", Strip.width - 200, 20, 176, 52, () => { CancelTargeting(); Refresh(); }, 20);
                return;
            }

            string title;
            Color titleColor;
            _turnTimerLabel = null;
            _turnTimerBar = null;
            if (_game.ChoosingStartingHands && !_game.IsOver)
            {
                bool chosen = _game.HasChosenStartingHand(_viewer);
                Theme.Label(t, "STARTING HANDS", 34, Theme.Gold, 28, 8, 640, 46, TextAnchor.MiddleLeft, display: true);
                string waitingFor = _game.Players[1 - _viewer].Name;
                Theme.Label(t, chosen ? $"Waiting for {waitingFor} to choose their starting hand" : "Choose which cards to keep",
                    16, Theme.TextDim, 30, 54, 760, 26, TextAnchor.MiddleLeft);
                var wait = Theme.Ghost(t, "WAITING", Strip.width - 236, 16, 212, 60, null, 22);
                wait.interactable = false;
                return;
            }
            if (_game.IsOver)
            {
                title = "MATCH OVER";
                titleColor = Theme.Gold;
            }
            else if (CanAct && !InShowcase)
            {
                title = "YOUR TURN";
                titleColor = Theme.Gold;
            }
            else if (_handoffPending)
            {
                title = "PASS THE DEVICE";
                titleColor = Theme.TextDim;
            }
            else
            {
                title = $"{_game.ActivePlayer.Name.ToUpperInvariant()}'S TURN";
                titleColor = Theme.Text;
            }
            Theme.Label(t, title, 34, titleColor, 28, 8, 620, 46, TextAnchor.MiddleLeft, display: true);
            string sub = $"Turn {_game.TurnNumber}";
            if (InShowcase) { }
            else if (CanAct) sub += "  ·  Drag cards onto the board, click a legend to attack, Space ends your turn";
            else if (!_game.IsOver && !_hotSeat) sub += "  ·  Waiting for your opponent";
            var subLabel = Theme.Label(t, sub, 16, Theme.TextDim, 30, 54, 630, 26, TextAnchor.MiddleLeft);
            subLabel.resizeTextForBestFit = true;
            subLabel.resizeTextMinSize = 12;
            subLabel.resizeTextMaxSize = 16;
            subLabel.verticalOverflow = VerticalWrapMode.Truncate;
            DrawTurnTimer(t);

            if (CanAct)
                Theme.Cta(t, "END TURN", Strip.width - 236, 16, 212, 60, EndTurnClicked, 28);
            else
            {
                var waiting = Theme.Ghost(t, _game.IsOver ? "GAME OVER" : "WAITING", Strip.width - 236, 16, 212, 60, null, 22);
                waiting.interactable = false;
            }
        }

        // ── Stage ──

        void DrawStage()
        {
            var title = Accent(Theme.Label(_board, "STAGE", 16, Theme.Gold, StageSlot.x, StageSlot.y - 30, 200, 22, display: true), 1f);
            title.raycastTarget = false;
            var slot = Quiet(Theme.RoundedPanel(_board, new Color(1, 1, 1, 0.05f), StageSlot.x, StageSlot.y, StageSlot.width, StageSlot.height, 14, "StageSlot"));
            Accent(Quiet(Theme.RoundedPanel(slot.transform, new Color(1, 1, 1, 0.12f), 0, 0, StageSlot.width, StageSlot.height, 14, "Line", outline: true)), 0.6f);
            _zones[(BoardZone.Stage, -1)] = slot.rectTransform;
            _zoneOutlines[(BoardZone.Stage, -1)] = DropOutline(slot.transform, StageSlot.width, StageSlot.height, 14);

            StageCard stage = _game.ActiveStage;
            if (stage == null)
            {
                Theme.Label(slot.transform, "No stage\nin play", 18, Theme.TextFaint, 0, 0, StageSlot.width, StageSlot.height, TextAnchor.MiddleCenter, display: true);
                Theme.Label(_board, "Drag a Stage card here to change the battlefield for both players.", 15, Theme.TextFaint,
                    StageInfo.x, StageInfo.y + 6, StageInfo.width, 120);
                return;
            }

            AddCard(stage, true, StageSlot.x, StageSlot.y, _game.ActiveStageOwner, BoardZone.Stage, 0, scale: StageSlot.width / CardView.W);
            var name = Theme.Label(_board, stage.Name.ToUpperInvariant(), 22, Theme.Text, StageInfo.x, StageInfo.y + 2, StageInfo.width, 30, display: true);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 12;
            name.resizeTextMaxSize = 22;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            Theme.Label(_board, $"Played by {_game.ActiveStageOwner?.Name}", 14, Theme.TextDim, StageInfo.x, StageInfo.y + 34, StageInfo.width, 20);
            string effects = CleanEffects(stage.EffectDescription);
            if (string.IsNullOrWhiteSpace(effects)) effects = "No special effect.";
            if (!_game.DrawCardStartTurn) effects += "\n\nPlayers don't draw at the start of their turn.";
            TriggerIcons.PaintAbilities(_board, effects, StageInfo.x, StageInfo.y + 62, StageInfo.width, StageInfo.height - 62, 15, 10, Theme.Text);
        }

        static string CleanEffects(string description)
        {
            return string.Join("\n", (description ?? "").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.EndsWith("N/A")));
        }

        // ── Deck & discard piles ──

        void DrawPiles(Player p, Vector2 deckPos, Vector2 discardPos, bool bottom)
        {
            float w = CardView.W * PileScale, h = CardView.H * PileScale;
            PileLabel($"DECK  {p.Deck.Count}", deckPos.x, deckPos.y + h + 6, w);
            PileLabel($"DISCARD  {p.DiscardPile.Count}", discardPos.x, discardPos.y + h + 6, w);

            if (p.Deck.Count > 0)
            {
                // A couple of offset backs suggest the thickness of the deck.
                int layers = Mathf.Min(3, (p.Deck.Count + 9) / 10);
                for (int i = layers - 1; i >= 1; i--)
                    AddCard(p.Deck[0], false, deckPos.x + i * 3, deckPos.y - i * 3, p, BoardZone.Deck, -1, scale: PileScale);
                AddCard(p.Deck[p.Deck.Count - 1], false, deckPos.x, deckPos.y, p, BoardZone.Deck, p.Deck.Count - 1, scale: PileScale);
            }
            else
            {
                EmptyPile(deckPos, w, h, "EMPTY");
            }

            if (p.DiscardPile.Count > 0)
            {
                int top = p.DiscardPile.Count - 1;
                AddCard(p.DiscardPile[top], true, discardPos.x, discardPos.y, p, BoardZone.DiscardPile, top, scale: PileScale);
            }
            else
            {
                EmptyPile(discardPos, w, h, "");
            }

            // Your discard pile is a drop zone: discard a hand card by paying its Essence cost.
            if (bottom)
            {
                var zone = Quiet(UI.Panel(_board, new Color(0, 0, 0, 0), discardPos.x, discardPos.y, w, h, "DiscardZone"));
                _zones[(BoardZone.DiscardPile, p.Index)] = zone.rectTransform;
                _zoneOutlines[(BoardZone.DiscardPile, p.Index)] = DropOutline(zone.transform, w, h, 12);
            }
        }

        void PileLabel(string text, float x, float y, float w)
        {
            Theme.Label(_board, text, 15, Theme.TextDim, x, y, w, 22, TextAnchor.MiddleCenter, display: true);
        }

        void EmptyPile(Vector2 pos, float w, float h, string text)
        {
            var slot = Quiet(Theme.RoundedPanel(_board, new Color(1, 1, 1, 0.07f), pos.x, pos.y, w, h, 12, "EmptyPile", outline: true));
            if (text.Length > 0)
                Theme.Label(slot.transform, text, 16, Theme.TextFaint, 0, 0, w, h, TextAnchor.MiddleCenter, display: true);
        }

        // ── Hands ──

        void DrawTopHand()
        {
            Player p = Top;
            int n = p.Hand.Count;
            HandBadge(n, 84);
            if (n == 0) return;
            const float scale = 0.7f;
            float w = CardView.W * scale;
            float maxWidth = 900;
            float spacing = Mathf.Min(14, n > 1 ? (maxWidth - w * n) / (n - 1) : 0);
            float total = w * n + spacing * (n - 1);
            float start = 960 - total / 2;
            for (int i = 0; i < n; i++)
                AddCard(p.Hand[i], false, start + i * (w + spacing), -64, p, BoardZone.Hand, i, scale: scale);
        }

        void DrawBottomHand()
        {
            Player p = Bottom;
            int n = p.Hand.Count;
            HandBadge(n, BottomHand.y - 12);
            if (n == 0)
            {
                Theme.Label(_board, "No cards in hand", 18, Theme.TextFaint, BottomHand.x, BottomHand.y + 90, BottomHand.width, 30, TextAnchor.MiddleCenter, display: true);
                return;
            }
            float spacing = 14;
            if (CardView.W * n + spacing * (n - 1) > BottomHand.width)
                spacing = (BottomHand.width - CardView.W * n) / (n - 1);
            float total = CardView.W * n + spacing * (n - 1);
            float start = BottomHand.x + (BottomHand.width - total) / 2;
            bool showPlayable = CanAct && p == _game.ActivePlayer;
            for (int i = 0; i < n; i++)
            {
                // While the device is being passed, the hand stays face down.
                CardView v = AddCard(p.Hand[i], !_handoffPending, start + i * (CardView.W + spacing), BottomHand.y + 28, p, BoardZone.Hand, i);
                v.CanDrag = view => CanAct && view.PlayerIndex == _game.ActiveIndex;
                v.Dropped = HandleDrop;
                v.DragStarted = OnDragStarted;
                v.DragEnded = OnDragEnded;
                v.gameObject.AddComponent<HandHover>();
                if (WaitingForOpponent)
                {
                    // Not your turn: the whole hand is darkened.
                    v.SetDimmed(true);
                }
                else if (showPlayable && IsPlayable(p, i))
                {
                    v.SetGlow(PlayableGlow, 0.5f);
                    v.gameObject.AddComponent<GlowPulse>();
                }
                else if (p == _game.ActivePlayer && p.PlayedEssenceCardThisTurn && p.Hand[i] is EssenceCard)
                {
                    // Only one Essence per turn: the rest are darkened until next turn.
                    v.SetDimmed(true);
                }
                else if (showPlayable && p.Hand[i].Cost > p.Essence)
                {
                    // Can't afford it yet this turn.
                    v.SetDimmed(true);
                }
                else if (showPlayable)
                {
                    // Affordable, but it can't be played right now: a legend while your field is full, a weapon no
                    // legend of yours can carry, a battle card with no legend on the side it targets (yours for
                    // heals/boosts, the enemy's for damage/stuns). The board is redrawn after every move, so it
                    // lights up again as soon as it becomes playable.
                    v.SetDimmed(true);
                }
            }
        }

        /// <summary>"7 CARDS" pill centred over a hand.</summary>
        void HandBadge(int count, float y)
        {
            const float w = 132, h = 30;
            bool full = count >= 15; // a crowded hand turns the counter red
            Color accent = full ? new Color32(255, 96, 96, 255) : Theme.Gold;
            Color fill = full ? new Color(0.32f, 0.04f, 0.07f, 0.95f) : new Color(0.03f, 0.04f, 0.12f, 0.92f);
            var pill = Quiet(Theme.RoundedPanel(_board, fill, 960 - w / 2, y, w, h, 15, "HandCount"));
            Quiet(Theme.RoundedPanel(pill.transform, new Color(accent.r, accent.g, accent.b, full ? 0.9f : 0.55f), 0, 0, w, h, 15, "Line", outline: true));
            var label = Theme.Label(pill.transform, count == 1 ? "1 CARD" : $"{count} CARDS", 16, full ? new Color32(255, 200, 200, 255) : Theme.Gold, 0, 0, w, h, TextAnchor.MiddleCenter, display: true);
            label.raycastTarget = false;
        }

        /// <summary>Whether the hand card has at least one legal way to be played right now.</summary>
        bool IsPlayable(Player p, int hand)
        {
            Card card = p.Hand[hand];
            int pi = p.Index;
            switch (card.Type)
            {
                case CardType.Essence: return _game.Validate(GameCommand.PlayEssence(pi, hand)) == null;
                case CardType.Stage: return _game.Validate(GameCommand.PlayStage(pi, hand)) == null;
                case CardType.Legend: return _game.Validate(GameCommand.PlayLegend(pi, hand)) == null;
                case CardType.Weapon:
                    for (int l = 0; l < p.PlayingField.Count; l++)
                        if (_game.Validate(GameCommand.PlayWeapon(pi, hand, l)) == null) return true;
                    return false;
                default:
                    // A discard pick (Afterlife) targets a discard-pile index, the rest a legend on the field.
                    int last = card is BattleCard b && b.PicksFromDiscard ? p.DiscardPile.Count : Game.MaxLegendsOnField;
                    for (int t = -1; t < last; t++)
                        if (_game.Validate(GameCommand.PlayBattle(pi, hand, t)) == null) return true;
                    return false;
            }
        }

        // ── Bottom-right corner: help + leave ──

        void DrawCorner()
        {
            GearButton(_board, 1840, 20, 56);
            var help = Theme.GlassPanel(_board, 1508, 864, 388, 196, 20, "Help");
            help.raycastTarget = false;
            Theme.Label(help.transform, "HOW TO PLAY", 15, Theme.Gold, 20, 14, 340, 20, display: true);
            Theme.Label(help.transform,
                "Drag cards from your hand onto the board.\nDrop one on your discard pile to discard it (costs its Essence).\nClick any card to inspect it.\nClick your legend, pick an attack, then a target.\nSpace or END TURN passes the turn.\nEsc opens the options.",
                15, Theme.TextDim, 20, 40, 356, 148);
        }

        // ───────────────────────────── Cards ─────────────────────────────

        CardView AddCard(Card card, bool faceUp, float x, float y, Player owner, BoardZone zone, int index, bool stunned = false, float scale = 1f)
        {
            CardView v = CardView.Create(_cards, card, faceUp, x, y, scale, stunned);
            v.PlayerIndex = owner?.Index ?? -1;
            v.Zone = zone;
            v.Index = index;
            v.Clicked = OnCardClicked;
            v.DragLayer = _drag;
            Register(v, card, x, y, scale, zone, index, owner);
            return v;
        }

        static Image Quiet(Image img)
        {
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A gold outline shown on a zone while a card that can be dropped there is being dragged.</summary>
        static Image DropOutline(Transform parent, float w, float h, float radius)
        {
            var line = Theme.RoundedPanel(parent, DropZoneGlow, -3, -3, w + 6, h + 6, radius + 3, "DropOutline", outline: true);
            line.raycastTarget = false;
            line.gameObject.SetActive(false);
            return line;
        }

        // ───────────────────────────── Clicking ─────────────────────────────

        void OnBackgroundClicked()
        {
            if (Targeting)
            {
                CancelTargeting();
                Refresh();
            }
            CloseDetail();
        }

        void OnCardClicked(CardView view)
        {
            if (Targeting)
            {
                if (view.Zone == BoardZone.PlayingField && view.Card is LegendCard target && IsValidAttackTarget(_game.Players[view.PlayerIndex], target))
                {
                    SubmitAttack(_targetAttacker, _targetAttackIndex, view.Index);
                }
                return;
            }
            if (!view.FaceUp) return;
            if (view.Zone == BoardZone.DiscardPile) { ShowDiscardPile(view.PlayerIndex); return; }
            OpenDetail(view.Card);
        }

        // ── Discard pile viewer ──

        int _pileViewPlayer = -1;
        bool PileViewOpen => _pileViewPlayer >= 0;

        /// <summary>The Afterlife being played: the discard pile is open to pick a legend for it (null otherwise).</summary>
        Card _pilePick;
        const string PickPrompt = "Pick a legend from your discard pile.";

        /// <summary>Every card in a player's discard pile, newest first. Hover for the full card, click to open its details.</summary>
        void ShowDiscardPile(int playerIndex)
        {
            Player p = _game.Players[playerIndex];
            UI.Clear(_pileView);
            // A pick only stays open while it can still be made (it's still your turn and the card is still in hand).
            if (_pilePick != null && (!CanAct || _pilePick.Owner != p || !p.Hand.Contains(_pilePick) || !p.DiscardPile.Any(c => c is LegendCard)))
                _pilePick = null;
            bool picking = _pilePick != null;
            if (p.DiscardPile.Count == 0) { _pileViewPlayer = -1; return; }
            _pileViewPlayer = playerIndex;

            var dim = UI.FullPanel(_pileView, new Color(Theme.Night.r, Theme.Night.g, Theme.Night.b, 0.72f), "Dim");
            UI.Clicks(dim.gameObject, _ => CloseDiscardPile());

            const float W = 1060, H = 780, X = 120, Y = (1080 - H) / 2, Scale = 0.8f, Gap = 18;
            var box = Theme.GlassPanel(dim.rectTransform, X, Y, W, H, 26, "DiscardPile");
            box.color = new Color32(11, 14, 34, 255);
            UI.Clicks(box.gameObject, _ => { });
            Transform t = box.transform;
            string owner = _hotSeat || InShowcase || !IsLocallyControlled(p.Index) ? p.Name.ToUpperInvariant() + "'S" : "YOUR";
            if (picking)
            {
                Theme.Label(t, $"{_pilePick.Name.ToUpperInvariant()}  ·  PICK A LEGEND", 36, Theme.Gold, 36, 24, W - 140, 50, display: true);
                Theme.Label(t, "CLICK A LEGEND TO RETURN IT TO YOUR HAND  ·  CLICK OUTSIDE OR ESC TO CANCEL", 15, Theme.TextDim, 38, 76, W - 80, 22, display: true);
            }
            else
            {
                Theme.Label(t, $"{owner} DISCARD PILE", 36, Theme.Text, 36, 24, W - 140, 50, display: true);
                Theme.Label(t, $"{p.DiscardPile.Count} CARD{(p.DiscardPile.Count == 1 ? "" : "S")}  ·  NEWEST FIRST  ·  HOVER TO ENLARGE, CLICK FOR DETAILS",
                    15, Theme.TextDim, 38, 76, W - 80, 22, display: true);
            }
            var close = Theme.Ghost(t, "×", W - 70, 26, 44, 44, CloseDiscardPile, 30);
            close.GetComponentInChildren<Text>().font = Theme.Body; // the display font has no multiplication sign

            var area = UI.Place(UI.Rect("Grid", t), 20, 112, W - 40, H - 132);
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 60;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = UI.Stretch(UI.Rect("Viewport", area));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var content = UI.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CardView.W * Scale, CardView.H * Scale);
            grid.spacing = new Vector2(Gap, Gap);
            grid.padding = new RectOffset(16, 16, 8, 16);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            // The full-size card appears right of the panel.
            var previewAt = new Vector2(X + W + 24, (1080 - CardView.H * 1.9f) / 2);
            for (int i = p.DiscardPile.Count - 1; i >= 0; i--)
            {
                Card card = p.DiscardPile[i];
                var cell = UI.Rect(card.Name, content);
                CardView thumb = CardPeek.Create(cell, card, 0, 0, Scale, _drag, previewAt);
                if (!picking)
                    UI.Clicks(thumb.gameObject, _ => { CloseDiscardPile(); OpenDetail(card); });
                else if (card is LegendCard)
                {
                    int index = i;
                    thumb.SetGlow(Theme.Gold, 0.8f);
                    UI.Clicks(thumb.gameObject, _ => PickFromDiscard(p, index));
                }
                else
                    thumb.SetDimmed(true, 0.8f); // only legends can be picked
            }
        }

        /// <summary>Plays the Afterlife on the legend at discard-pile <paramref name="index"/>.</summary>
        void PickFromDiscard(Player p, int index)
        {
            Card card = _pilePick;
            CloseDiscardPile();
            int hand = card != null ? p.Hand.IndexOf(card) : -1;
            if (hand >= 0) Submit(GameCommand.PlayBattle(p.Index, hand, index));
        }

        void CloseDiscardPile()
        {
            _pilePick = null;
            _pileViewPlayer = -1;
            UI.Clear(_pileView); // the thumbnails remove their own full-size previews
        }

        bool IsValidAttackTarget(Player side, LegendCard legend)
        {
            if (!Targeting) return false;
            Player wanted = _targetOwnSide ? _targetAttacker.Owner : _game.GetOpponent(_targetAttacker.Owner);
            return side == wanted && (_targetOwnSide || _game.WantedAllows(legend)); // Wanted legends must be attacked first
        }

        void CancelTargeting()
        {
            _targetAttacker = null;
            _targetAttackIndex = -1;
            StopTargetLine();
        }

        // ───────────────────────────── Drag & drop (WinForms TryToSnapCard) ─────────────────────────────

        void OnDragStarted(CardView view)
        {
            CloseDetail();
            Card card = view.Card;
            int me = view.PlayerIndex, enemy = 1 - me;
            void Show(BoardZone zone, int player)
            {
                if (_zoneOutlines.TryGetValue((zone, player), out Image line)) line.gameObject.SetActive(true);
            }
            switch (card.Type)
            {
                case CardType.Essence: Show(BoardZone.EssenceField, me); break;
                case CardType.Stage: Show(BoardZone.Stage, -1); break;
                case CardType.Legend: Show(BoardZone.PlayingField, me); break;
                case CardType.Weapon:
                    foreach (CardView v in _legendViews)
                        if (v.PlayerIndex == me && ((LegendCard)v.Card).HasWeapon(((WeaponCard)card).Weapon)) v.SetGlow(DropZoneGlow);
                    break;
                case CardType.Battle:
                    var battle = (BattleCard)card;
                    if (!battle.TargetRequired)
                    {
                        Show(BoardZone.PlayingField, me);
                        Show(BoardZone.PlayingField, enemy);
                    }
                    else
                    {
                        int side = battle.FriendlyFire ? me : enemy;
                        foreach (CardView v in _legendViews)
                            if (v.PlayerIndex == side) v.SetGlow(DropZoneGlow);
                    }
                    break;
            }
            // Your discard pile takes any card you can pay for.
            if (_game.Players[me].Essence >= card.Cost) Show(BoardZone.DiscardPile, me);
        }

        void OnDragEnded(CardView view)
        {
            foreach (Image line in _zoneOutlines.Values)
                if (line != null) line.gameObject.SetActive(false);
        }

        bool Over(BoardZone zone, int player, Vector2 screen)
        {
            return _zones.TryGetValue((zone, player), out RectTransform rt)
                && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null);
        }

        int LegendUnder(int player, Vector2 screen)
        {
            foreach (CardView v in _legendViews)
            {
                if (v.PlayerIndex == player && RectTransformUtility.RectangleContainsScreenPoint(v.Rect, screen, null))
                    return v.Index;
            }
            return -1;
        }

        void HandleDrop(CardView view, Vector2 screen)
        {
            bool played = TryDrop(view, screen);
            if (!played) Refresh(); // clears drag highlights and hover state
        }

        bool TryDrop(CardView view, Vector2 screen)
        {
            if (!CanAct || view.Zone != BoardZone.Hand) return false;
            int me = view.PlayerIndex;
            int enemy = 1 - me;
            Player player = _game.Players[me];
            if (view.Index < 0 || view.Index >= player.Hand.Count || player.Hand[view.Index] != view.Card) return false;
            Card card = view.Card;
            int hand = view.Index;

            // Any card can be dropped on your own discard pile: it costs the card's Essence, and no effect triggers.
            if (Over(BoardZone.DiscardPile, me, screen))
                return Submit(GameCommand.DiscardCard(me, hand));

            switch (card)
            {
                case EssenceCard _:
                    return Over(BoardZone.EssenceField, me, screen) && Submit(GameCommand.PlayEssence(me, hand));
                case StageCard _:
                    return Over(BoardZone.Stage, -1, screen) && Submit(GameCommand.PlayStage(me, hand));
                case LegendCard _:
                    return Over(BoardZone.PlayingField, me, screen) && Submit(GameCommand.PlayLegend(me, hand));
                case WeaponCard _:
                    if (!Over(BoardZone.PlayingField, me, screen)) return false;
                    int legend = LegendUnder(me, screen);
                    return legend >= 0 && Submit(GameCommand.PlayWeapon(me, hand, legend));
                case BattleCard battle when battle.PicksFromDiscard:
                {
                    // Afterlife: dropping it opens your discard pile to pick the legend; nothing is played until you do.
                    if (!Over(BoardZone.PlayingField, me, screen) && !Over(BoardZone.PlayingField, enemy, screen)) return false;
                    string why = _game.Validate(GameCommand.PlayBattle(me, hand, -1));
                    if (why != PickPrompt) { _app.Toast(why); return false; }
                    _pilePick = card;
                    ShowDiscardPile(me);
                    return false; // the card stays in the hand until a legend is picked
                }
                case BattleCard battle:
                    if (!Over(BoardZone.PlayingField, me, screen) && !Over(BoardZone.PlayingField, enemy, screen)) return false;
                    int target = -1;
                    if (battle.TargetRequired)
                    {
                        target = LegendUnder(battle.FriendlyFire ? me : enemy, screen);
                        if (target < 0)
                        {
                            if (LegendUnder(battle.FriendlyFire ? enemy : me, screen) >= 0)
                                _app.Toast(battle.FriendlyFire ? "Play this on one of your own legends." : "Play this on an enemy legend.");
                            return false;
                        }
                    }
                    return Submit(GameCommand.PlayBattle(me, hand, target));
            }
            return false;
        }

        // ───────────────────────────── Detail panel (DetailedCardControl) ─────────────────────────────

        void CloseDetail()
        {
            _detailCard = null;
            UI.Clear(_detail);
        }

        void RefreshDetail()
        {
            if (_detailCard == null) return;
            if (IsVisible(_detailCard)) OpenDetail(_detailCard);
            else CloseDetail();
        }

        bool IsVisible(Card c)
        {
            if (_game.ActiveStage == c) return true;
            foreach (Player p in _game.Players)
            {
                if (p.PlayingField.Contains(c)) return true;
                if (p.PlayingField.Any(l => l.StackedCards.Contains(c))) return true;
                if (p.EssenceField.Contains(c) || p.DiscardPile.Contains(c)) return true;
            }
            return Bottom.Hand.Contains(c);
        }

        const float DetailX = 1268, DetailY = 84;
        /// <summary>Tallest the detail panel may grow (to just above the bottom of the 1080 canvas).</summary>
        const float MaxDetailH = 1080 - DetailY - 12;

        /// <summary>Where the full-size preview of a mentioned card appears: left of the detail panel, near the thumbnail.</summary>
        static Vector2 PeekSpot(float panelY) =>
            new Vector2(DetailX - CardView.W * 1.9f - 24, Mathf.Clamp(DetailY + panelY - 150, 20, 1080 - CardView.H * 1.9f - 20));

        /// <summary>A row of hoverable thumbnails for the cards in <paramref name="ids"/>; returns the y below it.</summary>
        float PeekRow(Transform parent, int[] ids, float x, float y, float width, float scale)
        {
            float w = CardView.W * scale, h = CardView.H * scale, gap = 10;
            int perRow = Mathf.Max(1, (int)((width + gap) / (w + gap)));
            for (int i = 0; i < ids.Length; i++)
            {
                float px = x + (i % perRow) * (w + gap), py = y + (i / perRow) * (h + gap);
                CardPeek.Create(parent, ids[i], px, py, scale, _drag, PeekSpot(py));
            }
            return y + ((ids.Length + perRow - 1) / perRow) * (h + gap);
        }

        void OpenDetail(Card card)
        {
            UI.Clear(_detail);
            _detailCard = card;
            const float X = DetailX, Y = DetailY, W = 628, H = 916;

            var panel = Theme.GlassPanel(_detail, X, Y, W, H, 26, "DetailPanel");
            panel.color = new Color32(11, 14, 34, 255); // fully opaque: in linear colour space even 97% lets bright cards show through
            panel.gameObject.AddComponent<SlideIn>().Offset = new Vector2(40, 0);
            UI.Clicks(panel.gameObject, e => { }); // swallow clicks so the board behind does not react
            Transform t = panel.transform;
            var close = Theme.Ghost(t, "×", W - 62, 18, 44, 44, CloseDetail, 30);
            close.GetComponentInChildren<Text>().font = Theme.Body;

            CardView big = CardView.Create(t, card, true, 24, 24, 1.9f, card is LegendCard l0 && l0.IsStunned);
            foreach (Graphic g in big.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;

            // Info column
            const float ix = 326, iw = W - ix - 24;
            var name = Theme.Label(t, card.Name.ToUpperInvariant(), 30, Theme.Text, ix, 72, iw, 76, display: true);
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 16;
            name.resizeTextMaxSize = 30;
            string type = card is WeaponCard wc ? $"{card.Type} · {wc.Weapon}" : $"{card.Type} · {card.Element}";
            Theme.Label(t, type.ToUpperInvariant(), 15, CardArt.Accent(card.Element), ix, 150, iw, 20, display: true);
            Theme.Label(t, $"Cost {card.Cost}" + (card.Owner != null ? $"  ·  {card.Owner.Name}" : ""), 15, Theme.TextDim, ix, 172, iw, 20);

            // Made during the match (crafted, summoned, set by an effect): say by what.
            float top = 206;
            if (card.CraftedBy != null) top = TriggerIcons.PaintCraftedChip(t, card.CraftedBy, ix, 198, iw) + 8;
            if (card.RevivedBy != null) top = TriggerIcons.PaintRevivedChip(t, card.RevivedBy, ix, card.CraftedBy != null ? top : 198, iw) + 8;

            if (card is LegendCard legend)
            {
                float half = (iw - 8) / 2;
                InfoChip(t, $"ATK {legend.Power}", new Color32(226, 112, 28, 255), ix, top, half);
                InfoChip(t, $"HP {legend.CurrentHP} / {legend.BaseHealth}", new Color32(214, 40, 62, 255), ix + half + 8, top, half);
                InfoChip(t, $"{(legend.IsSingleWeapon ? "WEAPON" : "WEAPONS")}  {legend.WeaponText(Pretty)}", new Color(1, 1, 1, 0.12f), ix, top + 44, iw);

                float y = 418;
                Theme.Label(t, "ATTACKS", 16, Theme.Gold, 24, y, 300, 22, display: true);
                y += 28;
                if (legend.Attacks.Count == 0)
                {
                    Theme.Label(t, "This legend has no attacks.", 17, Theme.TextDim, 24, y, W - 48, 30);
                    y += 40;
                }
                for (int i = 0; i < legend.Attacks.Count; i++)
                {
                    AttackRow(t, legend, i, 24, y, W - 48);
                    y += 82;
                }

                string effects = TriggerIcons.SplitCardText(legend).abilities;
                if (!string.IsNullOrWhiteSpace(effects))
                {
                    // A stunned legend's abilities are turned off (Game skips its triggers): shown dimmed.
                    bool off = legend.IsStunned && legend.OnPlayingField;
                    Theme.Label(t, off ? "ABILITIES  ·  OFF WHILE STUNNED" : "ABILITIES", 16, off ? Theme.TextDim : Theme.Gold, 24, y + 4, 400, 22, display: true);
                    int[] mentioned = CardReferences.For(card);
                    float textW = W - 48;
                    if (mentioned != null)
                    {
                        // Thumbnails of the mentioned cards sit at the right of the abilities text.
                        float pw = (CardView.W * 0.3f + 10) * mentioned.Length;
                        PeekRow(t, mentioned, W - 24 - pw + 10, y + 4, pw, 0.3f);
                        textW -= pw + 8;
                    }
                    TriggerIcons.PaintAbilities(t, effects, 24, y + 30, textW, 62, 16, 11, off ? Theme.TextFaint : Theme.Text);
                    y += 96;
                }

                // EFFECTS, then EQUIPPED at the bottom. The panel grows (up to the screen edge) when effects need the room.
                bool equipped = legend.StackedCards.Count > 0;
                float below = equipped ? 136 : 20;
                y = EffectsSection(t, legend, 24, y, W - 48, MaxDetailH - below);
                float panelH = Mathf.Min(MaxDetailH, Mathf.Max(H, y + below));
                if (panelH > H)
                {
                    UI.Place(panel.rectTransform, X, Y, W, panelH);
                    if (t.Find("Outline") is RectTransform outline) UI.Place(outline, 0, 0, W, panelH);
                }

                if (equipped)
                {
                    Theme.Label(t, $"EQUIPPED ({legend.StackedCards.Count})", 16, Theme.Gold, 24, panelH - 128, 300, 22, display: true);
                    float sx = 24;
                    foreach (Card stacked in legend.StackedCards.Take(9))
                    {
                        CardView mini = CardView.Create(t, stacked, true, sx, panelH - 100, 0.4f);
                        Card s = stacked;
                        mini.Clicked = _ => OpenDetail(s);
                        sx += CardView.W * 0.4f + 8;
                    }
                }
            }
            else
            {
                // Same layout as legends: details next to the card, ABILITIES (one icon per trigger) below it.
                var (abilities, info) = TriggerIcons.SplitCardText(card);
                TriggerIcons.PaintAbilities(t, info, ix, top, iw, 402 - top, 18, 12, Theme.TextDim);

                float y = 418;
                if (abilities.Length > 0)
                {
                    Theme.Label(t, "ABILITIES", 16, Theme.Gold, 24, y, 300, 22, display: true);
                    y += 30 + TriggerIcons.PaintAbilities(t, abilities, 24, y + 30, W - 48, 200, 18, 12, Theme.Text) + 24;
                }

                int[] mentioned = CardReferences.For(card);
                if (mentioned != null)
                {
                    Theme.Label(t, "MENTIONED CARDS  ·  hover to see", 15, Theme.Gold, 24, y, W - 48, 22, display: true);
                    PeekRow(t, mentioned, 24, y + 28, W - 48, 0.5f);
                }
            }
        }

        static float InfoChip(Transform t, string text, Color color, float x, float y, float w, Color? textColor = null)
        {
            var chip = Theme.RoundedPanel(t, color, x, y, w, 36, 12, "Chip");
            chip.raycastTarget = false;
            Theme.Label(chip.transform, text, 17, textColor ?? Theme.Text, 14, 0, w - 28, 36, TextAnchor.MiddleLeft, display: true);
            return y + 44;
        }

        const string BuffHex = "#8CFFA0", DebuffHex = "#FF8C8C", StatusHex = "#F5C04A", FireHex = "#FFA060", SpentHex = "#B4B9D2", WantedHex = "#E8C48C";

        /// <summary>One EFFECTS row: its icon, the full text, and a shorter text for when rows go two to a line.</summary>
        readonly struct EffectRow
        {
            public readonly TriggerIcons.EffectIcon Icon;
            public readonly string Full, Short;
            public EffectRow(TriggerIcons.EffectIcon icon, string full, string brief) { Icon = icon; Full = full; Short = brief; }
        }

        /// <summary>
        /// Everything currently affecting a legend, each with what caused it: statuses (Silent Galaxy, stunned, burned,
        /// already attacked) and ATK / max-HP changes per source. Healing is not a change to a stat, so it is not listed.
        /// </summary>
        List<EffectRow> LegendEffects(LegendCard legend)
        {
            var rows = new List<EffectRow>();
            if (!legend.CanAttack)
            {
                // Silent Galaxy (non-Cosmic legends) or Leaves From the Vine (everyone).
                bool leaves = _game.ActiveStage?.Id == EffectCatalogue.LeavesFromTheVineId;
                string stage = leaves ? "Leaves From the Vine" : "Silent Galaxy";
                rows.Add(new EffectRow(TriggerIcons.EffectIcon.NoStrikes,
                    Line(DebuffHex, "Can't deal damage", stage, leaves ? "no legend can deal damage" : "only Cosmic legends can deal damage"),
                    Line(DebuffHex, "Can't deal damage", stage, null)));
            }
            if (legend.IsStunned)
                rows.Add(new EffectRow(TriggerIcons.EffectIcon.Stunned,
                    Line(StatusHex, "Stunned", legend.StunnedBy, "skips its next attack, abilities off"),
                    Line(StatusHex, "Stunned", legend.StunnedBy, null)));
            if (Game.IsWanted(legend))
                rows.Add(new EffectRow(TriggerIcons.EffectIcon.Wanted,
                    Line(WantedHex, "Wanted", "Wanted", "the opponent can only attack Wanted legends"),
                    Line(WantedHex, "Wanted", "Wanted", null)));
            if (legend.IsBurned)
                rows.Add(new EffectRow(TriggerIcons.EffectIcon.Burned,
                    Line(FireHex, "Burned", legend.BurnedBy, "-1 HP at the start of each of its turns"),
                    Line(FireHex, "Burned  -1 HP each turn", legend.BurnedBy, null)));
            foreach (LegendCard.StatModifier m in legend.Modifiers)
            {
                if (m.Power != 0)
                {
                    string s = Line(m.Power > 0 ? BuffHex : DebuffHex, $"{m.Power:+0;-0} ATK", m.Source, null);
                    rows.Add(new EffectRow(m.Power > 0 ? TriggerIcons.EffectIcon.AttackUp : TriggerIcons.EffectIcon.AttackDown, s, s));
                }
                if (m.Health != 0)
                {
                    string s = Line(m.Health > 0 ? BuffHex : DebuffHex, $"{m.Health:+0;-0} max HP", m.Source, null);
                    rows.Add(new EffectRow(m.Health > 0 ? TriggerIcons.EffectIcon.HealthUp : TriggerIcons.EffectIcon.HealthDown, s, s));
                }
            }
            if (legend.AttackedThisTurn && legend.Owner == _game.ActivePlayer)
            {
                string s = Line(SpentHex, "Already attacked", null, "this turn");
                rows.Add(new EffectRow(TriggerIcons.EffectIcon.Attacked, s, s));
            }
            return rows;
        }

        /// <summary>
        /// EFFECTS: one row per effect on the legend (icon + what + source). Rows go two to a line when one column does
        /// not fit above <paramref name="bottom"/>, and the last cell becomes "+N more" if even that is not enough.
        /// Drawn only when there is something to show. Returns the bottom edge.
        /// </summary>
        float EffectsSection(Transform t, LegendCard legend, float x, float y, float w, float bottom)
        {
            List<EffectRow> rows = LegendEffects(legend);
            if (rows.Count == 0) return y;

            const float header = 30, rowH = 28, icon = 22, colGap = 12;
            Theme.Label(t, "EFFECTS", 16, Theme.Gold, x, y + 4, 300, 22, display: true);
            float top = y + header;
            int fitRows = Mathf.Max(1, (int)((bottom - top) / rowH));

            int columns = rows.Count <= fitRows ? 1 : 2;
            int cells = Mathf.Min(rows.Count, fitRows * columns);
            int hidden = rows.Count - cells;
            if (hidden > 0) { cells--; hidden++; } // the last cell says how many are not shown

            float cellW = (w - (columns - 1) * colGap) / columns;
            int perColumn = (cells + (hidden > 0 ? 1 : 0) + columns - 1) / columns;
            for (int i = 0; i <= cells; i++)
            {
                if (i == cells && hidden == 0) break;
                float cx = x + (i / perColumn) * (cellW + colGap), cy = top + (i % perColumn) * rowH;
                string text;
                float tx = cx;
                if (i < cells)
                {
                    TriggerIcons.MiniEffect(t, rows[i].Icon, cx, cy + (rowH - icon) / 2, icon);
                    tx += icon + 8;
                    text = columns == 1 ? rows[i].Full : rows[i].Short;
                }
                else
                {
                    // "+N more" opens the full list over the panel.
                    text = $"<color=#F5C04A>+{hidden} more</color>  <color=#9AA0C0>· click to see all</color>";
                    var hit = Theme.RoundedPanel(t, new Color(1, 1, 1, 0.06f), cx - 6, cy + 1, cellW + 6, rowH - 2, 10, "MoreEffects");
                    float bottomEdge = cy + rowH + 8;
                    UI.Clicks(hit.gameObject, _ => ToggleAllEffects(t, rows, x, bottomEdge, w));
                    hit.gameObject.AddComponent<HoverFx>().Init(hit, new Color(1, 1, 1, 0.06f), new Color(1, 1, 1, 0.14f), 1f);
                }

                EffectLabel(t, text, tx, cy, cx + cellW - tx, rowH);
            }
            return top + perColumn * rowH + 6;
        }

        static Text EffectLabel(Transform t, string text, float x, float y, float w, float h)
        {
            var label = Theme.Label(t, text, 15, Theme.Text, x, y, w, h, TextAnchor.MiddleLeft);
            label.supportRichText = true;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 15;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// Every effect on the legend, one full row each, in a box over the detail panel whose bottom sits at
        /// <paramref name="bottom"/>. Clicking it (or "+N more" again) closes it.
        /// </summary>
        void ToggleAllEffects(Transform t, List<EffectRow> rows, float x, float bottom, float w)
        {
            if (t.Find("AllEffects") is Transform open) { Destroy(open.gameObject); return; }

            const float header = 44, rowH = 28, icon = 22, pad = 14;
            float h = header + rows.Count * rowH + pad;
            float y = Mathf.Max(80, bottom - h);
            var box = Theme.RoundedPanel(t, new Color32(20, 24, 52, 255), x - 8, y, w + 16, h, 16, "AllEffects");
            Quiet(Theme.RoundedPanel(box.transform, Theme.Gold, 0, 0, w + 16, h, 16, "Line", outline: true));
            UI.Clicks(box.gameObject, _ => Destroy(box.gameObject));
            Transform b = box.transform;
            Theme.Label(b, $"ALL EFFECTS ({rows.Count})", 16, Theme.Gold, pad, 12, 300, 22, display: true).raycastTarget = false;
            Theme.Label(b, "click to close", 13, Theme.TextDim, w + 16 - pad - 200, 12, 200, 22, TextAnchor.MiddleRight).raycastTarget = false;
            for (int i = 0; i < rows.Count; i++)
            {
                float ry = header + i * rowH;
                TriggerIcons.MiniEffect(b, rows[i].Icon, pad, ry + (rowH - icon) / 2, icon);
                EffectLabel(b, rows[i].Full, pad + icon + 8, ry, w + 16 - 2 * pad - icon - 8, rowH);
            }
        }

        /// <summary>"+5 ATK  Super Saiyan": the effect coloured, then its source, then an optional dim note.</summary>
        static string Line(string hex, string what, string source, string note)
        {
            string s = $"<color={hex}>{what}</color>";
            if (!string.IsNullOrEmpty(source)) s += $"  {source}";
            if (!string.IsNullOrEmpty(note)) s += $"  <color=#9AA0C0>{note}</color>";
            return s;
        }

        void AttackRow(Transform t, LegendCard legend, int index, float x, float y, float w)
        {
            Attack attack = legend.Attacks[index];
            bool meets = attack.MeetsWeaponRequirements(legend);
            string reason = null;
            if (!meets) reason = "Needs " + RequirementText(attack);
            else if (!CanControlAttacks(legend)) reason = legend.Owner == _game.ActivePlayer ? null : "Only on its owner's turn";
            else reason = _game.CanUseAttack(legend, attack);
            bool usable = reason == null && CanControlAttacks(legend);

            var row = Theme.RoundedPanel(t, usable ? new Color(1, 1, 1, 0.09f) : new Color(1, 1, 1, 0.035f), x, y, w, 74, 14, "Attack " + attack.Name);
            Quiet(Theme.RoundedPanel(row.transform, usable ? new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.6f) : new Color(1, 1, 1, 0.1f), 0, 0, w, 74, 14, "Line", outline: true));
            if (usable)
            {
                var b = row.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                int i = index;
                b.onClick.AddListener(() => OnAttackClicked(legend, i));
                row.gameObject.AddComponent<HoverFx>().Init(row, new Color(1, 1, 1, 0.09f), new Color(1, 1, 1, 0.18f), 1.015f);
            }
            else row.raycastTarget = false;
            Transform rt = row.transform;
            float alpha = usable ? 1f : 0.55f;

            // Weapon requirement icons
            float ix = 14;
            ix = RequirementChip(rt, attack.WeaponOne, attack.WeaponOneAmount, attack.WeaponOneBurnAmount, ix, alpha);
            if (attack.WeaponTwo != null)
                ix = RequirementChip(rt, attack.WeaponTwo.Value, attack.WeaponTwoAmount, attack.WeaponTwoBurnAmount, ix, alpha);

            // Name + tags / reason
            var name = Theme.Label(rt, attack.Name, 22, new Color(1, 1, 1, alpha), ix + 6, 8, w - ix - 120, 30, TextAnchor.MiddleLeft, display: true);
            name.verticalOverflow = VerticalWrapMode.Truncate;
            var tags = new List<string>();
            if (attack.EffectText != null) tags.Add(attack.EffectText);
            if (attack.Kind != AttackKind.Strike) tags.Add(attack.Kind.ToString().ToUpperInvariant());
            if (attack.InstaEffect) tags.Add("INSTANT");
            if (attack.MultiHit) tags.Add("HITS ALL ENEMIES");
            if (attack.FriendlyFire) tags.Add("TARGETS YOUR LEGEND");
            if (attack.OneHitKO) tags.Add("KNOCK-OUT");
            int burns = attack.WeaponOneBurnAmount + attack.WeaponTwoBurnAmount;
            if (burns > 0) tags.Add($"BURNS {burns} WEAPON{(burns > 1 ? "S" : "")}");
            string sub = reason ?? (tags.Count > 0 ? string.Join("  ·  ", tags) : "Ready");
            Color subColor = reason != null ? new Color32(255, 160, 160, 255) : usable ? ReadyGreen : Theme.TextDim;
            var subText = Theme.Label(rt, sub, 14, subColor, ix + 6, 40, w - ix - 120, 24, TextAnchor.MiddleLeft, display: true);
            subText.verticalOverflow = VerticalWrapMode.Truncate;
            subText.resizeTextForBestFit = true;
            subText.resizeTextMinSize = 9;
            subText.resizeTextMaxSize = 14;

            // Damage
            string damage;
            string damageSub = "DMG";
            if (attack.OneHitKO) { damage = "KO"; damageSub = ""; }
            else if (attack.Kind != AttackKind.Strike) { damage = "—"; damageSub = attack.Kind.ToString().ToUpperInvariant(); }
            else
            {
                int baseDamage = Attack.CalculateBaseDamage(legend, attack);
                int bonus = Attack.CalculateElementalBonus(legend, attack);
                damage = (baseDamage + bonus).ToString();
                if (bonus > 0) damageSub = $"+{bonus} ELEMENT";
            }
            int[] mentioned = CardReferences.For(attack);
            if (mentioned != null)
            {
                // The card this attack makes or sets, as a hoverable thumbnail where the damage would be.
                float tw = CardView.W * 0.3f;
                for (int m = 0; m < mentioned.Length; m++)
                    CardPeek.Create(rt, mentioned[m], w - 14 - (m + 1) * (tw + 8) + 8, 7, 0.3f, _drag, PeekSpot(y + 7));
                return;
            }
            Theme.Label(rt, damage, 36, new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, alpha), w - 112, 4, 98, 44, TextAnchor.MiddleRight, display: true);
            Theme.Label(rt, damageSub, 12, new Color(1, 1, 1, 0.6f * alpha), w - 132, 46, 118, 18, TextAnchor.MiddleRight, display: true);
        }

        static float RequirementChip(Transform parent, Weapon weapon, int amount, int burn, float x, float alpha)
        {
            var chip = Theme.RoundedPanel(parent, new Color(0, 0, 0, 0.4f), x, 13, 64, 48, 12, "Req " + weapon);
            chip.raycastTarget = false;
            Texture2D icon = Art.Weapon(weapon);
            if (icon != null)
            {
                var img = UI.Picture(chip.transform, icon, 6, 7, 34, 34, cover: false);
                img.color = new Color(1, 1, 1, alpha);
            }
            else
            {
                Theme.Label(chip.transform, "ANY", 13, new Color(1, 1, 1, alpha), 2, 0, 40, 48, TextAnchor.MiddleCenter, display: true);
            }
            Theme.Label(chip.transform, amount == 0 ? "—" : $"×{amount}", 16, new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, alpha), 36, 0, 26, 48, TextAnchor.MiddleCenter, display: true);
            if (burn > 0) TriggerIcons.BurnCount(chip.transform, burn, 42, -12, 28); // flame with the number of weapons burned
            return x + 72;
        }

        static string RequirementText(Attack attack)
        {
            string one = $"{attack.WeaponOneAmount}× {(attack.WeaponOne == Weapon.Any ? "any weapon" : Pretty(attack.WeaponOne))}";
            if (attack.WeaponTwo == null) return one;
            return $"{one} + {attack.WeaponTwoAmount}× {Pretty(attack.WeaponTwo.Value)}";
        }

        static string Pretty(Weapon w)
        {
            switch (w)
            {
                case Weapon.RocketLance: return "Rocket Lance";
                case Weapon.BattleBoots: return "Battle Boots";
                default: return w.ToString();
            }
        }

        bool CanControlAttacks(LegendCard legend)
        {
            return CanAct && legend.Owner == _game.ActivePlayer && legend.OnPlayingField;
        }

        void OnAttackClicked(LegendCard legend, int attackIndex)
        {
            if (!CanAct)
            {
                _app.Toast("It is not your turn!");
                return;
            }
            Attack attack = legend.Attacks[attackIndex];
            string error = _game.CanUseAttack(legend, attack);
            if (error != null)
            {
                _app.Toast(error);
                return;
            }
            AttackMode mode = _game.GetAttackMode(legend, attack);
            if (mode == AttackMode.OwnLegend || mode == AttackMode.EnemyLegend)
            {
                Player side = mode == AttackMode.OwnLegend ? legend.Owner : _game.GetOpponent(legend.Owner);
                if (side.PlayingField.Count == 0)
                {
                    _app.Toast("There is no legend to target.");
                    return;
                }
                _targetAttacker = legend;
                _targetAttackIndex = attackIndex;
                _targetOwnSide = mode == AttackMode.OwnLegend;
                CloseDetail();
                Refresh();
                StartTargetLine();
                return;
            }
            SubmitAttack(legend, attackIndex, -1);
        }

        // ───────────────────────────── Overlays ─────────────────────────────

        RectTransform ShowOverlay(float opacity)
        {
            ResetHandOverlay();
            UI.Clear(_overlay);
            var dim = UI.FullPanel(_overlay, new Color(Theme.Night.r, Theme.Night.g, Theme.Night.b, opacity), "Dim");
            return dim.rectTransform;
        }

        void CloseOverlay()
        {
            ResetHandOverlay();
            UI.Clear(_overlay);
        }

        void ResetHandOverlay()
        {
            _burn = null;
            _handOverlay = false;
            _handTimerLabel = null;
            _opponentHandLabel = null;
        }

        Transform Dialog(RectTransform dim, string title, string message, string eyebrow = null)
        {
            var box = Theme.GlassPanel(dim, 560, 330, 800, 420, 28, "Dialog");
            box.color = new Color32(14, 18, 42, 255);
            box.gameObject.AddComponent<SlideIn>().Offset = new Vector2(0, -30);
            if (eyebrow != null)
                Theme.Label(box.transform, eyebrow, 18, Theme.Gold, 0, 38, 800, 26, TextAnchor.MiddleCenter, display: true);
            var t = Theme.Label(box.transform, title, 58, Theme.Text, 20, 66, 760, 80, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(t, 3, 0.6f);
            Theme.Label(box.transform, message, 22, Theme.TextDim, 60, 156, 680, 110, TextAnchor.MiddleCenter);
            return box.transform;
        }

        void ShowHandoff()
        {
            _handoffPending = true;
            CancelTargeting();
            CloseDetail();
            var dim = ShowOverlay(1f); // fully opaque: nothing of the next player's hand may show through
            Player next = _game.Players[_viewer];
            bool choosing = _game.ChoosingStartingHands && !_game.HasChosenStartingHand(_viewer);
            var box = Dialog(dim, next.Name.ToUpperInvariant(), $"Pass the device to {next.Name}.\nThe other player should look away.",
                choosing ? "STARTING HAND" : $"TURN {_game.TurnNumber}");
            Theme.Cta(box, choosing ? "SEE MY HAND" : "START MY TURN", 220, 296, 360, 76, () =>
            {
                _handoffPending = false;
                CloseOverlay();
                Refresh();
                if (choosing) ShowStartingHand();
                else TurnIntro(true);
            }, 30);
        }

        // ───────────────────────────── Starting hand ─────────────────────────────

        /// <summary>Hand indices the viewer marked to shuffle back into the deck.</summary>
        readonly HashSet<int> _redrawPick = new HashSet<int>();

        /// <summary>
        /// Before turn 1: the viewer's starting hand, large. Clicking a card marks it to go back into the deck; the button
        /// keeps the hand or redraws the marked cards (once per match).
        /// </summary>
        void ShowStartingHand()
        {
            var dim = ShowOverlay(0.95f);
            Player me = _game.Players[_viewer];
            Theme.Label(dim, "YOUR STARTING HAND", 18, Theme.Gold, 0, 112, 1920, 26, TextAnchor.MiddleCenter, display: true);
            var title = Theme.Label(dim, "KEEP OR REDRAW", 58, Theme.Text, 0, 142, 1920, 80, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(title, 3, 0.6f);
            Theme.Label(dim, "Click the cards you want to shuffle back into your deck. You draw as many new cards. You can do this once.",
                20, Theme.TextDim, 0, 226, 1920, 30, TextAnchor.MiddleCenter);
            StartHandClock();
            _handTimerLabel = Theme.Label(dim, "", 20, Theme.TextDim, 0, 772, 1920, 28, TextAnchor.MiddleCenter, display: true);
            _handTimerLabel.raycastTarget = false;
            OpponentHandLine(dim, 806);

            const float scale = 1.3f, gap = 18;
            float w = CardView.W * scale;
            int n = me.Hand.Count;
            float start = 960 - (n * w + (n - 1) * gap) / 2;
            for (int i = 0; i < n; i++)
            {
                int index = i;
                bool picked = _redrawPick.Contains(i);
                CardView v = CardView.Create(dim, me.Hand[i], true, start + i * (w + gap), 300 + (picked ? 36 : 0), scale);
                v.Clicked = _ =>
                {
                    if (!_redrawPick.Remove(index)) _redrawPick.Add(index);
                    Sfx.Play("ui_click", 0.5f);
                    ShowStartingHand();
                };
                if (picked)
                {
                    v.SetDimmed(true, 0.85f);
                    v.AddCenterBanner("BACK TO DECK", Theme.Gold);
                }
            }

            int count = _redrawPick.Count;
            string label = count == 0 ? "KEEP HAND" : count == 1 ? "REDRAW 1 CARD" : $"REDRAW {count} CARDS";
            Theme.Cta(dim, label, 960 - 210, 680, 420, 76, ConfirmStartingHand, 30);
        }

        void ConfirmStartingHand()
        {
            int me = _viewer, mask = 0;
            _handDeadline = -1; // chosen: the clock stops
            List<Card> hand = _game.Players[me].Hand;
            foreach (int i in _redrawPick) mask |= 1 << i;
            var oldBySlot = _redrawPick.ToDictionary(i => i, i => hand[i]); // the new cards take these same slots
            _redrawPick.Clear();
            CloseOverlay();
            if (!Submit(GameCommand.RedrawStartingHand(me, mask))) return;
            RememberHand(me); // seen: turned over in place at the start of the first turn instead of dealt again
            bool startedTurnOne = !_game.ChoosingStartingHands; // our choice was the last one

            void Continue()
            {
                if (_hotSeat)
                {
                    // Next: the other player's starting hand, or (both chosen) the first turn.
                    _viewer = _game.ChoosingStartingHands ? 1 - me : _game.ActiveIndex;
                    ShowHandoff();
                    Refresh();
                }
                else
                {
                    CloseOverlay();
                    // Online: if our choice was the last one, turn 1 begins now (if the opponent's choice arrives later,
                    // BothHandsChosen shows the banner).
                    if (startedTurnOne) TurnIntro(false);
                }
            }

            if (mask == 0)
            {
                // Kept the hand. Online, while the opponent is still choosing, we wait on the hand screen.
                if (!_hotSeat && _game.ChoosingStartingHands) ShowWaitingForHands();
                else Continue();
                return;
            }

            // The new starting hand. If this choice also started turn 1 and it's our turn, the turn's draw (the last card)
            // isn't part of the starting hand.
            var newHand = hand.ToList();
            if (!_game.ChoosingStartingHands && _game.ActiveIndex == me && newHand.Count > Game.StartingHandCards)
                newHand.RemoveAt(newHand.Count - 1);
            ShowRedrawResult(newHand, oldBySlot, Continue);
        }

        /// <summary>
        /// After a redraw: the new starting hand, large. Each swapped slot animates in place, one after another: the old
        /// card sinks and fades out, then the new card rises into the same slot, fading in while it turns over, and stays
        /// marked NEW. The other cards don't move. CONTINUE moves on (next player or the first turn).
        /// </summary>
        void ShowRedrawResult(List<Card> hand, Dictionary<int, Card> oldBySlot, Action next)
        {
            _handoffPending = false; // shown before any hand-off to the other player
            var dim = ShowOverlay(0.95f);
            _handOverlay = true; // the turn clock waits until this is closed
            Theme.Label(dim, "YOUR STARTING HAND", 18, Theme.Gold, 0, 112, 1920, 26, TextAnchor.MiddleCenter, display: true);
            var title = Theme.Label(dim, "YOUR NEW HAND", 58, Theme.Text, 0, 142, 1920, 80, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(title, 3, 0.6f);
            var backNames = oldBySlot.OrderBy(kv => kv.Key).Select(kv => kv.Value.Name);
            int fresh = oldBySlot.Count;
            Theme.Label(dim, $"Shuffled back: {string.Join(", ", backNames)}.  You drew {fresh} new {(fresh == 1 ? "card" : "cards")}, marked NEW.",
                20, Theme.TextDim, 0, 226, 1920, 30, TextAnchor.MiddleCenter);

            const float scale = 1.3f, gap = 18, y = 300;
            float w = CardView.W * scale;
            int n = hand.Count;
            float start = 960 - (n * w + (n - 1) * gap) / 2;
            float delay = 0.3f;
            for (int i = 0; i < n; i++)
            {
                float x = start + i * (w + gap);
                if (!oldBySlot.TryGetValue(i, out Card old))
                {
                    CardView.Create(dim, hand[i], true, x, y, scale);
                    continue;
                }
                // The card that goes back, as it looked when picked (lowered, dimmed, BACK TO DECK) ...
                CardView oldView = CardView.Create(dim, old, true, x, y + 36, scale);
                oldView.SetDimmed(true, 0.85f);
                oldView.AddCenterBanner("BACK TO DECK", Theme.Gold);
                // ... and the new one, marked NEW, with a face-down copy for the first half of its turn.
                CardView newView = CardView.Create(dim, hand[i], true, x, y, scale);
                newView.SetGlow(Theme.Gold, 0.6f);
                newView.AddStatus("NEW", new Color(0.04f, 0.05f, 0.14f, 0.95f), Theme.Gold);
                CardView back = CardView.Create(dim, hand[i], false, x, y, scale);
                StartCoroutine(SwapSlot(oldView, newView, back, x, y, scale, delay));
                delay += 0.3f;
            }

            if (!_hotSeat && _game.ChoosingStartingHands)
            {
                // Online, the opponent is still choosing: stay here; BothHandsChosen closes this once they're done.
                OpponentHandLine(dim, 700);
                return;
            }
            Theme.Cta(dim, "CONTINUE", 960 - 210, 680, 420, 76, () => { CloseOverlay(); next(); }, 30);
        }

        /// <summary>Online, after keeping the hand: the hand, and whether the opponent is still choosing theirs.</summary>
        void ShowWaitingForHands()
        {
            var dim = ShowOverlay(0.95f);
            _handOverlay = true;
            Player me = _game.Players[_viewer];
            Theme.Label(dim, "YOUR STARTING HAND", 18, Theme.Gold, 0, 112, 1920, 26, TextAnchor.MiddleCenter, display: true);
            var title = Theme.Label(dim, "HAND KEPT", 58, Theme.Text, 0, 142, 1920, 80, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(title, 3, 0.6f);
            Theme.Label(dim, "The first turn starts as soon as both players have chosen.", 20, Theme.TextDim, 0, 226, 1920, 30, TextAnchor.MiddleCenter);

            const float scale = 1.3f, gap = 18;
            float w = CardView.W * scale;
            int n = me.Hand.Count;
            float start = 960 - (n * w + (n - 1) * gap) / 2;
            for (int i = 0; i < n; i++)
                CardView.Create(dim, me.Hand[i], true, start + i * (w + gap), 300, scale);
            OpponentHandLine(dim, 700);
        }

        /// <summary>Online: the opponent's choice arrived after ours. Leave the hand screen and start turn 1.</summary>
        void BothHandsChosen()
        {
            if (!_handOverlay)
            {
                TurnIntro(false);
                return;
            }
            if (_opponentHandLabel != null) _opponentHandLabel.text = OpponentHandStatus(); // "... is ready"
            // A moment to read it (and to let a redraw's cards land) before the board.
            After(1.2f, () =>
            {
                if (_ended || !_handOverlay) return;
                CloseOverlay();
                Refresh();
                TurnIntro(false);
            });
        }

        /// <summary>Old card sinks and fades out; the new card rises into the slot, fading in while it turns over.</summary>
        static IEnumerator SwapSlot(CardView oldView, CardView newView, CardView back, float x, float y, float scale, float delay)
        {
            float w = CardView.W * scale;
            CanvasGroup Group(CardView v) => v.GetComponent<CanvasGroup>();
            void Place(CardView v, float top, float scaleX)
            {
                v.Rect.localScale = new Vector3(scale * scaleX, scale, 1);
                v.Rect.anchoredPosition = new Vector2(x + w * (1 - scaleX) / 2, -top); // turn around the card's centre
            }
            foreach (var g in new[] { Group(newView), Group(back) }) { g.alpha = 0; g.blocksRaycasts = false; }

            for (float t = 0; t < delay; t += Time.unscaledDeltaTime) yield return null;
            if (oldView == null || newView == null || back == null) yield break; // closed early

            // Out: sink and fade.
            const float outTime = 0.32f, inTime = 0.55f, sink = 60, rise = 90;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / outTime)
            {
                if (oldView == null) yield break;
                float e = t * t;
                Place(oldView, y + 36 + sink * e, 1);
                Group(oldView).alpha = 1 - t;
                yield return null;
            }
            if (oldView != null) Destroy(oldView.gameObject);
            Sfx.Play("card_draw", 0.5f);

            // In: rise into the slot, fading in, back first, then the face.
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / inTime)
            {
                if (newView == null || back == null) yield break;
                float top = y + rise * (1 - Theme.EaseOut(t));
                float scaleX = Mathf.Max(0.04f, Mathf.Abs(Mathf.Cos(t * Mathf.PI)));
                float alpha = Mathf.Clamp01(t * 2.2f);
                bool showBack = t < 0.5f;
                Place(showBack ? back : newView, top, scaleX);
                Group(back).alpha = showBack ? alpha : 0;
                Group(newView).alpha = showBack ? 0 : alpha;
                yield return null;
            }
            if (back != null) Destroy(back.gameObject);
            if (newView == null) yield break;
            Place(newView, y, 1);
            Group(newView).alpha = 1;
        }

        void ShowGameOver()
        {
            if (_ended) return;
            _ended = true;
            CancelTargeting();
            Player winner = _game.Winner;
            Player loser = winner != null ? _game.GetOpponent(winner) : null;
            bool iWon = !_hotSeat && winner != null && winner.Index == _localIndex;
            StarterDecks.Starter unlocked = UnlockBotDeck(iWon); // saved now, even if the player quits during the animation
            // Let the final blow and the K.O. play out before the result appears.
            GameOverFx(winner, loser, 0.5f);
            string title = _hotSeat || InShowcase ? $"{winner?.Name.ToUpperInvariant()} WINS" : iWon ? "VICTORY" : "DEFEAT";
            string message = $"{loser?.Name} has been defeated after {_game.TurnNumber} turns.{BotDeckLine(iWon, unlocked)}";

            // The result with its buttons. After an unlock it comes back when the player leaves the reveal.
            void ShowResult()
            {
                var dim = ShowOverlay(0.75f);
                var box = Dialog(dim, title, message, "MATCH OVER");
                if (_rematch != null)
                {
                    // Bot games: straight into another match at the same difficulty (with a deck still to unlock).
                    Theme.Cta(box, "PLAY AGAIN", 60, 296, 330, 76, Rematch, 30);
                    Theme.Ghost(box, "BACK TO MENU", 410, 296, 330, 76, LeaveMatch, 26);
                }
                else Theme.Cta(box, "BACK TO MENU", 220, 296, 360, 76, LeaveMatch, 30);
            }

            After(loser != null ? 2.8f : 0, () =>
            {
                if (unlocked == null) { ShowResult(); return; }
                // First the victory with one way on, then the reveal on its own screen, then the result again.
                var dim = ShowOverlay(0.75f);
                var box = Dialog(dim, title, message, "MATCH OVER");
                Theme.Cta(box, "SEE YOUR NEW DECK", 190, 296, 420, 76, () => ShowDeckUnlocked(unlocked, ShowResult), 28);
            });
            Refresh();
        }

        void EndMatch(string title, string reason)
        {
            if (_ended) return;
            _ended = true;
            CancelTargeting();
            var dim = ShowOverlay(0.75f);
            var box = Dialog(dim, title.ToUpperInvariant(), reason, "MATCH ENDED");
            Theme.Cta(box, "BACK TO MENU", 220, 296, 360, 76, LeaveMatch, 30);
            Refresh();
        }

        void Desync(string reason)
        {
            _net?.Send(Protocol.Quit);
            EndMatch("Out of sync", "The game went out of sync and was stopped.\n" + reason);
        }

        void LeaveMatch()
        {
            _net?.Send(Protocol.Quit);
            _net?.Dispose();
            _net = null;
            _app.ShowMenu();
        }

        void Rematch()
        {
            _net?.Dispose();
            _net = null;
            _rematch();
        }
    }

    /// <summary>Lifts a hand card towards the player while the pointer is over it.</summary>
    public sealed class HandHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        RectTransform _rt;
        Vector2 _base;
        bool _over, _ready;
        float _t;

        void Start() => _rt = (RectTransform)transform;

        /// <summary>
        /// Stacks the hand left-to-right in hand order (each card overlaps the one before it), with the hovered card,
        /// if any, on top. Recomputed every time so hovering never leaves the order scrambled.
        /// </summary>
        static void Restack(Transform parent, HandHover hovered)
        {
            if (parent == null) return;
            var cards = new List<(int index, Transform t)>();
            foreach (Transform child in parent)
            {
                if (child.GetComponent<HandHover>() == null) continue;
                var view = child.GetComponent<CardView>();
                cards.Add((view != null ? view.Index : 0, child));
            }
            cards.Sort((a, b) => a.index.CompareTo(b.index));
            foreach (var c in cards) c.t.SetAsLastSibling();
            if (hovered != null) hovered.transform.SetAsLastSibling();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (e.dragging) return;
            _over = true;
            Restack(transform.parent, this);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _over = false;
            Restack(transform.parent, null);
        }

        void Update()
        {
            if (_rt == null || transform.parent == null || transform.parent.name == "Drag") return;
            // Wait for a deal / slide animation to settle before taking over the position.
            if (GetComponent<CardFlight>() != null) { _ready = false; return; }
            if (!_ready)
            {
                _base = _rt.anchoredPosition;
                _ready = true;
                Restack(transform.parent, _over ? this : null); // flights lift cards to the top; put the hand back in order
            }
            _t = Mathf.MoveTowards(_t, _over ? 1 : 0, Time.unscaledDeltaTime * 9);
            float e = Theme.EaseOut(_t);
            _rt.anchoredPosition = _base + new Vector2(0, 46 * e);
            float s = 1 + 0.12f * e;
            _rt.localScale = new Vector3(s, s, 1);
        }
    }
}
