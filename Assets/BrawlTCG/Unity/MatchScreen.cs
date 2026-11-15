using System;
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
        static readonly Rect TopPanel = new Rect(24, 20, 400, 176);
        static readonly Rect BottomPanel = new Rect(24, 884, 400, 176);
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
        bool _hotSeat;

        /// <summary>Player shown at the bottom (whose hand is visible).</summary>
        int _viewer;
        bool _handoffPending;
        bool _ended;

        RectTransform _board, _cards, _detail, _drag, _overlay;
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
            _localIndex = setup.LocalIndex;

            BuildBackdrop();
            var background = UI.FullPanel(transform, new Color(0, 0, 0, 0), "ClickCatcher");
            UI.Clicks(background.gameObject, e => OnBackgroundClicked());
            _board = UI.Stretch(UI.Rect("Board", transform));
            _cards = UI.Stretch(UI.Rect("Cards", transform));
            _fx = UI.Stretch(UI.Rect("Fx", transform));
            _top = UI.Stretch(UI.Rect("FxTop", transform));
            _shaker = gameObject.AddComponent<Shaker>();
            _detail = UI.Stretch(UI.Rect("Detail", transform));
            _drag = UI.Stretch(UI.Rect("Drag", transform));
            _overlay = UI.Stretch(UI.Rect("Overlay", transform));
            _menu = UI.Stretch(UI.Rect("OptionsMenu", transform));

            _game.Notification += OnNotification;
            _game.Start();
            _net?.StartReceiving();

            _viewer = _hotSeat ? _game.ActiveIndex : _localIndex;
            if (_hotSeat) ShowHandoff();
            Refresh();
            if (!_hotSeat) TurnIntro(true);
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
            raw.color = new Color(0.36f, 0.38f, 0.52f, 1f);
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
            }
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
        bool CanAct => !_ended && !_game.IsOver && !_handoffPending && IsLocallyControlled(_game.ActiveIndex);

        // ───────────────────────────── Frame loop ─────────────────────────────

        void Update()
        {
            PumpNetwork();

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool typing = UI.IsTyping();

            if (kb != null && kb.spaceKey.wasPressedThisFrame && !typing && _overlay.childCount == 0 && !OptionsOpen && CanAct)
                Submit(GameCommand.EndTurn(_game.ActiveIndex));

            // Esc steps back: closes the options menu, cancels targeting, closes the card detail, or opens the options.
            // Right-click does the same except opening the options.
            bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
            bool right = mouse != null && mouse.rightButton.wasPressedThisFrame;
            if (esc || right)
            {
                if (OptionsOpen) { if (esc) CloseOptions(); }
                else if (Targeting) { CancelTargeting(); Refresh(); }
                else if (_detailCard != null) CloseDetail();
                else if (esc) OpenOptions();
            }
        }

        void PumpNetwork()
        {
            if (_net == null || _ended) return;
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
                _viewer = _game.ActiveIndex;
                ShowHandoff();
            }
            Refresh();
            if (_game.IsOver) ShowGameOver();
            Animate(before);
            return true;
        }

        void EndTurnClicked()
        {
            if (!CanAct) return;
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
            Transform t = panel.transform;
            if (active) Quiet(Theme.RoundedPanel(t, Theme.Gold, 0, 0, r.width, r.height, 20, "ActiveLine", outline: true));

            // Avatar
            var avatar = Quiet(Theme.RoundedPanel(t, active ? Theme.Gold : new Color32(70, 80, 120, 255), 16, 16, 62, 62, 31, "Avatar"));
            string initial = p.Name.Length > 0 ? p.Name.Substring(0, 1).ToUpperInvariant() : "?";
            Theme.Label(avatar.transform, initial, 30, active ? Theme.Ink : Theme.Text, 0, 0, 62, 62, TextAnchor.MiddleCenter, display: true);

            var name = Theme.Label(t, p.Name, 26, Theme.Text, 92, 12, 220, 32, TextAnchor.MiddleLeft, display: true);
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 14;
            name.resizeTextMaxSize = 26;
            string role = _hotSeat ? $"PLAYER {p.Index + 1}" : IsLocallyControlled(p.Index) ? "YOU" : "OPPONENT";
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
            int shown = Mathf.Min(Mathf.Max(total, available), 12);
            Theme.Label(t, "ESSENCE", 13, Theme.TextDim, 16, 124, 90, 18, display: true);
            for (int i = 0; i < shown; i++)
            {
                bool filled = i < available;
                var gem = UI.Panel(t, filled ? new Color32(176, 120, 255, 255) : new Color(1, 1, 1, 0.14f), 20 + i * 24, 152, 15, 15, "Gem");
                gem.sprite = Theme.Rounded;
                gem.type = Image.Type.Sliced;
                gem.pixelsPerUnitMultiplier = 8;
                gem.raycastTarget = false;
                gem.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                gem.rectTransform.anchoredPosition = new Vector2(27 + i * 24, -158);
                gem.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                if (filled)
                {
                    var glow = UI.Panel(t, new Color(0.7f, 0.5f, 1f, 0.5f), 12 + i * 24, 144, 30, 30, "GemGlow");
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
            Quiet(Theme.RoundedPanel(bg.transform, new Color(1, 1, 1, 0.08f), 0, 0, lane.width, lane.height, 22, "Line", outline: true));
            string label = bottom ? (_hotSeat || IsLocallyControlled(p.Index) ? "YOUR FIELD" : $"{p.Name.ToUpperInvariant()}'S FIELD") : $"{p.Name.ToUpperInvariant()}'S FIELD";
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
                for (int k = shown - 1; k >= 0; k--)
                    AddCard(legend.StackedCards[k], true, x, y - 14 * (k + 1), p, BoardZone.Stacked, k);

                CardView view = AddCard(legend, true, x, y, p, BoardZone.PlayingField, slot, tapped: legend.IsTapped);
                _legendViews.Add(view);

                if (legend.IsTapped) { /* ribbon is painted on the card */ }
                else if (legend.AttackedThisTurn && p == _game.ActivePlayer) view.AddStatus("ATTACKED", new Color(0.1f, 0.1f, 0.18f, 0.92f), Theme.TextDim);
                else if (!legend.CanAttack) view.AddStatus("CAN'T ATTACK", new Color(0.35f, 0.08f, 0.12f, 0.95f), new Color32(255, 170, 170, 255));

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
            Quiet(Theme.RoundedPanel(strip.transform, new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.25f), 0, 0, Strip.width, Strip.height, 20, "Line", outline: true));
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
            if (_game.IsOver)
            {
                title = "MATCH OVER";
                titleColor = Theme.Gold;
            }
            else if (CanAct)
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
            Theme.Label(t, title, 34, titleColor, 28, 8, 640, 46, TextAnchor.MiddleLeft, display: true);
            string sub = $"Turn {_game.TurnNumber}";
            if (CanAct) sub += "  ·  Drag cards onto the board, click a legend to attack, Space ends your turn";
            else if (!_game.IsOver && !_hotSeat) sub += "  ·  Waiting for your opponent";
            Theme.Label(t, sub, 16, Theme.TextDim, 30, 54, 760, 26, TextAnchor.MiddleLeft);

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
            var title = Theme.Label(_board, "STAGE", 16, Theme.Gold, StageSlot.x, StageSlot.y - 30, 200, 22, display: true);
            title.raycastTarget = false;
            var slot = Quiet(Theme.RoundedPanel(_board, new Color(1, 1, 1, 0.05f), StageSlot.x, StageSlot.y, StageSlot.width, StageSlot.height, 14, "StageSlot"));
            Quiet(Theme.RoundedPanel(slot.transform, new Color(1, 1, 1, 0.12f), 0, 0, StageSlot.width, StageSlot.height, 14, "Line", outline: true));
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
            var text = Theme.Label(_board, effects, 15, Theme.Text, StageInfo.x, StageInfo.y + 62, StageInfo.width, StageInfo.height - 62);
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 15;
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
                if (showPlayable && IsPlayable(p, i))
                {
                    v.SetGlow(PlayableGlow, 0.5f);
                    v.gameObject.AddComponent<GlowPulse>();
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
                    for (int t = -1; t < Game.MaxLegendsOnField; t++)
                        if (_game.Validate(GameCommand.PlayBattle(pi, hand, t)) == null) return true;
                    return false;
            }
        }

        // ── Bottom-right corner: help + leave ──

        void DrawCorner()
        {
            GearButton(_board, 1840, 20, 56);
            var help = Theme.GlassPanel(_board, 1508, 884, 388, 176, 20, "Help");
            help.raycastTarget = false;
            Theme.Label(help.transform, "HOW TO PLAY", 15, Theme.Gold, 20, 14, 340, 20, display: true);
            Theme.Label(help.transform,
                "Drag cards from your hand onto the board.\nClick any card to inspect it.\nClick your legend, pick an attack, then a target.\nSpace or END TURN passes the turn.\nEsc opens the options.",
                15, Theme.TextDim, 20, 40, 356, 124);
        }

        // ───────────────────────────── Cards ─────────────────────────────

        CardView AddCard(Card card, bool faceUp, float x, float y, Player owner, BoardZone zone, int index, bool tapped = false, float scale = 1f)
        {
            CardView v = CardView.Create(_cards, card, faceUp, x, y, scale, tapped);
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
                    int attackerIndex = _targetAttacker.Owner.PlayingField.IndexOf(_targetAttacker);
                    Submit(GameCommand.Attack(_targetAttacker.Owner.Index, attackerIndex, _targetAttackIndex, view.Index));
                }
                return;
            }
            if (!view.FaceUp) return;
            OpenDetail(view.Card);
        }

        bool IsValidAttackTarget(Player side, LegendCard legend)
        {
            if (!Targeting || legend.IsTapped) return false;
            Player wanted = _targetOwnSide ? _targetAttacker.Owner : _game.GetOpponent(_targetAttacker.Owner);
            return side == wanted;
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

        void OpenDetail(Card card)
        {
            UI.Clear(_detail);
            _detailCard = card;
            const float X = 1268, Y = 84, W = 628, H = 916;

            var panel = Theme.GlassPanel(_detail, X, Y, W, H, 26, "DetailPanel");
            panel.color = new Color32(11, 14, 34, 255); // fully opaque: in linear colour space even 97% lets bright cards show through
            panel.gameObject.AddComponent<SlideIn>().Offset = new Vector2(40, 0);
            UI.Clicks(panel.gameObject, e => { }); // swallow clicks so the board behind does not react
            Transform t = panel.transform;
            var close = Theme.Ghost(t, "×", W - 62, 18, 44, 44, CloseDetail, 30);
            close.GetComponentInChildren<Text>().font = Theme.Body;

            CardView big = CardView.Create(t, card, true, 24, 24, 1.9f, card is LegendCard l0 && l0.IsTapped);
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

            if (card is LegendCard legend)
            {
                float cy = 206;
                cy = InfoChip(t, $"ATK {legend.Power}" + (legend.Power != legend.PrintedPower ? $"  (base {legend.PrintedPower})" : ""), new Color32(226, 112, 28, 255), ix, cy, iw);
                cy = InfoChip(t, $"HP {legend.CurrentHP} / {legend.BaseHealth}", new Color32(214, 40, 62, 255), ix, cy, iw);
                cy = InfoChip(t, $"WEAPONS  {Pretty(legend.PrimaryWeapon)} / {Pretty(legend.SecondaryWeapon)}", new Color(1, 1, 1, 0.12f), ix, cy, iw);
                foreach (string status in LegendStatuses(legend))
                    cy = InfoChip(t, status, new Color(1, 1, 1, 0.06f), ix, cy, iw, Theme.Gold);

                // Attacks
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

                string effects = CleanEffects(legend.EffectDescription);
                if (!string.IsNullOrWhiteSpace(effects))
                {
                    Theme.Label(t, "ABILITIES", 16, Theme.Gold, 24, y + 4, 300, 22, display: true);
                    var fx = Theme.Label(t, effects, 16, Theme.Text, 24, y + 30, W - 48, 60);
                    fx.verticalOverflow = VerticalWrapMode.Truncate;
                    fx.resizeTextForBestFit = true;
                    fx.resizeTextMinSize = 11;
                    fx.resizeTextMaxSize = 16;
                    y += 96;
                }

                if (legend.StackedCards.Count > 0)
                {
                    Theme.Label(t, $"EQUIPPED ({legend.StackedCards.Count})", 16, Theme.Gold, 24, H - 128, 300, 22, display: true);
                    float sx = 24;
                    foreach (Card stacked in legend.StackedCards.Take(9))
                    {
                        CardView mini = CardView.Create(t, stacked, true, sx, H - 100, 0.4f);
                        Card s = stacked;
                        mini.Clicked = _ => OpenDetail(s);
                        sx += CardView.W * 0.4f + 8;
                    }
                }
            }
            else
            {
                string text = card is StageCard || card is EssenceCard ? CleanEffects(card.Description) : card.Description;
                if (card is EssenceCard) text = "Each Essence card in play gives you 1 Essence at the start of your turn. You may play one Essence card per turn.";
                if (card is WeaponCard weapon) text += $"\n\nIt counts towards attacks that need a {Pretty(weapon.Weapon)} (and attacks that need any weapon). Matching the legend's element adds bonus damage.";
                if (card is BattleCard battle)
                {
                    string target = !battle.TargetRequired ? "No target needed." : battle.FriendlyFire ? "Target: one of your legends." : "Target: an enemy legend.";
                    string kind = battle.Stackable ? "Stays attached to the legend." : "Discarded after use.";
                    text += $"\n\n{target}\n{kind}";
                }
                var desc = Theme.Label(t, text, 19, Theme.Text, ix, 214, iw, 300);
                desc.verticalOverflow = VerticalWrapMode.Truncate;
                desc.resizeTextForBestFit = true;
                desc.resizeTextMinSize = 12;
                desc.resizeTextMaxSize = 19;
            }
        }

        static float InfoChip(Transform t, string text, Color color, float x, float y, float w, Color? textColor = null)
        {
            var chip = Theme.RoundedPanel(t, color, x, y, w, 36, 12, "Chip");
            chip.raycastTarget = false;
            Theme.Label(chip.transform, text, 17, textColor ?? Theme.Text, 14, 0, w - 28, 36, TextAnchor.MiddleLeft, display: true);
            return y + 44;
        }

        IEnumerable<string> LegendStatuses(LegendCard legend)
        {
            if (legend.IsTapped) yield return "TAPPED  ·  skips its next attack";
            if (legend.IsBurned) yield return "BURNED  ·  -1 HP each turn";
            if (!legend.CanAttack) yield return "CAN'T ATTACK RIGHT NOW";
            if (legend.AttackedThisTurn && legend.Owner == _game.ActivePlayer) yield return "ALREADY ATTACKED THIS TURN";
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
            else if (attack.AttackModifier <= -1000) { damage = "—"; damageSub = "EFFECT"; }
            else
            {
                int baseDamage = Attack.CalculateBaseDamage(legend, attack);
                int bonus = Attack.CalculateElementalBonus(legend, attack);
                damage = (baseDamage + bonus).ToString();
                if (bonus > 0) damageSub = $"+{bonus} ELEMENT";
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
            if (burn > 0)
            {
                var flame = Theme.RoundedPanel(chip.transform, new Color32(255, 120, 30, 255), 44, -6, 24, 18, 9, "Burn");
                flame.raycastTarget = false;
                Theme.Label(flame.transform, burn.ToString(), 13, Theme.Ink, 0, 0, 24, 18, TextAnchor.MiddleCenter, display: true);
            }
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
                if (!side.PlayingField.Any(l => !l.IsTapped))
                {
                    _app.Toast("There is no untapped legend to target.");
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
            Submit(GameCommand.Attack(legend.Owner.Index, legend.Owner.PlayingField.IndexOf(legend), attackIndex, -1));
        }

        // ───────────────────────────── Overlays ─────────────────────────────

        RectTransform ShowOverlay(float opacity)
        {
            UI.Clear(_overlay);
            var dim = UI.FullPanel(_overlay, new Color(Theme.Night.r, Theme.Night.g, Theme.Night.b, opacity), "Dim");
            return dim.rectTransform;
        }

        void CloseOverlay() => UI.Clear(_overlay);

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
            var box = Dialog(dim, next.Name.ToUpperInvariant(), $"Pass the device to {next.Name}.\nThe other player should look away.", $"TURN {_game.TurnNumber}");
            Theme.Cta(box, "START MY TURN", 220, 296, 360, 76, () =>
            {
                _handoffPending = false;
                CloseOverlay();
                Refresh();
                TurnIntro(true);
            }, 30);
        }

        void ShowGameOver()
        {
            if (_ended) return;
            _ended = true;
            CancelTargeting();
            Player winner = _game.Winner;
            Player loser = winner != null ? _game.GetOpponent(winner) : null;
            // Let the final blow and the K.O. play out before the result appears.
            GameOverFx(winner, loser, 0.5f);
            After(loser != null ? 2.8f : 0, () =>
            {
                var dim = ShowOverlay(0.75f);
                bool iWon = !_hotSeat && winner != null && winner.Index == _localIndex;
                string title = _hotSeat ? $"{winner?.Name.ToUpperInvariant()} WINS" : iWon ? "VICTORY" : "DEFEAT";
                var box = Dialog(dim, title, $"{loser?.Name} has been defeated after {_game.TurnNumber} turns.", "MATCH OVER");
                Theme.Cta(box, "BACK TO MENU", 220, 296, 360, 76, LeaveMatch, 30);
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
