using System;
using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;

// Pure C# (no UnityEngine): the clips can be checked against the rules engine outside Unity.
namespace BrawlTCG.UnityApp
{
    public enum ShowStepKind { Play, Attack, EndTurn, Wait, Inspect }

    /// <summary>One scripted action of a showcase clip. Cards are named by id, resolved when the step runs.</summary>
    public sealed class ShowStep
    {
        public ShowStepKind Kind;
        /// <summary>Card to play, attacking legend, or card to inspect.</summary>
        public int CardId = -1;
        /// <summary>Which copy on the field (0 = first) when several share the id.</summary>
        public int Nth;
        /// <summary>Play: picks the hand card instead of <see cref="CardId"/> (crafted cards).</summary>
        public Func<Card, bool> Pick;
        public string AttackName;
        /// <summary>Legend the card / attack is aimed at (side implied by the card or attack); -1 for none.</summary>
        public int TargetId = -1;
        public int TargetNth;
        /// <summary>Seconds to wait after the step; negative = a default that fits the action.</summary>
        public float Pause = -1;

        public override string ToString()
        {
            string target = TargetId >= 0 ? $" -> {Name(TargetId)}" + (TargetNth > 0 ? $"#{TargetNth}" : "") : "";
            switch (Kind)
            {
                case ShowStepKind.Play: return $"Play {(Pick != null ? "(picked)" : Name(CardId))}{target}";
                case ShowStepKind.Attack: return $"{Name(CardId)}{(Nth > 0 ? "#" + Nth : "")} · {AttackName}{target}";
                case ShowStepKind.Inspect: return $"Inspect {Name(CardId)}";
                case ShowStepKind.Wait: return $"Wait {Pause}s";
                default: return Kind.ToString();
            }
        }

        static string Name(int id) => CardCatalogue.Exists(id) ? CardCatalogue.Get(id).Name : id.ToString();
    }

    /// <summary>Anything the showcase director can play (and record): a match clip or a deck-building clip.</summary>
    public abstract class ShowcaseItem
    {
        public string Id, Title, Subtitle;
    }

    /// <summary>A staged match position plus the moves to play from it.</summary>
    public sealed class ShowcaseClip : ShowcaseItem
    {
        public string Name0, Name1;
        /// <summary>The player shown at the bottom of the screen.</summary>
        public int Hero;
        public int Seed = 1;
        public Action<Game> Setup;
        public readonly List<ShowStep> Steps = new List<ShowStep>();

        public Game CreateGame()
        {
            var game = new Game(Name0, Array.Empty<int>(), Name1, Array.Empty<int>(), Seed);
            Setup(game);
            return game;
        }

        ShowcaseClip Add(ShowStep step) { Steps.Add(step); return this; }

        /// <summary>Plays a card that needs no target (essence, stage, legend, untargeted battle card).</summary>
        public ShowcaseClip Play(int id, float pause = -1) => Add(new ShowStep { Kind = ShowStepKind.Play, CardId = id, Pause = pause });

        /// <summary>Plays a weapon or targeted battle card on a legend.</summary>
        public ShowcaseClip PlayOn(int id, int targetId, int targetNth = 0, float pause = -1) =>
            Add(new ShowStep { Kind = ShowStepKind.Play, CardId = id, TargetId = targetId, TargetNth = targetNth, Pause = pause });

        public ShowcaseClip PlayPicked(Func<Card, bool> pick, int targetId = -1, float pause = -1) =>
            Add(new ShowStep { Kind = ShowStepKind.Play, Pick = pick, TargetId = targetId, Pause = pause });

        public ShowcaseClip Attack(int legendId, string attack, int targetId = -1, int targetNth = 0, int legendNth = 0, float pause = -1) =>
            Add(new ShowStep { Kind = ShowStepKind.Attack, CardId = legendId, Nth = legendNth, AttackName = attack, TargetId = targetId, TargetNth = targetNth, Pause = pause });

        public ShowcaseClip EndTurn(float pause = -1) => Add(new ShowStep { Kind = ShowStepKind.EndTurn, Pause = pause });

        public ShowcaseClip Wait(float seconds) => Add(new ShowStep { Kind = ShowStepKind.Wait, Pause = seconds });

        /// <summary>Opens the card's detail panel for a moment, like a player checking it.</summary>
        public ShowcaseClip Inspect(int id, int nth = 0, float seconds = 2.2f) =>
            Add(new ShowStep { Kind = ShowStepKind.Inspect, CardId = id, Nth = nth, Pause = seconds });
    }

    /// <summary>Turns showcase steps into game commands for the current position.</summary>
    public static class ShowcaseResolver
    {
        static int FindOnField(Player side, int id, int nth)
        {
            for (int i = 0, seen = 0; i < side.PlayingField.Count; i++)
                if (side.PlayingField[i].Id == id && seen++ == nth) return i;
            return -1;
        }

        /// <summary>The command for a Play / Attack / EndTurn step, or null with the reason.</summary>
        public static GameCommand Resolve(Game game, ShowStep step, out string error)
        {
            error = null;
            int p = game.ActiveIndex;
            Player me = game.ActivePlayer, enemy = game.InactivePlayer;
            GameCommand cmd;
            switch (step.Kind)
            {
                case ShowStepKind.EndTurn:
                    cmd = GameCommand.EndTurn(p);
                    break;

                case ShowStepKind.Play:
                {
                    int hand = step.Pick != null ? me.Hand.FindIndex(c => step.Pick(c)) : me.Hand.FindIndex(c => c.Id == step.CardId);
                    if (hand < 0) { error = $"{me.Name} has no such card in hand"; return null; }
                    Card card = me.Hand[hand];
                    int target = -1;
                    if (card is WeaponCard) target = FindOnField(me, step.TargetId, step.TargetNth);
                    else if (card is BattleCard battle && battle.TargetRequired)
                        target = FindOnField(battle.FriendlyFire ? me : enemy, step.TargetId, step.TargetNth);
                    cmd = GameCommand.PlayCard(card, p, hand, target);
                    break;
                }

                case ShowStepKind.Attack:
                {
                    int l = FindOnField(me, step.CardId, step.Nth);
                    if (l < 0) { error = $"{me.Name} has no such legend on the field"; return null; }
                    LegendCard legend = me.PlayingField[l];
                    int a = -1;
                    for (int i = 0; i < legend.Attacks.Count; i++)
                    {
                        if (!string.Equals(legend.Attacks[i].Name, step.AttackName, StringComparison.OrdinalIgnoreCase)) continue;
                        if (a < 0) a = i;
                        if (game.CanUseAttack(legend, legend.Attacks[i]) == null) { a = i; break; }
                    }
                    if (a < 0) { error = $"{legend.Name} has no attack \"{step.AttackName}\""; return null; }
                    int target = -1;
                    AttackMode mode = game.GetAttackMode(legend, legend.Attacks[a]);
                    if (mode == AttackMode.OwnLegend) target = FindOnField(me, step.TargetId, step.TargetNth);
                    else if (mode == AttackMode.EnemyLegend) target = FindOnField(enemy, step.TargetId, step.TargetNth);
                    cmd = GameCommand.Attack(p, l, a, target);
                    break;
                }

                default:
                    error = "Not a game action";
                    return null;
            }
            error = game.Validate(cmd);
            return error == null ? cmd : null;
        }

        /// <summary>The card an Inspect step looks at: a legend on either field (active side first), else a hand card.</summary>
        public static Card FindInspected(Game game, ShowStep step)
        {
            foreach (Player side in new[] { game.ActivePlayer, game.InactivePlayer })
            {
                int i = FindOnField(side, step.CardId, step.Nth);
                if (i >= 0) return side.PlayingField[i];
            }
            foreach (Player side in game.Players)
            {
                Card c = side.Hand.FirstOrDefault(x => x.Id == step.CardId);
                if (c != null) return c;
            }
            return null;
        }
    }
}
