using System;
using System.Collections.Generic;
using System.Linq;

namespace BrawlTCG.Core
{
    /// <summary>
    /// The complete rules engine. Pure C#, deterministic for a given seed, decks and command sequence.
    /// All randomness must go through <see cref="Rng"/>.
    /// </summary>
    public sealed class Game
    {
        public const int StartingEssence = 1;
        public const int StartingHandCards = 7;
        public const int MaxCardsInHand = 15;
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
        /// <summary>The Workshop stage turns this off ("instead of drawing a card you obtain a random card").</summary>
        public bool DrawCardStartTurn { get; set; } = true;

        public bool IsStarted { get; private set; }
        public bool IsOver { get; private set; }
        public Player Winner { get; private set; }
        public int TurnNumber { get; private set; }

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
            StartTurn();
        }

        void StartTurn()
        {
            TurnNumber++;
            Player p = ActivePlayer;

            if (p.Hand.Count < MaxCardsInHand && DrawCardStartTurn)
                DrawCardFromDeck(p);

            if (ActiveStage?.StartTurnEffect != null)
                ActiveStage.StartTurnEffect.Invoke(p.PlayingField.ToList(), ActiveStage, this, ActiveStage);

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
                legend.IsTapped = false;
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
                            return IsTargetableLegend(me, cmd.TargetIndex) ? null : "Choose one of your untapped legends.";
                        case AttackMode.EnemyLegend:
                            return IsTargetableLegend(enemy, cmd.TargetIndex) ? null : "Choose an untapped enemy legend.";
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
            }
        }

        static bool InRange(int index, int count) => index >= 0 && index < count;

        static bool IsTargetableLegend(Player side, int index) => InRange(index, side.PlayingField.Count) && !side.PlayingField[index].IsTapped;

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

        /// <summary>Triggers every legend's while-in-play effect for the card just played, then resolves deaths.</summary>
        void PostPlayEffects(Player player, Card playedCard)
        {
            ResolveDeaths();
            foreach (LegendCard legend in GetAllLegendsOnPlayingField())
            {
                if (legend.WhileInPlayEffect != null && legend.OnPlayingField)
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
            if (legend.IsTapped) return "This legend is tapped.";
            if (legend.AttackedThisTurn) return "This legend already attacked this turn";
            if (!legend.CanAttack) return "This legend cannot attack right now";
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

        /// <summary>Gives the active player a fresh copy of the card.</summary>
        public void GenerateCard(int cardId) => AddCardToHand(ActivePlayer, CardCatalogue.Create(cardId));

        public void GenerateRandomCards(int n, Element? element = null, CardType? type = null)
        {
            for (int i = 0; i < n; i++)
                AddCardToHand(ActivePlayer, CardCatalogue.CreateRandom(Rng, element, type));
        }

        public void GenerateRandomWeaponCards(int n, Weapon? weapon = null)
        {
            for (int i = 0; i < n; i++)
            {
                Card card = weapon == null ? CardCatalogue.CreateRandomWeapon(Rng) : CardCatalogue.CreateRandomWeapon(Rng, weapon.Value);
                AddCardToHand(ActivePlayer, card);
            }
        }

        public void GenerateRandomLegendsWithWeapon(int n, Weapon weapon)
        {
            for (int i = 0; i < n; i++)
                AddCardToHand(ActivePlayer, CardCatalogue.CreateRandomLegendWithWeapon(Rng, weapon));
        }

        /// <summary>Gives the active player the stage plus the essence it costs, and plays it.</summary>
        public void GenerateAndPlayStage(int cardId)
        {
            var stage = (StageCard)CardCatalogue.Create(cardId);
            ActivePlayer.GainEssence(stage.Cost);
            AddCardToHand(ActivePlayer, stage);
            PlayStageCard(ActivePlayer, stage);
        }

        /// <summary>Gives the active player the legend plus the essence it costs, and plays it.</summary>
        public void GenerateAndPlayLegend(int cardId)
        {
            var legend = (LegendCard)CardCatalogue.Create(cardId);
            if (ActivePlayer.PlayingField.Count >= MaxLegendsOnField) return;
            ActivePlayer.GainEssence(legend.Cost);
            AddCardToHand(ActivePlayer, legend);
            PlayLegendCard(ActivePlayer, legend);
        }

        // ───────────────────────────── Death & victory ─────────────────────────────

        void ResolveDeaths()
        {
            foreach (Player p in _players)
            {
                foreach (LegendCard legend in p.PlayingField.ToList())
                {
                    if (legend.CurrentHP <= 0)
                        KillLegend(p, legend);
                }
            }
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
            sb.Append($"T{TurnNumber} A{ActiveIndex} S{ActiveStage?.Id ?? -1}/{ActiveStageOwner?.Index ?? -1} D{DrawCardStartTurn} O{IsOver}{Winner?.Index};");
            foreach (Player p in _players)
            {
                sb.Append($"P{p.Index}:{p.Health}/{p.MaxHealth} e{p.Essence} pe{p.PlayedEssenceCardThisTurn} d{string.Join(".", p.Deck.Select(c => c.Id))} ");
                sb.Append($"h{string.Join(".", p.Hand.Select(c => c.Id))} x{string.Join(".", p.DiscardPile.Select(c => c.Id))} es{p.EssenceField.Count} f[");
                foreach (LegendCard l in p.PlayingField)
                    sb.Append($"{l.Id}:{l.CurrentHP}/{l.BaseHealth}:{l.Power}:{l.IsTapped}{l.IsBurned}{l.CanAttack}{l.AttackedThisTurn}:{string.Join(".", l.StackedCards.Select(c => c.Id))} ");
                sb.Append("];");
            }
            return sb.ToString();
        }
    }
}
