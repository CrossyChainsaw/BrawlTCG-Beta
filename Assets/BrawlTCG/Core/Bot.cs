using System;
using System.Collections.Generic;
using System.Linq;

namespace BrawlTCG.Core
{
    /// <summary>
    /// A computer opponent. It plays one move at a time: every legal move is tried on a search copy of the match
    /// (<see cref="Game.CloneForSearch"/>, which hides the real deck order and random results) and the position it
    /// leads to is scored. With <see cref="Lookahead"/> on, the most promising moves are also followed by the best
    /// next move, so it sees combos like weapon → attack or Essence → legend. It ends the turn when no move
    /// improves the position.
    /// </summary>
    public sealed class Bot
    {
        public int PlayerIndex { get; }
        /// <summary>Follow the best moves one step further (stronger, slower).</summary>
        public bool Lookahead { get; set; } = true;
        /// <summary>How many first moves are followed one step further when <see cref="Lookahead"/> is on.</summary>
        public int LookaheadWidth { get; set; } = 6;
        /// <summary>A turn never has more moves than this (safety net).</summary>
        public int MaxMovesPerTurn { get; set; } = 40;

        /// <summary>
        /// Before committing, play the best few moves out: finish this turn and the opponent's whole reply (both sides
        /// playing the plain greedy policy, the opponent's hand redealt from their unseen cards), and pick the move whose
        /// position holds up best. Strongest and slowest.
        /// </summary>
        public bool Rollouts { get; set; }
        public int RolloutWidth { get; set; } = 5;
        public int RolloutsPerMove { get; set; } = 3;

        public enum Level { Easy, Normal, Hard }

        /// <summary>
        /// A bot of the given strength: Easy plays the best single move, Normal also looks a move ahead, Hard also plays
        /// out the opponent's reply.
        /// </summary>
        public static Bot Create(Level level, int playerIndex, int seed)
        {
            var bot = new Bot(playerIndex, seed);
            switch (level)
            {
                case Level.Easy: bot.Lookahead = false; break;
                case Level.Normal: bot.Lookahead = true; bot.DiscardWhenStuck = true; break;
                default: bot.Lookahead = true; bot.Rollouts = true; bot.DiscardWhenStuck = true; break;
            }
            return bot;
        }

        /// <summary>
        /// When no card in hand can be played, discard one before ending the turn (Normal and Hard): a card it holds
        /// two or more of, otherwise a random one. Once per turn, never Essence.
        /// </summary>
        public bool DiscardWhenStuck { get; set; }

        readonly Random _rng;
        int _movesThisTurn, _turnSeen = -1, _discardedTurn = -1;

        public Bot(int playerIndex, int seed)
        {
            PlayerIndex = playerIndex;
            _rng = new Random(seed);
        }

        /// <summary>The bot's next command, or null when it has nothing to do right now (not its turn / already chose).</summary>
        public GameCommand Decide(Game game)
        {
            if (game.IsOver) return null;
            if (game.ChoosingStartingHands)
                return game.HasChosenStartingHand(PlayerIndex) ? null : GameCommand.RedrawStartingHand(PlayerIndex, ChooseRedraw(game.Players[PlayerIndex]));
            if (game.ActiveIndex != PlayerIndex) return null;

            if (game.TurnNumber != _turnSeen) { _turnSeen = game.TurnNumber; _movesThisTurn = 0; }
            if (++_movesThisTurn > MaxMovesPerTurn) return GameCommand.EndTurn(PlayerIndex);

            List<GameCommand> moves = LegalMoves(game);
            if (moves.Count == 0) return EndTurnOrDiscard(game, moves);

            double stay = Evaluate(game, PlayerIndex);
            var scored = new List<(GameCommand cmd, double score, Game after)>(moves.Count);
            foreach (GameCommand cmd in moves)
            {
                Game copy = game.CloneForSearch(_rng.Next());
                if (copy.TryApply(cmd) != null) continue;
                scored.Add((cmd, Evaluate(copy, PlayerIndex), copy));
            }
            if (scored.Count == 0) return EndTurnOrDiscard(game, moves);

            if (Lookahead)
            {
                foreach (int i in Enumerable.Range(0, scored.Count).OrderByDescending(i => scored[i].score).Take(LookaheadWidth).ToList())
                {
                    var (cmd, score, after) = scored[i];
                    if (after.IsOver || after.ActiveIndex != PlayerIndex) continue;
                    double best = score;
                    foreach (GameCommand next in LegalMoves(after))
                    {
                        Game copy = after.CloneForSearch(_rng.Next());
                        if (copy.TryApply(next) != null) continue;
                        best = Math.Max(best, Evaluate(copy, PlayerIndex));
                    }
                    // A move that only sets up the next one is worth a little less than doing both now.
                    scored[i] = (cmd, Math.Max(score, best - 0.05), after);
                }
            }

            var top = scored.OrderByDescending(s => s.score).First();
            if (top.score <= stay + 0.01) return EndTurnOrDiscard(game, moves);
            if (!Rollouts) return top.cmd;

            // Which of the good moves holds up best once the turn and the opponent's reply are played out? Every
            // option is played out on the same random deals, so they differ only by the move.
            var options = scored.Where(s => s.score > stay + 0.01).OrderByDescending(s => s.score).Take(RolloutWidth).ToList();
            if (options.Count == 1) return top.cmd;
            var seeds = Enumerable.Range(0, RolloutsPerMove).Select(_ => _rng.Next()).ToArray();
            GameCommand bestCmd = top.cmd;
            double bestValue = double.MinValue;
            foreach (var option in options)
            {
                double total = 0;
                foreach (int seed in seeds)
                {
                    Game copy = game.CloneForSearch(seed, hideHandOf: 1 - PlayerIndex);
                    if (copy.TryApply(option.cmd) != null) { total = double.MinValue; break; }
                    PlayOut(copy, seed);
                    total += Evaluate(copy, PlayerIndex);
                }
                if (total > bestValue)
                {
                    bestValue = total;
                    bestCmd = option.cmd;
                }
            }
            return bestCmd;
        }

        /// <summary>
        /// The bot is done for the turn. With <see cref="DiscardWhenStuck"/>, if none of its cards can be played it first
        /// discards one it can pay for: one it holds a duplicate of, otherwise a random one.
        /// </summary>
        GameCommand EndTurnOrDiscard(Game game, List<GameCommand> moves)
        {
            var endTurn = GameCommand.EndTurn(PlayerIndex);
            if (!DiscardWhenStuck || _discardedTurn == game.TurnNumber) return endTurn;
            if (moves.Any(m => m.Type != CommandType.Attack)) return endTurn; // a card could still be played

            List<Card> hand = game.Players[PlayerIndex].Hand;
            var options = Enumerable.Range(0, hand.Count)
                .Where(h => !(hand[h] is EssenceCard) && game.Validate(GameCommand.DiscardCard(PlayerIndex, h)) == null)
                .ToList();
            if (options.Count == 0) return endTurn;

            var doubles = options.Where(h => hand.Count(c => c.Id == hand[h].Id) >= 2).ToList();
            var pool = doubles.Count > 0 ? doubles : options;
            _discardedTurn = game.TurnNumber;
            return GameCommand.DiscardCard(PlayerIndex, pool[_rng.Next(pool.Count)]);
        }

        /// <summary>Finishes the bot's turn and plays the opponent's reply with plain greedy bots on both sides.</summary>
        void PlayOut(Game g, int seed)
        {
            int me = PlayerIndex;
            var mine = new Bot(me, seed) { Lookahead = false };
            var theirs = new Bot(1 - me, seed + 1) { Lookahead = false };
            for (int guard = 0; !g.IsOver && g.ActiveIndex == me && guard < 30; guard++)
                Step(g, mine, me);
            for (int guard = 0; !g.IsOver && g.ActiveIndex != me && guard < 30; guard++)
                Step(g, theirs, 1 - me);

            static void Step(Game g, Bot bot, int index)
            {
                GameCommand c = bot.Decide(g) ?? GameCommand.EndTurn(index);
                if (g.TryApply(c) != null) g.TryApply(GameCommand.EndTurn(index));
            }
        }

        // ───────────── Moves ─────────────

        /// <summary>Every legal move of the active player except ending the turn and paid discards, without duplicates.</summary>
        public static List<GameCommand> LegalMoves(Game game)
        {
            var moves = new List<GameCommand>();
            int p = game.ActiveIndex;
            Player me = game.ActivePlayer, enemy = game.InactivePlayer;
            bool essenceTried = false;
            var seen = new HashSet<int>(); // the same card twice in hand gives the same moves
            for (int h = 0; h < me.Hand.Count; h++)
            {
                Card card = me.Hand[h];
                if (card.Cost > me.Essence && !(card is EssenceCard)) continue;
                if (!seen.Add(card.Id)) continue;
                switch (card)
                {
                    case EssenceCard _:
                        if (!essenceTried && !me.PlayedEssenceCardThisTurn) moves.Add(GameCommand.PlayEssence(p, h));
                        essenceTried = true;
                        break;
                    case StageCard _:
                        if (game.ActiveStage?.Id != card.Id || game.ActiveStageOwner != me) moves.Add(GameCommand.PlayStage(p, h));
                        break;
                    case LegendCard _:
                        moves.Add(GameCommand.PlayLegend(p, h));
                        break;
                    case WeaponCard _:
                        for (int l = 0; l < me.PlayingField.Count; l++) moves.Add(GameCommand.PlayWeapon(p, h, l));
                        break;
                    case BattleCard battle when battle.PicksFromDiscard:
                    {
                        // Afterlife: the most expensive legend in the discard pile (the newest of equals).
                        int pick = -1;
                        for (int d = 0; d < me.DiscardPile.Count; d++)
                            if (me.DiscardPile[d] is LegendCard && (pick < 0 || me.DiscardPile[d].Cost >= me.DiscardPile[pick].Cost))
                                pick = d;
                        if (pick >= 0) moves.Add(GameCommand.PlayBattle(p, h, pick));
                        break;
                    }
                    case BattleCard battle:
                        if (!battle.TargetRequired) moves.Add(GameCommand.PlayBattle(p, h, -1));
                        else
                        {
                            Player side = battle.FriendlyFire ? me : enemy;
                            for (int t = 0; t < side.PlayingField.Count; t++) moves.Add(GameCommand.PlayBattle(p, h, t));
                        }
                        break;
                }
            }
            for (int l = 0; l < me.PlayingField.Count; l++)
            {
                LegendCard legend = me.PlayingField[l];
                for (int a = 0; a < legend.AttackCount; a++) // Weapon Throw included
                {
                    Attack attack = legend.AttackAt(a);
                    if (game.CanUseAttack(legend, attack) != null) continue;
                    // Burn the weapons the legend's attacks need least, then the ones giving the least elemental bonus.
                    int burn = Game.BurnChoiceNeeded(legend, attack) ? Game.SmartBurnMask(legend, attack) : 0;
                    switch (game.GetAttackMode(legend, attack))
                    {
                        case AttackMode.EnemyLegend:
                            for (int t = 0; t < enemy.PlayingField.Count; t++)
                                if (game.WantedAllows(enemy.PlayingField[t])) moves.Add(GameCommand.Attack(p, l, a, t, burn));
                            break;
                        case AttackMode.OwnLegend:
                            for (int t = 0; t < me.PlayingField.Count; t++) moves.Add(GameCommand.Attack(p, l, a, t, burn));
                            break;
                        default:
                            moves.Add(GameCommand.Attack(p, l, a, -1, burn));
                            break;
                    }
                }
            }
            return moves.Where(m => game.Validate(m) == null).ToList();
        }

        /// <summary>Shuffle back expensive cards the hand can't use soon and Essence beyond what it needs.</summary>
        static int ChooseRedraw(Player me)
        {
            List<Card> hand = me.Hand;
            int essence = hand.Count(c => c is EssenceCard);
            int mask = 0;
            int keepEssence = 0;
            for (int i = 0; i < hand.Count; i++)
            {
                Card c = hand[i];
                if (c is EssenceCard)
                {
                    if (++keepEssence > 4) mask |= 1 << i;
                }
                else if (c.Cost >= 5 || (essence <= 1 && c.Cost >= 3))
                    mask |= 1 << i;
            }
            return mask;
        }

        // ───────────── Scoring ─────────────

        /// <summary>How good the position is for <paramref name="me"/> (higher is better).</summary>
        public static double Evaluate(Game game, int me)
        {
            Player mine = game.Players[me], theirs = game.Players[1 - me];
            if (game.IsOver) return game.Winner == mine ? 1e6 - game.TurnNumber : -1e6 + game.TurnNumber;

            double score = 0;
            score += PlayerHealthValue(mine.Health) - PlayerHealthValue(theirs.Health);
            score += SideValue(game, mine) - SideValue(game, theirs);

            // An empty field lets the other side hit the player directly.
            if (theirs.PlayingField.Count == 0) score += 0.8 * mine.PlayingField.Sum(l => StrikeDamage(game, l, ignoreUsed: true));
            if (mine.PlayingField.Count == 0) score -= 0.8 * theirs.PlayingField.Sum(l => StrikeDamage(game, l, ignoreUsed: true));

            if (game.ActiveStage != null) score += game.ActiveStageOwner == mine ? 2 : -2;
            return score;
        }

        static double PlayerHealthValue(int hp)
        {
            // Each point matters more the closer the player is to dying.
            double v = hp;
            if (hp < 20) v -= (20 - hp) * 0.5;
            if (hp < 10) v -= (10 - hp) * 1.0;
            return v * 1.2;
        }

        static double SideValue(Game game, Player p)
        {
            double v = 0;
            int essence = p.EssenceField.Count;
            v += Math.Min(essence, 9) * 2.2 + Math.Max(0, essence - 9) * 0.3;
            int hand = p.Hand.Count;
            v += Math.Min(hand, 10) * 1.3 + Math.Max(0, hand - 10) * 0.3;
            foreach (LegendCard l in p.PlayingField) v += LegendValue(game, l);
            return v;
        }

        static double LegendValue(Game game, LegendCard l)
        {
            double v = 4; // being on the field (blocks direct hits, uses a slot)
            v += Math.Max(0, l.CurrentHP) * 0.55 + Math.Max(0, l.BaseHealth - l.CurrentHP) * 0.05;
            v += Potential(game, l) * 1.1;
            v += l.StackedWeapons.Count() * 0.4;
            if (l.IsBurned) v -= 1.5;
            if (l.IsStunned && l.Owner != game.ActivePlayer) v -= 1.5;
            if (l.WhileInPlayEffect != null) v += 2;
            v += l.Attacks.Count(a => a.Kind == AttackKind.Craft || a.Kind == AttackKind.Draw || a.Kind == AttackKind.Summon) * 0.6;
            return v;
        }

        /// <summary>The best damage the legend's attacks could do, partly counting attacks it is still building toward.</summary>
        static double Potential(Game game, LegendCard l)
        {
            double best = 0;
            foreach (Attack a in l.Attacks)
            {
                if (a.Kind != AttackKind.Strike) continue;
                if (!l.CanAttack) continue;
                double dmg = a.OneHitKO ? 9 : Attack.CalculateDamage(l, a);
                if (a.MultiHit) dmg *= 1.6;
                double ready = Readiness(l, a);
                best = Math.Max(best, ready >= 1 ? dmg : dmg * ready * 0.45);
            }
            return best;
        }

        static double Readiness(LegendCard l, Attack a)
        {
            double r = a.WeaponOneAmount <= 0 ? 1 : Math.Min(1.0, (double)Attack.CountWeapons(l, a.WeaponOne) / a.WeaponOneAmount);
            if (a.WeaponTwo != null && a.WeaponTwoAmount > 0)
                r *= Math.Min(1.0, (double)Attack.CountWeapons(l, a.WeaponTwo.Value) / a.WeaponTwoAmount);
            return r;
        }

        /// <summary>Damage the legend can deal with an attack it can use (as if it hadn't attacked yet when <paramref name="ignoreUsed"/>).</summary>
        static int StrikeDamage(Game game, LegendCard l, bool ignoreUsed)
        {
            if (l.IsStunned || !l.CanAttack || (!ignoreUsed && l.AttackedThisTurn)) return 0;
            int best = 0;
            foreach (Attack a in l.Attacks)
                if (a.Kind == AttackKind.Strike && !a.OneHitKO && a.MeetsWeaponRequirements(l))
                    best = Math.Max(best, Attack.CalculateDamage(l, a));
            return best;
        }
    }
}
