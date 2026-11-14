using System;
using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>The board (WinForms FRM_Game + UIManager + DetailedCardControl). All rules live in <see cref="Game"/>.</summary>
    public sealed class MatchScreen : MonoBehaviour
    {
        const float SpacingHand = 20, SpacingField = 60, SpacingEssence = 20;

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

            var background = UI.FullPanel(transform, new Color(0, 0, 0, 0), "ClickCatcher");
            UI.Clicks(background.gameObject, e => OnBackgroundClicked());
            _board = UI.Stretch(UI.Rect("Board", transform));
            _cards = UI.Stretch(UI.Rect("Cards", transform));
            _detail = UI.Stretch(UI.Rect("Detail", transform));
            _drag = UI.Stretch(UI.Rect("Drag", transform));
            _overlay = UI.Stretch(UI.Rect("Overlay", transform));

            _game.Notification += OnNotification;
            _game.Start();
            _net?.StartReceiving();

            _viewer = _hotSeat ? _game.ActiveIndex : _localIndex;
            if (_hotSeat) ShowHandoff();
            Refresh();
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

            if (kb != null && kb.spaceKey.wasPressedThisFrame && !typing && _overlay.childCount == 0 && CanAct)
                Submit(GameCommand.EndTurn(_game.ActiveIndex));

            if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame))
            {
                if (Targeting) { CancelTargeting(); Refresh(); }
                else CloseDetail();
            }
        }

        void PumpNetwork()
        {
            if (_net == null || _ended) return;
            while (_net.TryReceive(out string line))
            {
                if (line == Protocol.Quit)
                {
                    EndMatch($"{_game.Players[1 - _localIndex].Name} left the game.");
                    return;
                }
                if (!line.StartsWith(Protocol.CommandPrefix, StringComparison.Ordinal))
                    continue; // ignore anything unknown (e.g. relay chatter)
                if (!GameCommand.TryParse(line.Substring(Protocol.CommandPrefix.Length), out GameCommand cmd) || cmd.PlayerIndex == _localIndex)
                {
                    Desync($"Invalid message from the other player: {line}");
                    return;
                }
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
            }
            if (_net.IsClosed && !_net.HasPending && !_ended)
                EndMatch(_game.IsOver ? "The other player disconnected." : "Connection lost: the other player disconnected.");
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
            return true;
        }

        void EndTurnClicked()
        {
            if (!CanAct) return;
            Submit(GameCommand.EndTurn(_game.ActiveIndex));
        }

        // ───────────────────────────── Board ─────────────────────────────

        static Rect ZoneRect(BoardZone zone, bool bottom)
        {
            switch (zone)
            {
                case BoardZone.Deck: return bottom ? new Rect(20, 760, 170, 220) : new Rect(20, 20, 170, 220);
                case BoardZone.Hand: return bottom ? new Rect(210, 760, 1500, 220) : new Rect(210, 20, 1500, 220);
                case BoardZone.DiscardPile: return bottom ? new Rect(1730, 760, 170, 220) : new Rect(1730, 20, 170, 220);
                case BoardZone.PlayingField: return bottom ? new Rect(210, 520, 1200, 220) : new Rect(210, 260, 1200, 220);
                case BoardZone.EssenceField: return bottom ? new Rect(1430, 520, 450, 220) : new Rect(1430, 260, 450, 220);
                case BoardZone.Stage: return new Rect(20, 400, 170, 220);
                default: throw new ArgumentOutOfRangeException(nameof(zone));
            }
        }

        static string ZoneLabel(BoardZone zone, bool bottom)
        {
            switch (zone)
            {
                case BoardZone.Deck: return bottom ? "Your Deck" : "Enemy Deck";
                case BoardZone.Hand: return bottom ? "Your Cards" : "Enemy Cards";
                case BoardZone.DiscardPile: return bottom ? "Discard Pile" : "Enemy Discard Pile";
                case BoardZone.PlayingField: return bottom ? "Your Field" : "Enemy Field";
                case BoardZone.EssenceField: return bottom ? "Your Essence" : "Enemy Essence";
                default: return "Stage";
            }
        }

        void Refresh()
        {
            UI.Clear(_board);
            UI.Clear(_cards);
            _zones.Clear();
            _legendViews.Clear();

            foreach (bool bottom in new[] { false, true })
            {
                Player p = bottom ? Bottom : Top;
                DrawZone(BoardZone.Deck, p, bottom, $" ({p.Deck.Count})");
                DrawZone(BoardZone.Hand, p, bottom, $" ({p.Hand.Count})");
                DrawZone(BoardZone.DiscardPile, p, bottom, $" ({p.DiscardPile.Count})");
                DrawZone(BoardZone.PlayingField, p, bottom, "");
                DrawZone(BoardZone.EssenceField, p, bottom, "");
                DrawPlayerInfo(p, bottom);

                DrawDeck(p, bottom);
                DrawDiscard(p, bottom);
                DrawHand(p, bottom);
                DrawField(p, bottom);
                DrawEssence(p, bottom);
            }
            DrawZone(BoardZone.Stage, null, true, "");
            DrawStage();
            DrawControls();
        }

        void DrawZone(BoardZone zone, Player p, bool bottom, string suffix)
        {
            Rect r = ZoneRect(zone, bottom);
            var border = UI.Panel(_board, Color.black, r.x, r.y, r.width, r.height, zone.ToString());
            border.raycastTarget = false;
            var inner = UI.Panel(border.transform, UI.ZoneColor, 2, 2, r.width - 4, r.height - 4, "Inner");
            inner.raycastTarget = false;
            UI.Label(border.transform, ZoneLabel(zone, bottom) + suffix, 13, Color.black, 5, 4, r.width - 10, 18);
            _zones[(zone, p?.Index ?? -1)] = border.rectTransform;
        }

        void DrawPlayerInfo(Player p, bool bottom)
        {
            Rect r = bottom ? new Rect(20, 655, 170, 85) : new Rect(20, 260, 170, 85);
            var border = UI.Panel(_board, Color.black, r.x, r.y, r.width, r.height, "PlayerInfo");
            border.raycastTarget = false;
            var inner = UI.Panel(border.transform, p == _game.ActivePlayer ? UI.ActiveGreen : UI.ZoneColor, 2, 2, r.width - 4, r.height - 4, "Inner");
            inner.raycastTarget = false;
            string health = p.MaxHealth != Player.StartingHealth ? $"{p.Health}/{p.MaxHealth}" : p.Health.ToString();
            UI.Label(border.transform, $"{p.Name}\nHealth: {health}\nEssence: {p.Essence}", 15, Color.black, 6, 4, r.width - 12, r.height - 8, TextAnchor.UpperLeft, FontStyle.Bold, bestFit: true);
        }

        CardView AddCard(Card card, bool faceUp, float x, float y, Player owner, BoardZone zone, int index, bool tapped = false, float scale = 1f, Transform parent = null)
        {
            CardView v = CardView.Create(parent ?? _cards, card, faceUp, x, y, scale, tapped);
            v.PlayerIndex = owner?.Index ?? -1;
            v.Zone = zone;
            v.Index = index;
            v.Clicked = OnCardClicked;
            v.DragLayer = _drag;
            return v;
        }

        void DrawDeck(Player p, bool bottom)
        {
            if (p.Deck.Count == 0) return;
            Rect r = ZoneRect(BoardZone.Deck, bottom);
            AddCard(p.Deck[p.Deck.Count - 1], false, r.x + 10, r.y + 20, p, BoardZone.Deck, p.Deck.Count - 1);
        }

        void DrawDiscard(Player p, bool bottom)
        {
            if (p.DiscardPile.Count == 0) return;
            Rect r = ZoneRect(BoardZone.DiscardPile, bottom);
            int top = p.DiscardPile.Count - 1;
            AddCard(p.DiscardPile[top], true, r.x + 10, r.y + 20, p, BoardZone.DiscardPile, top);
        }

        static (float start, float spacing) Arrange(Rect zone, int n, float spacing)
        {
            if (n > 1 && CardView.W * n + spacing * (n - 1) > zone.width)
                spacing = (zone.width - CardView.W * n - 20) / (n - 1);
            float start = zone.x + (zone.width - CardView.W * n - spacing * (n - 1)) / 2;
            return (start, spacing);
        }

        void DrawHand(Player p, bool bottom)
        {
            Rect r = ZoneRect(BoardZone.Hand, bottom);
            var (start, spacing) = Arrange(r, p.Hand.Count, SpacingHand);
            for (int i = 0; i < p.Hand.Count; i++)
            {
                CardView v = AddCard(p.Hand[i], bottom, start + i * (CardView.W + spacing), r.y + 10, p, BoardZone.Hand, i);
                if (bottom)
                {
                    v.CanDrag = view => CanAct && view.PlayerIndex == _game.ActiveIndex;
                    v.Dropped = HandleDrop;
                }
            }
        }

        void DrawField(Player p, bool bottom)
        {
            Rect r = ZoneRect(BoardZone.PlayingField, bottom);
            var (start, spacing) = Arrange(r, p.PlayingField.Count, SpacingField);
            for (int i = 0; i < p.PlayingField.Count; i++)
            {
                LegendCard legend = p.PlayingField[i];
                float x = start + i * (CardView.W + spacing);
                float y = r.y + 10;
                // Stacked weapon / battle cards peek out above the legend, first card closest to it.
                for (int k = legend.StackedCards.Count - 1; k >= 0; k--)
                    AddCard(legend.StackedCards[k], true, x, y - 20 * (k + 1), p, BoardZone.Stacked, k);

                CardView view = AddCard(legend, true, x, y, p, BoardZone.PlayingField, i, tapped: legend.IsTapped);
                _legendViews.Add(view);

                if (Targeting && IsValidAttackTarget(p, legend))
                    view.SetHighlight(true);
                if (legend == _targetAttacker)
                    view.SetHighlight(true);
            }
        }

        void DrawEssence(Player p, bool bottom)
        {
            Rect r = ZoneRect(BoardZone.EssenceField, bottom);
            var (start, spacing) = Arrange(r, p.EssenceField.Count, SpacingEssence);
            for (int i = 0; i < p.EssenceField.Count; i++)
                AddCard(p.EssenceField[i], true, start + i * (CardView.W + spacing), r.y + 10, p, BoardZone.EssenceField, i);
        }

        void DrawStage()
        {
            if (_game.ActiveStage == null) return;
            Rect r = ZoneRect(BoardZone.Stage, true);
            AddCard(_game.ActiveStage, true, r.x + 10, r.y + 20, _game.ActiveStageOwner, BoardZone.Stage, 0);
        }

        void DrawControls()
        {
            string text;
            if (_game.IsOver) text = "Game over";
            else if (CanAct) text = $"END TURN  (Space)   -   Turn {_game.TurnNumber}";
            else if (_handoffPending) text = "...";
            else text = $"{_game.ActivePlayer.Name}'s turn   -   Turn {_game.TurnNumber}";
            var endTurn = UI.Button(_board, text, 1430, 485, 330, 30, EndTurnClicked, CanAct ? UI.ButtonGreen : UI.ButtonGray, 16);
            endTurn.interactable = CanAct;
            UI.Button(_board, "Leave", 1770, 485, 110, 30, LeaveClicked, UI.ButtonGray, 16);

            if (Targeting)
            {
                var banner = UI.Panel(_board, new Color(0, 0, 0, 0.75f), 210, 484, 1200, 32, "TargetBanner");
                banner.raycastTarget = false;
                Attack attack = _targetAttacker.Attacks[_targetAttackIndex];
                string side = _targetOwnSide ? "one of your legends" : "an enemy legend";
                UI.Label(banner.transform, $"{attack.Name}: click {side} to target it   (right-click / Esc to cancel)", 16, UI.Highlight, 0, 0, 1200, 32, TextAnchor.MiddleCenter);
            }
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
        }

        // ───────────────────────────── Drag & drop (WinForms TryToSnapCard) ─────────────────────────────

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
            if (!CanAct || view.Zone != BoardZone.Hand) return;
            int me = view.PlayerIndex;
            int enemy = 1 - me;
            Player player = _game.Players[me];
            if (view.Index < 0 || view.Index >= player.Hand.Count || player.Hand[view.Index] != view.Card) return;
            Card card = view.Card;
            int hand = view.Index;

            switch (card)
            {
                case EssenceCard _:
                    if (Over(BoardZone.EssenceField, me, screen)) Submit(GameCommand.PlayEssence(me, hand));
                    break;
                case StageCard _:
                    if (Over(BoardZone.Stage, -1, screen)) Submit(GameCommand.PlayStage(me, hand));
                    break;
                case LegendCard _:
                    if (Over(BoardZone.PlayingField, me, screen)) Submit(GameCommand.PlayLegend(me, hand));
                    break;
                case WeaponCard _:
                    if (Over(BoardZone.PlayingField, me, screen))
                    {
                        int legend = LegendUnder(me, screen);
                        if (legend >= 0) Submit(GameCommand.PlayWeapon(me, hand, legend));
                    }
                    break;
                case BattleCard battle:
                    if (Over(BoardZone.PlayingField, me, screen) || Over(BoardZone.PlayingField, enemy, screen))
                    {
                        int target = -1;
                        if (battle.TargetRequired)
                        {
                            target = LegendUnder(battle.FriendlyFire ? me : enemy, screen);
                            if (target < 0)
                            {
                                if (LegendUnder(battle.FriendlyFire ? enemy : me, screen) >= 0)
                                    _app.Toast("You cannot play this BattleCard on this Card");
                                break;
                            }
                        }
                        Submit(GameCommand.PlayBattle(me, hand, target));
                    }
                    break;
            }
        }

        // ───────────────────────────── Detail view (DetailedCardControl) ─────────────────────────────

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
                if (p.PlayingField.Contains(c)) return !((LegendCard)c).IsTapped;
                if (p.PlayingField.Any(l => l.StackedCards.Contains(c))) return true;
                if (p.EssenceField.Contains(c) || p.DiscardPile.Contains(c)) return true;
            }
            return Bottom.Hand.Contains(c);
        }

        void OpenDetail(Card card)
        {
            UI.Clear(_detail);
            _detailCard = card;
            const float X = 1450, Y = 20, W = 450, H = 600;

            var border = UI.Panel(_detail, Color.black, X, Y, W, H, "DetailCard");
            UI.Clicks(border.gameObject, e => CloseDetail());
            var face = UI.Panel(border.transform, CardStyle.CardColor(card.Element), 3, 3, W - 6, H - 6, "Face");
            face.raycastTarget = false;
            Transform t = border.transform;
            Color text = CardStyle.TextColor(card.Element);

            UI.Label(t, card.Name, 18, text, 8, 6, 220, 24, TextAnchor.UpperLeft, FontStyle.Bold, bestFit: true);
            UI.Label(t, card.Cost.ToString(), 18, text, 380, H - 30, 60, 24, TextAnchor.UpperRight);

            if (card is LegendCard legend)
            {
                UI.Label(t, $"Att {legend.Power}", 18, text, 175, 6, 100, 24, TextAnchor.UpperCenter);
                UI.Label(t, $"HP {legend.CurrentHP}/{legend.BaseHealth}", 18, text, 290, 6, 152, 24, TextAnchor.UpperRight);
                UI.Picture(t, Art.Get(card.Art), 10, 34, 430, 286, cover: true);

                float y = 330;
                for (int i = 0; i < legend.Attacks.Count; i++)
                {
                    Attack attack = legend.Attacks[i];
                    int index = i;
                    var button = UI.Button(t, AttackButtonText(legend, attack), 10, y, 430, 48, () => OnAttackClicked(legend, index), UI.ButtonGray, 14);
                    button.interactable = CanControlAttacks(legend) && attack.MeetsWeaponRequirements(legend);
                    y += 52;
                }
                string status = "";
                if (legend.IsBurned) status += "Burned (loses 1 HP at the start of its owner's turn)\n";
                if (!legend.CanAttack) status += "Cannot attack right now\n";
                if (legend.AttackedThisTurn && legend.Owner == _game.ActivePlayer) status += "Already attacked this turn\n";
                UI.Label(t, legend.EffectDescription + status, 15, text, 10, y + 4, 430, H - y - 36, TextAnchor.UpperLeft, FontStyle.Bold, bestFit: true);

                // Stacked cards at 1.5x along the bottom right, as in the WinForms version.
                float sx = App.Width - CardView.W * 1.5f - 20;
                float sy = App.Height - CardView.H * 1.5f - 20;
                foreach (Card stacked in legend.StackedCards)
                {
                    CardView v = CardView.Create(_detail, stacked, true, sx, sy, 1.5f);
                    v.Clicked = _ => CloseDetail();
                    sx -= CardView.W * 1.5f / 2 + 20;
                }
            }
            else if (card is WeaponCard)
            {
                UI.Picture(t, Art.Get(card.Art), 40, 40, 370, 380, cover: false);
                UI.Label(t, card.Description, 18, text, 12, 440, 426, 120, TextAnchor.UpperLeft, FontStyle.Bold, bestFit: true);
            }
            else
            {
                UI.Picture(t, Art.Get(card.Art), 10, 34, 430, 360, cover: false);
                UI.Label(t, card.Description, 18, text, 12, 404, 426, H - 404 - 36, TextAnchor.UpperLeft, FontStyle.Bold, bestFit: true);
            }
        }

        static string AttackButtonText(LegendCard legend, Attack attack)
        {
            string damage;
            if (attack.OneHitKO)
            {
                damage = "KO";
            }
            else
            {
                int baseDamage = Attack.CalculateBaseDamage(legend, attack);
                int bonus = Attack.CalculateElementalBonus(legend, attack);
                damage = bonus > 0 ? $"{baseDamage} Damage + {bonus} Elemental Bonus" : $"{baseDamage} Damage";
            }
            return $"{attack.Name} ({damage})\n{attack.WeaponDescription}";
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
                return;
            }
            Submit(GameCommand.Attack(legend.Owner.Index, legend.Owner.PlayingField.IndexOf(legend), attackIndex, -1));
        }

        // ───────────────────────────── Overlays ─────────────────────────────

        RectTransform ShowOverlay(float opacity)
        {
            UI.Clear(_overlay);
            var dim = UI.FullPanel(_overlay, new Color(0, 0, 0, opacity), "Dim");
            return dim.rectTransform;
        }

        void CloseOverlay() => UI.Clear(_overlay);

        Transform Dialog(RectTransform dim, string title, string message)
        {
            var box = UI.Panel(dim, new Color32(40, 40, 60, 255), 560, 340, 800, 400, "Dialog");
            UI.Label(box.transform, title, 40, Color.white, 20, 30, 760, 60, TextAnchor.MiddleCenter);
            UI.Label(box.transform, message, 22, Color.white, 40, 110, 720, 160, TextAnchor.MiddleCenter, FontStyle.Normal, bestFit: true);
            return box.transform;
        }

        void ShowHandoff()
        {
            _handoffPending = true;
            CancelTargeting();
            CloseDetail();
            var dim = ShowOverlay(1f);
            Player next = _game.Players[_viewer];
            var box = Dialog(dim, $"{next.Name}'s turn", "Pass the device to " + next.Name + ".\nThe other player should look away.");
            UI.Button(box, "Start my turn", 250, 300, 300, 60, () =>
            {
                _handoffPending = false;
                CloseOverlay();
                Refresh();
            }, UI.ButtonGreen, 22);
        }

        void ShowGameOver()
        {
            if (_ended) return;
            _ended = true;
            CancelTargeting();
            Player winner = _game.Winner;
            Player loser = winner != null ? _game.GetOpponent(winner) : null;
            var dim = ShowOverlay(0.7f);
            bool iWon = !_hotSeat && winner != null && winner.Index == _localIndex;
            string title = _hotSeat ? $"{winner?.Name} wins!" : iWon ? "Victory!" : "Defeat";
            var box = Dialog(dim, title, $"{loser?.Name} has been defeated.\n{winner?.Name} wins the match.");
            UI.Button(box, "Back to Menu", 250, 300, 300, 60, LeaveMatch, UI.ButtonGray, 22);
            Refresh();
        }

        void EndMatch(string reason)
        {
            if (_ended) return;
            _ended = true;
            CancelTargeting();
            var dim = ShowOverlay(0.7f);
            var box = Dialog(dim, "Match ended", reason);
            UI.Button(box, "Back to Menu", 250, 300, 300, 60, LeaveMatch, UI.ButtonGray, 22);
            Refresh();
        }

        void Desync(string reason)
        {
            _net?.Send(Protocol.Quit);
            EndMatch("The game went out of sync and was stopped.\n" + reason);
        }

        void LeaveClicked()
        {
            if (_ended || _game.IsOver)
            {
                LeaveMatch();
                return;
            }
            var dim = ShowOverlay(0.6f);
            var box = Dialog(dim, "Leave the match?", _hotSeat ? "The current game will be lost." : "Your opponent will be told that you left.");
            UI.Button(box, "Leave", 120, 300, 260, 60, LeaveMatch, new Color32(220, 120, 120, 255), 22);
            UI.Button(box, "Stay", 420, 300, 260, 60, CloseOverlay, UI.ButtonGray, 22);
        }

        void LeaveMatch()
        {
            _net?.Send(Protocol.Quit);
            _net?.Dispose();
            _net = null;
            _app.ShowMenu();
        }
    }
}
