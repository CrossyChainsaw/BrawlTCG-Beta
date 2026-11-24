using System;
using System.Collections.Generic;
using System.Globalization;

namespace BrawlTCG.Core
{
    public enum CommandType
    {
        EndTurn,
        PlayEssence,
        PlayStage,
        PlayLegend,
        PlayWeapon,
        PlayBattle,
        Attack,
        /// <summary>Pay the card's Essence cost to put it from your hand onto your discard pile (no effects).</summary>
        DiscardCard,
        /// <summary>
        /// Before the first turn: shuffle the chosen starting-hand cards back into your deck and draw as many new ones.
        /// <see cref="GameCommand.TargetIndex"/> holds the chosen hand indices as a bitmask (0 = keep the hand).
        /// </summary>
        RedrawStartingHand,
    }

    /// <summary>
    /// A single player action. Every state change during a match goes through one of these, so two clients
    /// that apply the same command sequence (with the same seed and decks) stay identical (lockstep).
    /// </summary>
    public sealed class GameCommand
    {
        public CommandType Type { get; }
        public int PlayerIndex { get; }
        public int HandIndex { get; }
        /// <summary>Field index of the legend (weapon target / attacker).</summary>
        public int LegendIndex { get; }
        public int AttackIndex { get; }
        /// <summary>Field index of the target legend; -1 for none. The side is implied by the card / attack.</summary>
        public int TargetIndex { get; }
        /// <summary>
        /// Attack: the attacker's stacked cards to burn, as a bitmask of <see cref="LegendCard.StackedCards"/> indices.
        /// 0 = the default (the first matching weapons). See <see cref="Game.IsValidBurnMask"/>.
        /// </summary>
        public int BurnMask { get; }

        GameCommand(CommandType type, int playerIndex, int handIndex = -1, int legendIndex = -1, int attackIndex = -1, int targetIndex = -1,
            int burnMask = 0)
        {
            Type = type;
            PlayerIndex = playerIndex;
            HandIndex = handIndex;
            LegendIndex = legendIndex;
            AttackIndex = attackIndex;
            TargetIndex = targetIndex;
            BurnMask = burnMask;
        }

        public static GameCommand EndTurn(int player) => new GameCommand(CommandType.EndTurn, player);
        public static GameCommand PlayEssence(int player, int hand) => new GameCommand(CommandType.PlayEssence, player, hand);
        public static GameCommand PlayStage(int player, int hand) => new GameCommand(CommandType.PlayStage, player, hand);
        public static GameCommand PlayLegend(int player, int hand) => new GameCommand(CommandType.PlayLegend, player, hand);
        public static GameCommand PlayWeapon(int player, int hand, int legend) => new GameCommand(CommandType.PlayWeapon, player, hand, legendIndex: legend);
        public static GameCommand PlayBattle(int player, int hand, int target) => new GameCommand(CommandType.PlayBattle, player, hand, targetIndex: target);
        public static GameCommand Attack(int player, int legend, int attack, int target, int burnMask = 0) =>
            new GameCommand(CommandType.Attack, player, legendIndex: legend, attackIndex: attack, targetIndex: target, burnMask: burnMask);
        public static GameCommand DiscardCard(int player, int hand) => new GameCommand(CommandType.DiscardCard, player, hand);
        /// <param name="mask">Bit i set = shuffle hand card i back into the deck.</param>
        public static GameCommand RedrawStartingHand(int player, int mask) => new GameCommand(CommandType.RedrawStartingHand, player, targetIndex: mask);

        /// <summary>Plays the hand card at <paramref name="hand"/> with the command matching its type.</summary>
        public static GameCommand PlayCard(Card card, int player, int hand, int target)
        {
            switch (card.Type)
            {
                case CardType.Essence: return PlayEssence(player, hand);
                case CardType.Stage: return PlayStage(player, hand);
                case CardType.Legend: return PlayLegend(player, hand);
                case CardType.Weapon: return PlayWeapon(player, hand, target);
                case CardType.Battle: return PlayBattle(player, hand, target);
                default: throw new ArgumentOutOfRangeException(nameof(card));
            }
        }

        public string Serialize()
        {
            return string.Join(",", new[]
            {
                ((int)Type).ToString(CultureInfo.InvariantCulture),
                PlayerIndex.ToString(CultureInfo.InvariantCulture),
                HandIndex.ToString(CultureInfo.InvariantCulture),
                LegendIndex.ToString(CultureInfo.InvariantCulture),
                AttackIndex.ToString(CultureInfo.InvariantCulture),
                TargetIndex.ToString(CultureInfo.InvariantCulture),
                BurnMask.ToString(CultureInfo.InvariantCulture),
            });
        }

        public static bool TryParse(string text, out GameCommand command)
        {
            command = null;
            if (string.IsNullOrEmpty(text)) return false;
            string[] parts = text.Split(',');
            if (parts.Length != 6 && parts.Length != 7) return false; // 6: from before burn choices (BurnMask 0)
            var values = new int[7];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[i]))
                    return false;
            }
            if (!Enum.IsDefined(typeof(CommandType), values[0])) return false;
            command = new GameCommand((CommandType)values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
            return true;
        }

        public override string ToString() => $"{Type}(p{PlayerIndex} h{HandIndex} l{LegendIndex} a{AttackIndex} t{TargetIndex}" + (BurnMask != 0 ? $" b{BurnMask})" : ")");

        /// <summary>Every command the active player could try right now (most will be illegal). For bots and tests.</summary>
        public static IEnumerable<GameCommand> EnumerateCandidates(Game game)
        {
            if (game.ChoosingStartingHands)
            {
                // Every possible starting-hand choice of each player still choosing.
                for (int player = 0; player < 2; player++)
                    if (!game.HasChosenStartingHand(player))
                        for (int mask = 0; mask < 1 << game.Players[player].Hand.Count; mask++)
                            yield return RedrawStartingHand(player, mask);
                yield break;
            }

            int p = game.ActiveIndex;
            Player me = game.ActivePlayer;
            Player enemy = game.InactivePlayer;
            int maxTargets = Math.Max(me.PlayingField.Count, enemy.PlayingField.Count);

            for (int h = 0; h < me.Hand.Count; h++)
            {
                yield return PlayEssence(p, h);
                yield return PlayStage(p, h);
                yield return PlayLegend(p, h);
                for (int l = 0; l < me.PlayingField.Count; l++)
                    yield return PlayWeapon(p, h, l);
                for (int t = -1; t < maxTargets; t++)
                    yield return PlayBattle(p, h, t);
                yield return DiscardCard(p, h);
            }
            for (int l = 0; l < me.PlayingField.Count; l++)
            {
                int attacks = me.PlayingField[l].Attacks.Count;
                for (int a = 0; a < attacks; a++)
                    for (int t = -1; t < maxTargets; t++)
                        yield return Attack(p, l, a, t);
            }
            yield return EndTurn(p);
        }
    }
}
