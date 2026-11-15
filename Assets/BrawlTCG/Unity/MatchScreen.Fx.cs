using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BrawlTCG.Core;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Match animations. The board is still rebuilt from the game state after every command; this part remembers
    /// where every card was drawn, compares that with the new board and animates the difference (cards flying
    /// between zones, attacks, hits, K.O.s, heals, buffs). Purely visual: rules and netcode are untouched.
    /// </summary>
    public sealed partial class MatchScreen
    {
        struct Placement
        {
            public Vector2 TopLeft;
            public float Scale;
            public bool FaceUp;
            public BoardZone Zone;
            public int Player;

            public Vector2 Center => TopLeft + new Vector2(CardView.W, CardView.H) * Scale / 2;
        }

        sealed class RefEq<T> : IEqualityComparer<T> where T : class
        {
            public static readonly RefEq<T> Instance = new RefEq<T>();
            public bool Equals(T a, T b) => ReferenceEquals(a, b);
            public int GetHashCode(T o) => RuntimeHelpers.GetHashCode(o);
        }

        sealed class Snapshot
        {
            public GameCommand Cmd;
            public int Viewer;
            public Dictionary<Card, Placement> Layout;
            public readonly Dictionary<LegendCard, (int hp, int power)> Legends = new Dictionary<LegendCard, (int, int)>(RefEq<LegendCard>.Instance);
            public readonly int[] Hp = new int[2];
            public readonly int[] EssenceCards = new int[2];
            public LegendCard Attacker, Target;
            public Attack Attack;
            public AttackMode Mode;
            public Card Played;
            public LegendCard PlayedOn;
        }

        static readonly Color DamageRed = new Color32(255, 84, 96, 255);
        static readonly Color HealGreen = new Color32(110, 240, 150, 255);
        static readonly Color EssencePurple = new Color32(186, 130, 255, 255);
        static readonly Vector2 Showcase = new Vector2(960, 500);

        readonly Dictionary<Card, Placement> _layout = new Dictionary<Card, Placement>(RefEq<Card>.Instance);
        readonly Dictionary<Card, CardView> _views = new Dictionary<Card, CardView>(RefEq<Card>.Instance);
        RectTransform _fx, _top;
        Shaker _shaker;
        TargetLine _targetLine;

        // ───────────── Bookkeeping ─────────────

        void Register(CardView view, Card card, float x, float y, float scale, BoardZone zone, int index, Player owner)
        {
            if (card == null || (zone == BoardZone.Deck && index < 0)) return; // deck thickness layers
            _layout[card] = new Placement
            {
                TopLeft = new Vector2(x, y),
                Scale = scale,
                FaceUp = view.FaceUp,
                Zone = zone,
                Player = owner != null ? owner.Index : -1,
            };
            _views[card] = view;
        }

        Snapshot TakeSnapshot(GameCommand cmd)
        {
            var s = new Snapshot { Cmd = cmd, Viewer = _viewer, Layout = new Dictionary<Card, Placement>(_layout, RefEq<Card>.Instance) };
            foreach (Player p in _game.Players)
            {
                s.Hp[p.Index] = p.Health;
                s.EssenceCards[p.Index] = p.EssenceField.Count;
                foreach (LegendCard l in p.PlayingField) s.Legends[l] = (l.CurrentHP, l.Power);
            }
            if (cmd.PlayerIndex < 0 || cmd.PlayerIndex > 1) return s;
            Player me = _game.Players[cmd.PlayerIndex];
            Player enemy = _game.GetOpponent(me);
            if (cmd.Type == CommandType.Attack)
            {
                if (cmd.LegendIndex < 0 || cmd.LegendIndex >= me.PlayingField.Count) return s;
                s.Attacker = me.PlayingField[cmd.LegendIndex];
                if (cmd.AttackIndex < 0 || cmd.AttackIndex >= s.Attacker.Attacks.Count) return s;
                s.Attack = s.Attacker.Attacks[cmd.AttackIndex];
                s.Mode = _game.GetAttackMode(s.Attacker, s.Attack);
                Player side = s.Mode == AttackMode.OwnLegend ? me : s.Mode == AttackMode.EnemyLegend ? enemy : null;
                if (side != null && cmd.TargetIndex >= 0 && cmd.TargetIndex < side.PlayingField.Count) s.Target = side.PlayingField[cmd.TargetIndex];
            }
            else if (cmd.Type != CommandType.EndTurn && cmd.HandIndex >= 0 && cmd.HandIndex < me.Hand.Count)
            {
                s.Played = me.Hand[cmd.HandIndex];
                if (cmd.Type == CommandType.PlayWeapon && cmd.LegendIndex >= 0 && cmd.LegendIndex < me.PlayingField.Count)
                    s.PlayedOn = me.PlayingField[cmd.LegendIndex];
                if (cmd.Type == CommandType.PlayBattle && s.Played is BattleCard battle && cmd.TargetIndex >= 0)
                {
                    Player side = battle.FriendlyFire ? me : enemy;
                    if (cmd.TargetIndex < side.PlayingField.Count) s.PlayedOn = side.PlayingField[cmd.TargetIndex];
                }
            }
            return s;
        }

        // ───────────── Board geometry ─────────────

        static Vector2 PileCenter(Vector2 topLeft) => topLeft + new Vector2(CardView.W, CardView.H) * PileScale / 2;
        Vector2 DeckCenter(int player) => PileCenter(player == _viewer ? BottomDeck : TopDeck);
        Vector2 DiscardCenter(int player) => PileCenter(player == _viewer ? BottomDiscard : TopDiscard);
        Rect PanelRect(int player) => player == _viewer ? BottomPanel : TopPanel;
        Vector2 AvatarCenter(int player) { Rect r = PanelRect(player); return new Vector2(r.x + 47, r.y + 47); }
        Vector2 HandCenter(int player) => player == _viewer ? new Vector2(960, BottomHand.y + 28 + CardView.H / 2) : new Vector2(960, -64 + CardView.H * 0.35f);

        Vector2 GemCenter(int player, int gem)
        {
            Rect r = PanelRect(player);
            return new Vector2(r.x + 27 + Mathf.Clamp(gem, 0, 11) * 24, r.y + 159);
        }

        Vector2 CenterOf(Snapshot s, Card card)
        {
            if (s.Layout.TryGetValue(card, out Placement p)) return p.Center;
            if (_layout.TryGetValue(card, out p)) return p.Center;
            return Showcase;
        }

        (BoardZone zone, int player) Locate(Card c)
        {
            if (_game.ActiveStage == c) return (BoardZone.Stage, -1);
            foreach (Player p in _game.Players)
            {
                if (p.Hand.Contains(c)) return (BoardZone.Hand, p.Index);
                if (p.DiscardPile.Contains(c)) return (BoardZone.DiscardPile, p.Index);
                if (p.EssenceField.Contains(c)) return (BoardZone.EssenceField, p.Index);
                if (p.Deck.Contains(c)) return (BoardZone.Deck, p.Index);
                foreach (LegendCard l in p.PlayingField)
                {
                    if (l == c) return (BoardZone.PlayingField, p.Index);
                    if (l.StackedCards.Contains(c)) return (BoardZone.Stacked, p.Index);
                }
            }
            return (BoardZone.Preview, -1); // left the game
        }

        // ───────────── Timing helpers ─────────────

        IEnumerator AfterRoutine(float seconds, Action action)
        {
            if (seconds > 0) yield return new WaitForSecondsRealtime(seconds);
            if (this != null) action();
        }

        void After(float seconds, Action action)
        {
            if (seconds <= 0) action();
            else StartCoroutine(AfterRoutine(seconds, action));
        }

        /// <summary>Attack sound of the attack's weapon (or the legend's main weapon for "any weapon" attacks).</summary>
        static string WeaponCue(LegendCard attacker, Attack attack)
        {
            Weapon w = attack.WeaponOne != Weapon.Any ? attack.WeaponOne : attacker.PrimaryWeapon;
            return "hit_" + w.ToString().ToLowerInvariant();
        }

        /// <summary>Plays a sound cue after a delay (in sync with the matching visual).</summary>
        void Sound(string cue, float delay = 0, float volume = 1) => After(delay, () => Sfx.Play(cue, volume));

        void Shake(float amount, float delay = 0)
        {
            if (_shaker == null) return;
            After(delay, () => { if (_shaker != null) _shaker.Add(amount * 0.5f); });
        }

        // ───────────── The diff ─────────────

        void Animate(Snapshot s)
        {
            if (s == null || _fx == null) return;
            if (s.Viewer != _viewer || _handoffPending) return; // the board flipped; the hand-off intro takes over
            int actor = s.Cmd.PlayerIndex;
            float impact = 0;
            Element? source = null;

            // ── Attacks: wind-up, lunge, element hit at the target ──
            if (s.Attacker != null && s.Attack != null)
            {
                Element el = s.Attacker.Element;
                source = el;
                Color tint = Fx.Tint(el);
                Vector2 from = CenterOf(s, s.Attacker);
                Fx.Float(_top, s.Attack.Name.ToUpperInvariant(), from + new Vector2(0, -128), tint, 30, 0, 36);
                bool aimed = s.Target != null || s.Mode == AttackMode.Player;
                if (aimed)
                {
                    Vector2 to = s.Target != null ? CenterOf(s, s.Target) : AvatarCenter(1 - actor);
                    impact = 0.28f;
                    if (_views.TryGetValue(s.Attacker, out CardView av) && av != null)
                        av.gameObject.AddComponent<Lunge>().Init(to - from, impact);
                    Fx.Glow(_fx, from, new Color(tint.r, tint.g, tint.b, 0.8f), 340, 0, 0.35f);
                    if (el == Element.Magic || el == Element.Cosmic)
                        Fx.Bolt(_fx, from, to, tint, impact - 0.1f, 0.42f, 8);
                    if (el == Element.Magic || el == Element.Cosmic) Sound("bolt", impact - 0.1f);
                    bool mirror = UnityEngine.Random.value > 0.5f;
                    Fx.Play(_fx, "Slash", to, 250, impact - 0.03f, 36, null, UnityEngine.Random.Range(-35f, 35f), mirror);
                    Fx.Play(_fx, Fx.HitClip(el), to, 320, impact, 30);
                    Sound(WeaponCue(s.Attacker, s.Attack), impact);
                    Fx.Glow(_fx, to, new Color(1, 1, 1, 0.9f), 260, impact, 0.25f);
                    Fx.Sparks(_fx, to, tint, 20, 620, impact);
                    Shake(0.38f, impact);
                }
                else
                {
                    // Instant attacks (crafting, drawing, summoning): a burst of power on the attacker.
                    impact = 0.18f;
                    Sound("power");
                    Fx.Play(_fx, "Sparkle", from, 300, 0, 30, tint);
                    Fx.Shockwave(_fx, from, tint, 360, 0.05f);
                    Fx.Sparks(_fx, from, tint, 16, 420, 0.05f);
                }
            }
            else if (s.Played != null)
            {
                source = s.Played.Element;
                impact = s.Cmd.Type == CommandType.PlayBattle ? 0.9f : 0.42f;
            }
            else if (s.Cmd.Type == CommandType.EndTurn)
            {
                impact = 0.55f;
                if (!_game.IsOver) TurnIntro(false);
            }

            float leaveDelay = impact + 0.6f; // cards that leave the field wait for their K.O.
            int drawn = 0;
            var leavers = new List<(Card card, Placement before, Vector2 dest, float scale, CardView real)>();

            // ── Cards still on the board: fly from where they were ──
            foreach (KeyValuePair<Card, Placement> kv in _layout)
            {
                Card card = kv.Key;
                Placement after = kv.Value;
                if (!_views.TryGetValue(card, out CardView view) || view == null) continue;
                bool known = s.Layout.TryGetValue(card, out Placement before);

                if (card == s.Played)
                {
                    FlyPlayed(s, known ? before : (Placement?)null, after.Center, after.Scale, view.FaceUp, view);
                    continue;
                }
                if (known)
                {
                    if (Same(before, after)) continue;
                    if (IsLeaving(before.Zone, after.Zone))
                    {
                        leavers.Add((card, before, after.Center, after.Scale, view));
                        continue;
                    }
                    var f = CardFlight.Begin(view, before.Center, before.Scale)
                        .To(after.Center, after.Scale, Duration(before.Center, after.Center), 0, before.Zone == after.Zone ? 0 : 50);
                    if (before.FaceUp != after.FaceUp) f.Flip();
                }
                else if (after.Zone == BoardZone.Hand)
                {
                    // Drawn or generated into a hand: dealt from that player's deck.
                    var f = CardFlight.Begin(view, DeckCenter(after.Player), PileScale, impact + drawn++ * 0.09f)
                        .To(after.Center, after.Scale, 0.45f, 0, 70);
                    Sound("card_draw", impact + (drawn - 1) * 0.09f, 0.7f);
                    if (after.FaceUp) f.Flip();
                }
                else if (after.Zone == BoardZone.PlayingField || after.Zone == BoardZone.Stacked || after.Zone == BoardZone.Stage)
                {
                    // Summoned out of nowhere (generated legends, stages, weapons).
                    CardFlight.Begin(view, after.Center, after.Scale * 0.2f, impact).To(after.Center, after.Scale, 0.32f);
                    Fx.Play(_fx, "Sparkle", after.Center, 280, impact, 30, Fx.Tint(card.Element));
                    Fx.Shockwave(_fx, after.Center, Fx.Tint(card.Element), 360, impact);
                }
            }

            // ── Cards that are no longer drawn (essence, discard under the top, removed): ghosts ──
            foreach (KeyValuePair<Card, Placement> kv in s.Layout)
            {
                Card card = kv.Key;
                Placement before = kv.Value;
                if (_layout.ContainsKey(card) || before.Zone == BoardZone.Deck) continue;
                (BoardZone zone, int owner) = Locate(card);
                Vector2 dest = before.Center;
                float destScale = before.Scale * 0.6f;
                bool faceUp = before.FaceUp;
                switch (zone)
                {
                    case BoardZone.DiscardPile: dest = DiscardCenter(owner); destScale = PileScale; faceUp = true; break;
                    case BoardZone.EssenceField: dest = GemCenter(owner, _game.Players[owner].EssenceField.Count - 1); destScale = 0.25f; faceUp = true; break;
                    case BoardZone.Deck: dest = DeckCenter(owner); destScale = PileScale; break;
                    case BoardZone.Hand: dest = HandCenter(owner); destScale = owner == _viewer ? 1 : 0.7f; faceUp = owner == _viewer && !_handoffPending; break;
                    case BoardZone.Stacked:
                        foreach (LegendCard l in _game.Players[owner].PlayingField)
                            if (l.StackedCards.Contains(card) && _layout.TryGetValue(l, out Placement lp)) { dest = lp.Center + new Vector2(0, -40); destScale = 0.5f; }
                        faceUp = true;
                        break;
                }
                if (card == s.Played)
                {
                    FlyPlayed(s, before, dest, destScale, faceUp, null);
                    continue;
                }
                if (IsLeaving(before.Zone, zone))
                {
                    leavers.Add((card, before, dest, destScale, null));
                    continue;
                }
                var ghost = Ghost(card, faceUp, before.Scale);
                var f = CardFlight.Begin(ghost, before.Center, before.Scale).To(dest, destScale, 0.45f, 0, 50).FadeOutAndDestroy();
                if (faceUp != before.FaceUp) f.Flip();
            }

            // ── Cards leaving the field: they take the hit, wait for the K.O., then go to their pile.
            // Stacked cards first so the legend stays on top of its weapons. ──
            leavers.Sort((a, b) =>
            {
                int za = a.before.Zone == BoardZone.PlayingField ? 1 : 0, zb = b.before.Zone == BoardZone.PlayingField ? 1 : 0;
                return za != zb ? za.CompareTo(zb) : a.before.TopLeft.y.CompareTo(b.before.TopLeft.y);
            });
            foreach (var lv in leavers)
            {
                var ghost = Ghost(lv.card, lv.before.FaceUp, lv.before.Scale);
                CardHit.Apply(ghost, DamageRed, 1, impact);
                var f = CardFlight.Begin(ghost, lv.before.Center, lv.before.Scale, leaveDelay).NoRaise().To(lv.dest, lv.scale, 0.45f, 0, 50);
                Reveal(f, lv.real);
            }

            // ── Legends: damage, healing, buffs, K.O.s ──
            foreach (KeyValuePair<LegendCard, (int hp, int power)> kv in s.Legends)
            {
                LegendCard l = kv.Key;
                bool alive = l.OnPlayingField;
                Vector2 c = alive && _layout.TryGetValue(l, out Placement now) ? now.Center : CenterOf(s, l);
                CardView view = alive && _views.TryGetValue(l, out CardView v) ? v : null;
                bool mainHit = s.Attacker != null && l == s.Target;
                string hitClip = source != null ? Fx.HitClip(source.Value) : (l.IsBurned ? "HitFire" : "HitBig");

                if (!alive)
                {
                    int dealt = Mathf.Max(1, kv.Value.hp - Mathf.Min(0, l.CurrentHP));
                    Element koEl = source ?? l.Element;
                    Color koTint = Fx.Tint(koEl);
                    if (!mainHit) Fx.Play(_fx, hitClip, c, 300, impact);
                    Fx.Float(_top, "-" + dealt, c + new Vector2(0, -20), DamageRed, 64, impact + 0.04f);
                    float ko = impact + 0.18f;
                    Fx.Play(_fx, Fx.KoClip(koEl), c + new Vector2(0, -70), 560, ko, 30);
                    Sound("ko", ko);
                    Fx.Flash(_top, new Color(koTint.r, koTint.g, koTint.b, 0.1f), 0.4f, ko);
                    Fx.Shockwave(_fx, c, koTint, 620, ko, 0.6f);
                    Fx.Sparks(_fx, c, koTint, 42, 900, ko);
                    Fx.Float(_top, "K.O.", c + new Vector2(0, 40), Theme.Gold, 96, ko + 0.12f, 70);
                    Shake(0.75f, ko);
                    continue;
                }

                int dHp = l.CurrentHP - kv.Value.hp;
                int dPow = l.Power - kv.Value.power;
                if (dHp < 0)
                {
                    if (!mainHit) { Fx.Play(_fx, hitClip, c, 280, impact); Shake(0.22f, impact); }
                    if (!mainHit) Sound(s.Attack != null ? WeaponCue(s.Attacker, s.Attack) : "hit", impact, 0.8f);
                    CardHit.Apply(view, DamageRed, 1, impact);
                    Fx.Float(_top, dHp.ToString(), c + new Vector2(0, -10), DamageRed, 62, impact + 0.04f);
                }
                else if (dHp > 0)
                {
                    CardHit.Apply(view, HealGreen, 0.25f, impact);
                    Fx.Play(_fx, "Sparkle", c, 280, impact, 30, HealGreen);
                    Sound("heal", impact);
                    Fx.Glow(_fx, c, new Color(HealGreen.r, HealGreen.g, HealGreen.b, 0.7f), 320, impact, 0.6f);
                    Fx.Float(_top, "+" + dHp, c + new Vector2(0, -10), HealGreen, 62, impact + 0.04f);
                }
                if (dPow != 0)
                {
                    Color pc = dPow > 0 ? Theme.Gold : new Color32(200, 160, 255, 255);
                    Fx.Float(_top, (dPow > 0 ? "+" : "") + dPow + " ATK", c + new Vector2(0, 56), pc, 38, impact + 0.3f, 70);
                    if (dPow > 0) Fx.Glow(_fx, c, new Color(pc.r, pc.g, pc.b, 0.6f), 300, impact + 0.3f, 0.5f);
                    Sound(dPow > 0 ? "buff" : "debuff", impact + 0.3f);
                }
            }

            // ── Players ──
            for (int i = 0; i < 2; i++)
            {
                Player p = _game.Players[i];
                int d = p.Health - s.Hp[i];
                Vector2 avatar = AvatarCenter(i);
                Rect r = PanelRect(i);
                Vector2 numberAt = new Vector2(r.x + 200, r.y + 84);
                if (d < 0)
                {
                    Fx.Play(_fx, "Explosion", avatar, 340, impact, 30);
                    Sound("player_hit", impact);
                    Fx.Play(_fx, "Clash", new Vector2(r.center.x, r.y + 90), 300, impact + 0.05f, 30);
                    Fx.Float(_top, d.ToString(), numberAt, DamageRed, 80, impact + 0.05f, 70);
                    Fx.Flash(_top, new Color(0.9f, 0.1f, 0.15f, 0.09f), 0.4f, impact);
                    Shake(0.6f, impact);
                }
                else if (d > 0)
                {
                    Fx.Play(_fx, "Sparkle", avatar, 260, impact, 30, HealGreen);
                    Sound("heal", impact);
                    Fx.Float(_top, "+" + d, numberAt, HealGreen, 72, impact + 0.05f, 70);
                }
            }
        }

        static bool IsLeaving(BoardZone from, BoardZone to) =>
            (from == BoardZone.PlayingField || from == BoardZone.Stacked) && to != BoardZone.PlayingField && to != BoardZone.Stacked;

        /// <summary>A non-interactive copy of a card on the effects layer.</summary>
        CardView Ghost(Card card, bool faceUp, float scale)
        {
            var ghost = CardView.Create(_fx, card, faceUp, 0, 0, scale);
            ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;
            return ghost;
        }

        /// <summary>Hides the real card while its ghost travels; shows it when the ghost lands (or fades the ghost if there is none).</summary>
        static void Reveal(CardFlight flight, CardView real)
        {
            if (real == null)
            {
                flight.FadeOutAndDestroy();
                return;
            }
            var group = real.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 0;
            flight.DestroyAtEnd();
            Action landed = flight.Landed;
            flight.Landed = () =>
            {
                landed?.Invoke();
                if (group != null) group.alpha = 1;
            };
        }

        static bool Same(Placement a, Placement b) =>
            (a.TopLeft - b.TopLeft).sqrMagnitude < 1 && Mathf.Abs(a.Scale - b.Scale) < 0.01f && a.FaceUp == b.FaceUp;

        static float Duration(Vector2 a, Vector2 b) => Mathf.Clamp(0.22f + Vector2.Distance(a, b) / 3200f, 0.22f, 0.5f);

        /// <summary>The card that was just played: battle cards are shown off mid-board first, the rest fly straight in and land with a bang.</summary>
        void FlyPlayed(Snapshot s, Placement? before, Vector2 dest, float destScale, bool faceUp, CardView real)
        {
            Card card = s.Played;
            Vector2 start = before.HasValue ? before.Value.Center : HandCenter(s.Cmd.PlayerIndex);
            float startScale = before.HasValue ? before.Value.Scale : 1;
            bool startFaceUp = before.HasValue && before.Value.FaceUp;
            Color tint = Fx.Tint(card.Element);
            int player = s.Cmd.PlayerIndex;

            if (card is BattleCard battle)
            {
                // Effects first so the card is drawn on top of its glow.
                Fx.Glow(_fx, Showcase, new Color(tint.r, tint.g, tint.b, 0.85f), 640, 0.3f, 0.6f);
                Fx.Shockwave(_fx, Showcase, tint, 540, 0.32f);
                var bg = Ghost(card, faceUp, startScale);
                Sound("card_play");
                Sound("battle_card", 0.3f);
                CardFlight bf = CardFlight.Begin(bg, start, startScale).To(Showcase, 1.6f, 0.32f, 0.5f, 40).To(dest, destScale, 0.32f);
                if (!startFaceUp && faceUp) bf.Flip();
                Reveal(bf, real);
                Fx.Play(_fx, "Sparkle", Showcase, 440, 0.3f, 30, tint);
                const float impact = 0.9f;
                bool friendly = s.PlayedOn != null && s.PlayedOn.Owner == _game.Players[player];
                if (s.PlayedOn != null)
                {
                    Vector2 target = CenterOf(s, s.PlayedOn);
                    Fx.Bolt(_fx, Showcase, target, tint, impact - 0.12f, 0.34f, 7);
                    if (friendly) Fx.Play(_fx, "Sparkle", target, 300, impact, 30, tint);
                    else Fx.Play(_fx, Fx.HitClip(battle.Element), target, 300, impact, 30);
                    Sound(friendly ? "heal" : "hit", impact);
                }
                else
                {
                    Fx.Flash(_top, new Color(tint.r, tint.g, tint.b, 0.14f), 0.4f, impact);
                    Fx.Shockwave(_fx, Showcase, tint, 900, impact, 0.7f);
                }
                return;
            }

            var ghost = Ghost(card, faceUp, startScale);
            Sound("card_play");
            CardFlight f = CardFlight.Begin(ghost, start, startScale).To(dest, destScale, card is EssenceCard ? 0.45f : 0.36f, 0, 60);
            if (!startFaceUp && faceUp) f.Flip();
            f.Landed = () => Land(s, card, dest, player);
            Reveal(f, real);
        }

        void Land(Snapshot s, Card card, Vector2 at, int player)
        {
            if (_fx == null) return;
            Color tint = Fx.Tint(card.Element);
            switch (card)
            {
                case LegendCard _:
                    Sfx.Play("land_legend");
                    Fx.Play(_fx, "Ring", at + new Vector2(0, 70), 420, 0, 30, tint);
                    Fx.Shockwave(_fx, at, tint, 460);
                    Fx.Glow(_fx, at, new Color(tint.r, tint.g, tint.b, 0.8f), 380, 0, 0.45f);
                    Fx.Sparks(_fx, at + new Vector2(0, 80), tint, 26, 620);
                    Fx.Float(_top, card.Name.ToUpperInvariant(), at + new Vector2(0, -132), tint, 30, 0.05f, 36);
                    Shake(0.32f);
                    break;
                case WeaponCard weapon:
                    Sfx.Play("equip");
                    Vector2 lc = s.PlayedOn != null && _layout.TryGetValue(s.PlayedOn, out Placement lp) ? lp.Center : at;
                    Fx.Play(_fx, "Slash", lc, 300, 0, 30, null, UnityEngine.Random.Range(-25f, 25f));
                    Fx.Play(_fx, "Sparkle", lc, 300, 0.05f, 30, Theme.Gold);
                    Fx.Glow(_fx, lc, new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.7f), 340, 0, 0.5f);
                    Fx.Sparks(_fx, lc, Theme.Gold, 18, 480);
                    Fx.Float(_top, weapon.Weapon.ToString().ToUpperInvariant() + " EQUIPPED", lc + new Vector2(0, -132), Theme.Gold, 28, 0.05f, 36);
                    Shake(0.15f);
                    break;
                case StageCard _:
                    Sfx.Play("stage");
                    Fx.Flash(_top, new Color(tint.r, tint.g, tint.b, 0.18f), 0.6f);
                    Fx.Shockwave(_fx, at, tint, 900, 0, 0.8f);
                    Fx.Play(_fx, "Ring", at, 460, 0, 30, tint);
                    Fx.Sparks(_fx, at, tint, 36, 760);
                    Fx.Banner(_top, card.Name.ToUpperInvariant(), "THE BATTLEFIELD CHANGES", tint, 0.1f);
                    Shake(0.45f);
                    break;
                case EssenceCard _:
                    Sfx.Play("essence");
                    Fx.Play(_fx, "Sparkle", at, 200, 0, 30, EssencePurple);
                    Fx.Glow(_fx, at, new Color(EssencePurple.r, EssencePurple.g, EssencePurple.b, 0.9f), 160, 0, 0.5f);
                    Fx.Sparks(_fx, at, EssencePurple, 14, 300, 0, 300);
                    break;
            }
        }

        // ───────────── Turn intro, targeting line, game over ─────────────

        /// <summary>Turn banner; with <paramref name="deal"/> the visible hand is dealt from the deck.</summary>
        void TurnIntro(bool deal)
        {
            if (_fx == null || _game.IsOver) return;
            Player active = _game.ActivePlayer;
            bool mine = IsLocallyControlled(active.Index);
            string title = _hotSeat ? $"{active.Name.ToUpperInvariant()}'S TURN" : mine ? "YOUR TURN" : $"{active.Name.ToUpperInvariant()}'S TURN";
            Fx.Banner(_top, title, $"TURN {_game.TurnNumber}", mine ? Theme.Gold : new Color32(140, 170, 255, 255));
            Sfx.Play("turn", mine ? 0.8f : 0.5f);
            if (!deal) return;
            int i = 0;
            foreach (Card c in Bottom.Hand)
            {
                if (!_views.TryGetValue(c, out CardView view) || view == null || !_layout.TryGetValue(c, out Placement p)) continue;
                CardFlight.Begin(view, DeckCenter(_viewer), PileScale, 0.35f + i++ * 0.07f).To(p.Center, p.Scale, 0.42f, 0, 70).Flip();
                Sound("card_draw", 0.35f + (i - 1) * 0.07f, 0.6f);
            }
        }

        void StartTargetLine()
        {
            StopTargetLine();
            if (_fx == null || _targetAttacker == null) return;
            var rt = UI.Stretch(UI.Rect("TargetLine", _fx));
            _targetLine = rt.gameObject.AddComponent<TargetLine>();
            LegendCard attacker = _targetAttacker;
            Color tint = _targetOwnSide ? HealGreen : Theme.Gold;
            _targetLine.Init(_fx, () => _layout.TryGetValue(attacker, out Placement p) ? p.Center : Showcase, tint);
            Sfx.Play("aim", 0.6f);
        }

        void StopTargetLine()
        {
            if (_targetLine != null) Destroy(_targetLine.gameObject);
            _targetLine = null;
        }

        void GameOverFx(Player winner, Player loser, float delay)
        {
            if (_fx == null || loser == null) return;
            Vector2 at = AvatarCenter(loser.Index);
            Color white = new Color(1, 1, 1, 0.3f);
            Fx.Play(_fx, "KoMagic", at + new Vector2(0, -40), 520, delay, 30);
            Sound("ko", delay + 0.1f, 0.8f);
            Fx.Play(_fx, "Explosion", at, 560, delay + 0.12f, 30);
            Fx.Flash(_top, white, 0.6f, delay + 0.12f);
            Fx.Shockwave(_fx, at, Theme.Gold, 1100, delay + 0.12f, 0.9f);
            Fx.Sparks(_fx, at, Theme.Gold, 60, 1100, delay + 0.12f);
            Shake(1f, delay + 0.12f);
            if (winner == null) return;
            Color wc = Theme.Gold;
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector2(UnityEngine.Random.Range(500f, 1420f), UnityEngine.Random.Range(220f, 720f));
                Fx.Sparks(_fx, p, i % 2 == 0 ? wc : HealGreen, 34, 700, delay + 0.6f + i * 0.18f, 500);
                Fx.Play(_fx, "Sparkle", p, 260, delay + 0.6f + i * 0.18f, 30, i % 2 == 0 ? wc : Color.white);
            }
            Fx.Banner(_top, "K.O.!", $"{winner.Name.ToUpperInvariant()} WINS", Theme.Gold, delay + 0.5f);
        }
    }
}
