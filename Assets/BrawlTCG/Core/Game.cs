using System;
using System.Collections.Generic;
using System.Linq;

namespace BrawlTCG.Core
{
    /// <summary>
    /// The complete rules engine. Pure C#, deterministic for a given seed, decks and command sequence.
    /// All randomness must go through <see cref="Rng"/>.
    /// </summary>
    public sealed partial class Game
    {
        public const int StartingEssence = 1;
        public const int StartingHandCards = 7;
        public const int MaxCardsInHand = 15;
        /// <summary>Damage at the start of a turn with no card (full hand or empty deck): to each of the player's legends, or to the player if they have none.</summary>
        public const int NoDrawDamage = 1;
        public const int MaxLegendsOnField = 5;
        const int EssenceCardId = 0;

        readonly Player[] _players;

        public IReadOnlyList<Player> Players => _players;
        public int ActiveIndex { get; private set; }
        public Player ActivePlayer => _players[ActiveIndex];
        public Player InactivePlayer => _players[1 - ActiveIndex];
        public Random Rng { get; }

        public StageCard ActiveStage { get; private set; }
        public Player ActiveStageOwner { get; private set; }
        /// <summary>The Workshop stage turns this off ("instead of drawing a card, craft a random card").</summary>
        public bool DrawCardStartTurn { get; set; } = true;

        public bool IsStarted { get; private set; }
        public bool IsOver { get; private set; }
        public Player Winner { get; private set; }
        public int TurnNumber { get; private set; }

        /// <summary>
        /// Set before <see cref="Start"/>: after drawing their starting hands both players may shuffle any of those cards
        /// back and redraw that many (one <see cref="CommandType.RedrawStartingHand"/> each) before the first turn.
        /// Both clients of a network game must use the same setting.
        /// </summary>
        public bool StartingHandRedraw { get; set; }
        /// <summary>True between <see cref="Start"/> and both players choosing their starting hand.</summary>
        public bool ChoosingStartingHands { get; private set; }
        readonly bool[] _chosenStartingHand = new bool[2];
        // One shuffle generator per player, so the two choices give the same result in either order (they are made at
        // the same time in a network game, so each client may receive them in a different order).
        Random[] _redrawRng;

        public bool HasChosenStartingHand(int player) => _chosenStartingHand[player];

        /// <summary>Messages for the players ("X starts", "X just took damage", ...).</summary>
        public event Action<string> Notification;

        /// <param name="deck0">Card ids of the first player's deck in draw order (last id is drawn first).</param>
        public Game(string name0, IList<int> deck0, string name1, IList<int> deck1, int seed)
        {
            Rng = new Random(seed);
            _players = new[] { new Player(0, name0), new Player(1, name1) };
            FillDeck(_players[0], deck0);
            FillDeck(_players[1], deck1);
        }

        static void FillDeck(Player player, IList<int> deck)
        {
            foreach (int id in deck)
            {
                Card card = CardCatalogue.Create(id);
                card.Owner = player;
                player.Deck.Add(card);
            }
        }

        public Player GetOpponent(Player p) => p == _players[0] ? _players[1] : _players[0];

        /// <summary>All legends on the field, active player's first.</summary>
        public List<LegendCard> GetAllLegendsOnPlayingField()
        {
            return ActivePlayer.PlayingField.Concat(InactivePlayer.PlayingField).ToList();
        }

        public void Notify(string message) => Notification?.Invoke(message);

        // ───────────────────────────── Setup & turns ─────────────────────────────

        public void Start()
        {
            if (IsStarted) throw new InvalidOperationException("Game already started.");
            IsStarted = true;
            ActiveIndex = 0;

            foreach (Player p in new[] { ActivePlayer, InactivePlayer })
            {
                for (int i = 0; i < StartingHandCards; i++)
                    p.DrawCardFromDeck();
            }
            foreach (Player p in new[] { ActivePlayer, InactivePlayer })
            {
                for (int i = 0; i < StartingEssence; i++)
                {
                    Card essence = CardCatalogue.Create(EssenceCardId);
                    essence.Owner = p;
                    p.EssenceField.Add(essence);
                }
            }

            Notify($"{ActivePlayer.Name} Starts");
            if (StartingHandRedraw)
            {
                ChoosingStartingHands = true;
                _redrawRng = new[] { new Random(Rng.Next()), new Random(Rng.Next()) };
                return; // the first turn starts once both players have chosen
            }
            StartTurn();
        }

        /// <summary>
        /// Each chosen slot gets a newly drawn card in the same position, so the rest of the hand doesn't move. Only then
        /// do the replaced cards go back into the deck and the deck is shuffled, so a replaced card can't be drawn
        /// straight back. If the deck runs out, the remaining chosen cards stay in the hand.
        /// </summary>
        void RedrawStartingHand(Player player, int mask)
        {
            var replaced = new List<Card>();
            for (int i = 0; i < player.Hand.Count; i++)
            {
                if ((mask & (1 << i)) == 0 || player.Deck.Count == 0) continue;
                Card drawn = player.Deck[player.Deck.Count - 1]; // top of the deck is the last element
                player.Deck.RemoveAt(player.Deck.Count - 1);
                replaced.Add(player.Hand[i]);
                player.Hand[i] = drawn;
            }
            if (replaced.Count > 0)
            {
                player.Deck.AddRange(replaced);
                Random rng = _redrawRng[player.Index];
                for (int i = player.Deck.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (player.Deck[i], player.Deck[j]) = (player.Deck[j], player.Deck[i]);
                }
            }
            _chosenStartingHand[player.Index] = true;
            if (_chosenStartingHand[0] && _chosenStartingHand[1])
            {
                ChoosingStartingHands = false;
                StartTurn();
            }
        }

        void StartTurn()
        {
            TurnNumber++;
            Player p = ActivePlayer;
            int handBefore = p.Hand.Count;

            if (p.Hand.Count < MaxCardsInHand && DrawCardStartTurn)
                DrawCardFromDeck(p);

            if (ActiveStage?.StartTurnEffect != null)
                ActiveStage.StartTurnEffect.Invoke(p.PlayingField.ToList(), ActiveStage, this, ActiveStage);

            // No card this turn (full hand or empty deck): each of the player's legends loses 1 HP, or, with no legends,
            // the player does. A card from the stage (The Workshop's craft, Space Time's extra draw) counts as getting a card.
            if (p.Hand.Count <= handBefore)
            {
                string reason = p.Deck.Count == 0 ? $"{p.Name} has no cards left to draw" : $"{p.Name}'s hand is full, so no card is drawn";
                if (p.PlayingField.Count > 0)
                {
                    foreach (LegendCard legend in p.PlayingField.ToList())
                        legend.LoseHealth(NoDrawDamage);
                    Notify($"{reason}: their legends lose {NoDrawDamage} HP.");
                }
                else
                {
                    p.LoseHealth(NoDrawDamage);
                    Notify($"{reason} and takes {NoDrawDamage} damage.");
                }
            }

            foreach (LegendCard legend in p.PlayingField.ToList())
                legend.TakeBurnDamage();

            ResolveDeaths();
            p.Essence = p.EssenceField.Count;
            CheckGameOver();
        }

        void EndTurn()
        {
            Player ending = ActivePlayer;
            foreach (LegendCard legend in ending.PlayingField)
            {
                legend.IsStunned = false;
                legend.AttackedThisTurn = false;
                legend.CanAttack = true;
            }

            ActiveIndex = 1 - ActiveIndex;
            ActivePlayer.PlayedEssenceCardThisTurn = false;
            StartTurn();
        }

        // ───────────────────────────── Commands ─────────────────────────────

        /// <summary>Returns null when the command is legal, otherwise the reason it is not.</summary>
        public string Validate(GameCommand cmd)
        {
            if (cmd == null) return "No command.";
            if (!IsStarted) return "The game has not started.";
            if (IsOver) return "The game is over.";

            // Starting hands: both players choose at the same time, before anyone has a turn.
            if (ChoosingStartingHands)
            {
                if (cmd.Type != CommandType.RedrawStartingHand) return "Choose your starting hand first.";
                if (cmd.PlayerIndex < 0 || cmd.PlayerIndex > 1) return "Invalid player.";
                if (_chosenStartingHand[cmd.PlayerIndex]) return "You already chose your starting hand.";
                int cards = _players[cmd.PlayerIndex].Hand.Count;
                return cmd.TargetIndex >= 0 && cmd.TargetIndex < 1 << cards ? null : "Invalid starting hand choice.";
            }
            if (cmd.Type == CommandType.RedrawStartingHand) return "The starting hands were already chosen.";

            if (cmd.PlayerIndex != ActiveIndex) return "It is not your turn!";

            Player me = ActivePlayer;
            Player enemy = InactivePlayer;

            switch (cmd.Type)
            {
                case CommandType.EndTurn:
                    return null;

                case CommandType.Attack:
                {
                    if (!InRange(cmd.LegendIndex, me.PlayingField.Count)) return "Invalid legend.";
                    LegendCard legend = me.PlayingField[cmd.LegendIndex];
                    if (!InRange(cmd.AttackIndex, legend.Attacks.Count)) return "Invalid attack.";
                    Attack attack = legend.Attacks[cmd.AttackIndex];
                    string error = CanUseAttack(legend, attack);
                    if (error != null) return error;
                    switch (GetAttackMode(legend, attack))
                    {
                        case AttackMode.OwnLegend:
                            return IsTargetableLegend(me, cmd.TargetIndex) ? null : "Choose one of your legends.";
                        case AttackMode.EnemyLegend:
                            return IsTargetableLegend(enemy, cmd.TargetIndex) ? null : "Choose an enemy legend.";
                        default:
                            return null;
                    }
                }
            }

            // Card plays
            if (!InRange(cmd.HandIndex, me.Hand.Count)) return "Invalid card.";
            Card card = me.Hand[cmd.HandIndex];

            switch (cmd.Type)
            {
                case CommandType.PlayEssence:
                    if (!(card is EssenceCard)) return "That is not an Essence card.";
                    if (me.PlayedEssenceCardThisTurn) return "You already played Essence this turn";
                    return null;

                case CommandType.PlayStage:
                    if (!(card is StageCard)) return "That is not a Stage card.";
                    return me.Essence >= card.Cost ? null : "Not enough Essence";

                case CommandType.PlayLegend:
                    if (!(card is LegendCard)) return "That is not a Legend card.";
                    if (me.Essence < card.Cost) return "Not enough Essence";
                    if (me.PlayingField.Count >= MaxLegendsOnField) return $"Already {MaxLegendsOnField} Legends";
                    return null;

                case CommandType.PlayWeapon:
                {
                    if (!(card is WeaponCard weapon)) return "That is not a Weapon card.";
                    if (me.Essence < card.Cost) return "Not enough Essence";
                    if (!InRange(cmd.LegendIndex, me.PlayingField.Count)) return "Play the weapon on one of your legends.";
                    LegendCard legend = me.PlayingField[cmd.LegendIndex];
                    return legend.HasWeapon(weapon.Weapon) ? null : $"You cannot play a {weapon.Weapon} on this legend";
                }

                case CommandType.PlayBattle:
                {
                    if (!(card is BattleCard battle)) return "That is not a Battle card.";
                    if (me.Essence < card.Cost) return "Not enough Essence";
                    if (!battle.TargetRequired) return null;
                    Player side = battle.FriendlyFire ? me : enemy;
                    if (!InRange(cmd.TargetIndex, side.PlayingField.Count))
                        return battle.FriendlyFire ? "Play this on one of your legends." : "Play this on an enemy legend.";
                    return null;
                }

                case CommandType.DiscardCard:
                    return me.Essence >= card.Cost ? null : $"Discarding costs {card.Cost} Essence";
            }
            return "Unknown command.";
        }

        /// <summary>Validates and applies the command. Returns null on success, otherwise the error.</summary>
        public string TryApply(GameCommand cmd)
        {
            string error = Validate(cmd);
            if (error != null) return error;
            Execute(cmd);
            return null;
        }

        void Execute(GameCommand cmd)
        {
            if (cmd.Type == CommandType.RedrawStartingHand)
            {
                RedrawStartingHand(_players[cmd.PlayerIndex], cmd.TargetIndex);
                return;
            }
            Player me = ActivePlayer;
            Player enemy = InactivePlayer;
            switch (cmd.Type)
            {
                case CommandType.EndTurn:
                    EndTurn();
                    return;
                case CommandType.Attack:
                {
                    LegendCard legend = me.PlayingField[cmd.LegendIndex];
                    PerformAttack(legend, legend.Attacks[cmd.AttackIndex], cmd.TargetIndex);
                    return;
                }
            }

            Card card = me.Hand[cmd.HandIndex];
            switch (cmd.Type)
            {
                case CommandType.PlayEssence:
                    PlayEssenceCard(me, (EssenceCard)card);
                    break;
                case CommandType.PlayStage:
                    PlayStageCard(me, (StageCard)card);
                    break;
                case CommandType.PlayLegend:
                    PlayLegendCard(me, (LegendCard)card);
                    break;
                case CommandType.PlayWeapon:
                    PlayWeaponCard(me, me.PlayingField[cmd.LegendIndex], (WeaponCard)card);
                    break;
                case CommandType.PlayBattle:
                {
                    var battle = (BattleCard)card;
                    LegendCard target = null;
                    if (battle.TargetRequired)
                        target = (battle.FriendlyFire ? me : enemy).PlayingField[cmd.TargetIndex];
                    PlayBattleCard(me, battle, target);
                    break;
                }
                case CommandType.DiscardCard:
                    // Pay the cost, straight to the discard pile; no card effects trigger.
                    PayAndRemoveFromHand(me, card);
                    Discard(me, card);
                    break;
            }
        }

        static bool InRange(int index, int count) => index >= 0 && index < count;

        /// <summary>Any legend on that side, stunned or not: being stunned only switches off its own attacks and abilities.</summary>
        static bool IsTargetableLegend(Player side, int index) => InRange(index, side.PlayingField.Count);

        // ───────────────────────────── Playing cards ─────────────────────────────

        void PayAndRemoveFromHand(Player player, Card card)
        {
            player.Hand.Remove(card);
            player.Essence -= card.Cost;
        }

        void PlayEssenceCard(Player player, EssenceCard card)
        {
            player.EssenceField.Add(card);
            player.PlayedEssenceCardThisTurn = true;
            PayAndRemoveFromHand(player, card);
            PostPlayEffects(player, card);
        }

        void PlayStageCard(Player player, StageCard stage)
        {
            if (ActiveStage != null)
            {
                StageCard old = ActiveStage;
                Discard(ActiveStageOwner, old);
                old.WhenDiscardedEffect?.Invoke(GetAllCardsInPlayingFields(), old, this, old);
            }

            PayAndRemoveFromHand(player, stage);
            ActiveStage = stage;
            ActiveStageOwner = player;
            stage.WhenPlayedEffect?.Invoke(GetAllCardsInPlayingFields(), stage, this, stage);
            PostPlayEffects(player, stage);
        }

        void PlayLegendCard(Player player, LegendCard legend)
        {
            player.PlayingField.Add(legend);
            legend.OnPlayingField = true;
            PayAndRemoveFromHand(player, legend);

            // The active stage's while-in-play effect applies to the newcomer.
            ActiveStage?.WhileInPlayEffect?.Invoke(legend, ActiveStage, this, ActiveStage);
            // Its own when-played effect. If that changes the stage, the new stage's when-played effect already
            // covered this legend (it is on the field), so the stage is not applied a second time.
            legend.WhenPlayedEffect?.Invoke(null, legend, this, legend);

            PostPlayEffects(player, legend);
        }

        void PlayWeaponCard(Player player, LegendCard legend, WeaponCard weapon)
        {
            legend.StackedCards.Add(weapon);
            PayAndRemoveFromHand(player, weapon);
            PostPlayEffects(player, weapon);
        }

        void PlayBattleCard(Player player, BattleCard battle, LegendCard target)
        {
            if (battle.TargetRequired)
            {
                battle.WhenPlayedEffect?.Invoke(target, battle, this, battle);
                PayAndRemoveFromHand(player, battle);
                if (battle.OneTimeUse)
                    Discard(player, battle);
                else
                    target.StackedCards.Add(battle);
            }
            else
            {
                battle.WhenPlayedEffect?.Invoke(null, battle, this, battle);
                PayAndRemoveFromHand(player, battle);
                Discard(player, battle);
            }
            PostPlayEffects(player, battle);
        }

        /// <summary>
        /// Triggers every legend's while-in-play effect for the card just played, then resolves deaths.
        /// Stunned legends' abilities are turned off.
        /// </summary>
        void PostPlayEffects(Player player, Card playedCard)
        {
            ResolveDeaths();
            foreach (LegendCard legend in GetAllLegendsOnPlayingField())
            {
                if (legend.WhileInPlayEffect != null && legend.OnPlayingField && !legend.IsStunned)
                    legend.WhileInPlayEffect.Invoke(null, legend, this, playedCard);
            }
            ResolveDeaths();
            CheckGameOver();
        }

        List<Card> GetAllCardsInPlayingFields()
        {
            return _players[0].PlayingField.Cast<Card>().Concat(_players[1].PlayingField).ToList();
        }

        // ───────────────────────────── Attacks ─────────────────────────────

        /// <summary>Returns null when the legend can use the attack now (ignoring targets), else why not.</summary>
        public string CanUseAttack(LegendCard legend, Attack attack)
        {
            if (IsOver) return "The game is over.";
            if (legend.Owner != ActivePlayer) return "It is not your turn!";
            if (!legend.OnPlayingField) return "This legend is not on the playing field.";
            if (legend.IsStunned) return "This legend is stunned.";
            if (legend.AttackedThisTurn) return "This legend already attacked this turn";
            // Silent Galaxy turns CanAttack off for non-Cosmic legends: they can't deal damage, but can still craft, set, summon...
            if (!legend.CanAttack && attack.Kind == AttackKind.Strike) return "Silent Galaxy: only Cosmic legends can deal damage";
            if (!attack.MeetsWeaponRequirements(legend)) return "This legend does not have the required weapons.";
            string pre = attack.Precondition?.Invoke(this, legend);
            if (pre != null) return pre;
            if (GetAttackMode(legend, attack) == AttackMode.None)
            {
                return attack.OneHitKO || CalculateDamageSafe(legend, attack) <= 0
                    ? "You cannot attack the player with this attack"
                    : "There is nothing to attack.";
            }
            return null;
        }

        static int CalculateDamageSafe(LegendCard legend, Attack attack) => Attack.CalculateDamage(legend, attack);

        public AttackMode GetAttackMode(LegendCard legend, Attack attack)
        {
            Player enemy = GetOpponent(legend.Owner);
            if (attack.InstaEffect) return AttackMode.Insta;
            if (enemy.PlayingField.Count == 0 && !attack.FriendlyFire)
                return attack.CanHitPlayer(legend) ? AttackMode.Player : AttackMode.None;
            if (attack.FriendlyFire) return AttackMode.OwnLegend;
            if (attack.MultiHit) return AttackMode.AllEnemies;
            return AttackMode.EnemyLegend;
        }

        void PerformAttack(LegendCard legend, Attack attack, int targetIndex)
        {
            Player me = legend.Owner;
            Player enemy = GetOpponent(me);

            switch (GetAttackMode(legend, attack))
            {
                case AttackMode.Insta:
                    attack.Execute(legend, null, this);
                    break;
                case AttackMode.Player:
                    attack.Execute(legend, enemy, this);
                    Notify($"{enemy.Name} just took damage");
                    break;
                case AttackMode.OwnLegend:
                    attack.Execute(legend, me.PlayingField[targetIndex], this);
                    break;
                case AttackMode.EnemyLegend:
                    attack.Execute(legend, enemy.PlayingField[targetIndex], this);
                    break;
                case AttackMode.AllEnemies:
                    foreach (LegendCard target in enemy.PlayingField.ToList())
                        attack.Execute(legend, target, this);
                    break;
            }

            // Weapons are burned once per attack, after the damage has been dealt.
            BurnWeapons(legend, attack.WeaponOne, attack.WeaponOneBurnAmount);
            BurnWeapons(legend, attack.WeaponTwo, attack.WeaponTwoBurnAmount);
            legend.AttackedThisTurn = true;

            ResolveDeaths();
            CheckGameOver();
        }

        void BurnWeapons(LegendCard legend, Weapon? weapon, int amount)
        {
            foreach (WeaponCard burned in legend.BurnWeapon(weapon, amount))
                Discard(legend.Owner, burned);
        }

        // ───────────────────────────── Helpers used by effects ─────────────────────────────

        public void Discard(Player player, Card card)
        {
            card.IsDiscarded = true;
            card.Owner = player;
            player.DiscardPile.Add(card);
        }

        public void DrawCardFromDeck(Player player) => player.DrawCardFromDeck();

        /// <param name="startTurn">Draws triggered at the start of a turn respect the hand limit.</param>
        public void DrawCards(Player player, int n, bool startTurn = false)
        {
            if (startTurn && player.Hand.Count >= MaxCardsInHand) return;
            for (int i = 0; i < n; i++)
                DrawCardFromDeck(player);
        }

        public void AddCardToHand(Player player, Card card)
        {
            card.Owner = player;
            card.IsDiscarded = false;
            player.Hand.Add(card);
        }

        /// <summary>Marks a card made during the match with what made it (the effect or attack running now).</summary>
        static T Crafted<T>(T card) where T : Card
        {
            card.CraftedBy = EffectContext.Current ?? "Unknown";
            return card;
        }

        /// <summary>Gives the active player a fresh copy of the card.</summary>
        public void GenerateCard(int cardId) => AddCardToHand(ActivePlayer, Crafted(CardCatalogue.Create(cardId)));

        public void GenerateRandomCards(int n, Element? element = null, CardType? type = null, bool noChests = false)
        {
            for (int i = 0; i < n; i++)
                AddCardToHand(ActivePlayer, Crafted(CardCatalogue.CreateRandom(Rng, element, type, noChests)));
        }

        public void GenerateRandomWeaponCards(int n, Weapon? weapon = null)
        {
            for (int i = 0; i < n; i++)
            {
                Card card = weapon == null ? CardCatalogue.CreateRandomWeapon(Rng) : CardCatalogue.CreateRandomWeapon(Rng, weapon.Value);
                AddCardToHand(ActivePlayer, Crafted(card));
            }
        }

        public void GenerateRandomLegends(int n, int maxCost)
        {
            for (int i = 0; i < n; i++)
                AddCardToHand(ActivePlayer, Crafted(CardCatalogue.CreateRandomLegend(Rng, maxCost)));
        }

        public void GenerateRandomLegendsWithWeapon(int n, Weapon weapon)
        {
            for (int i = 0; i < n; i++)
                AddCardToHand(ActivePlayer, Crafted(CardCatalogue.CreateRandomLegendWithWeapon(Rng, weapon)));
        }

        /// <summary>Gives the active player the stage plus the essence it costs, and plays it.</summary>
        public void GenerateAndPlayStage(int cardId)
        {
            var stage = Crafted((StageCard)CardCatalogue.Create(cardId));
            ActivePlayer.GainEssence(stage.Cost);
            AddCardToHand(ActivePlayer, stage);
            PlayStageCard(ActivePlayer, stage);
        }

        /// <summary>Gives the active player the legend plus the essence it costs, and plays it.</summary>
        public void GenerateAndPlayLegend(int cardId)
        {
            var legend = Crafted((LegendCard)CardCatalogue.Create(cardId));
            if (ActivePlayer.PlayingField.Count >= MaxLegendsOnField) return;
            ActivePlayer.GainEssence(legend.Cost);
            AddCardToHand(ActivePlayer, legend);
            PlayLegendCard(ActivePlayer, legend);
        }

        // ───────────────────────────── Death & victory ─────────────────────────────

        /// <summary>
        /// Removes dead legends. A "when eliminated" effect can kill more legends, so this repeats until nobody else
        /// died. It always ends: every pass removes at least one legend from the field.
        /// </summary>
        void ResolveDeaths()
        {
            bool anyDied;
            do
            {
                anyDied = false;
                foreach (Player p in _players)
                {
                    foreach (LegendCard legend in p.PlayingField.ToList())
                    {
                        if (legend.CurrentHP <= 0 && legend.OnPlayingField)
                        {
                            KillLegend(p, legend);
                            anyDied = true;
                        }
                    }
                }
            } while (anyDied);
        }

        void KillLegend(Player owner, LegendCard legend)
        {
            legend.CurrentHP = 0;
            foreach (Card stacked in legend.StackedCards)
                Discard(owner, stacked);
            legend.StackedCards.Clear();
            legend.OnPlayingField = false;
            owner.PlayingField.Remove(legend);
            Discard(owner, legend);
            // "When Eliminated" (e.g. Final Form Petra). It runs after the legend has left the field, so it can't hit itself.
            legend.WhenDiscardedEffect?.Invoke(null, legend, this, legend);
        }

        void CheckGameOver()
        {
            if (IsOver) return;
            foreach (Player p in _players)
            {
                if (p.Health <= 0)
                {
                    IsOver = true;
                    Winner = GetOpponent(p);
                    Notify($"{p.Name} has been defeated");
                    return;
                }
            }
        }

        /// <summary>Compact text snapshot of the full state, used to verify lockstep clients stay in sync.</summary>
        public string StateFingerprint()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"T{TurnNumber} A{ActiveIndex} S{ActiveStage?.Id ?? -1}/{ActiveStageOwner?.Index ?? -1} D{DrawCardStartTurn} O{IsOver}{Winner?.Index} R{ChoosingStartingHands}{_chosenStartingHand[0]}{_chosenStartingHand[1]};");
            foreach (Player p in _players)
            {
                sb.Append($"P{p.Index}:{p.Health}/{p.MaxHealth} e{p.Essence} pe{p.PlayedEssenceCardThisTurn} d{string.Join(".", p.Deck.Select(c => c.Id))} ");
                sb.Append($"h{string.Join(".", p.Hand.Select(c => c.Id))} x{string.Join(".", p.DiscardPile.Select(c => c.Id))} es{p.EssenceField.Count} f[");
                foreach (LegendCard l in p.PlayingField)
                    sb.Append($"{l.Id}:{l.CurrentHP}/{l.BaseHealth}:{l.Power}:{l.IsStunned}{l.IsBurned}{l.CanAttack}{l.AttackedThisTurn}{l.ReefSavedHP}:{string.Join(".", l.StackedCards.Select(c => c.Id))} ");
                sb.Append("];");
            }
            return sb.ToString();
        }
    }
}
