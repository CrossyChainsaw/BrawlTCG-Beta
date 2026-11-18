using System;
using System.Linq;

namespace BrawlTCG.Core
{
    /// <summary>
    /// Staging: builds a mid-match position directly instead of playing up to it, for showcase clips and tests.
    /// Never used by normal or network matches. Call the Stage* methods first (the stage before the legends, so its
    /// effects reach them), then <see cref="BeginStaged"/> instead of <see cref="Start"/>.
    /// </summary>
    public sealed partial class Game
    {
        Card StageCreate(int player, int id)
        {
            Card card = CardCatalogue.Create(id);
            card.Owner = _players[player];
            return card;
        }

        void ThrowIfStarted()
        {
            if (IsStarted) throw new InvalidOperationException("Stage the position before BeginStaged.");
        }

        /// <summary>Starts the match in the staged position, mid-turn: no cards are drawn and no start-of-turn effects run.</summary>
        public void BeginStaged(int activeIndex, int turnNumber)
        {
            ThrowIfStarted();
            IsStarted = true;
            ActiveIndex = activeIndex;
            TurnNumber = turnNumber;
            foreach (Player p in _players)
                p.Essence = p.EssenceField.Count;
        }

        public void StageEssence(int player, int count)
        {
            ThrowIfStarted();
            for (int i = 0; i < count; i++)
                _players[player].EssenceField.Add(StageCreate(player, EssenceCardId));
        }

        public void StageHealth(int player, int health, int maxHealth = Player.StartingHealth)
        {
            ThrowIfStarted();
            _players[player].SetHealth(health, maxHealth);
        }

        public void StageHand(int player, params int[] ids)
        {
            ThrowIfStarted();
            foreach (int id in ids)
                _players[player].Hand.Add(StageCreate(player, id));
        }

        /// <summary>Replaces the deck. <paramref name="ids"/>[0] is the next card drawn.</summary>
        public void StageDeck(int player, params int[] ids)
        {
            ThrowIfStarted();
            Player p = _players[player];
            p.Deck.Clear();
            foreach (int id in ids.Reverse())
                p.Deck.Add(StageCreate(player, id)); // top of the deck is the last element
        }

        public void StageDiscard(int player, params int[] ids)
        {
            ThrowIfStarted();
            foreach (int id in ids)
            {
                Card card = StageCreate(player, id);
                card.IsDiscarded = true;
                _players[player].DiscardPile.Add(card);
            }
        }

        /// <summary>Puts a stage into play as if <paramref name="owner"/> had played it (its when-played effect runs).</summary>
        public void StageStage(int owner, int id)
        {
            ThrowIfStarted();
            var stage = (StageCard)StageCreate(owner, id);
            ActiveStage = stage;
            ActiveStageOwner = _players[owner];
            stage.WhenPlayedEffect?.Invoke(GetAllCardsInPlayingFields(), stage, this, stage);
        }

        /// <summary>
        /// Puts a legend on the field with cards stacked on it. Stackable battle cards (Super Saiyan...) apply their
        /// effect as if played on it. <paramref name="damage"/> is taken last, from its full (buffed) health.
        /// </summary>
        public LegendCard StageLegend(int player, int id, int damage = 0, params int[] stacked)
        {
            ThrowIfStarted();
            Player p = _players[player];
            var legend = (LegendCard)StageCreate(player, id);
            p.PlayingField.Add(legend);
            legend.OnPlayingField = true;
            ActiveStage?.WhileInPlayEffect?.Invoke(legend, ActiveStage, this, ActiveStage);
            foreach (int s in stacked)
            {
                Card card = StageCreate(player, s);
                if (card is BattleCard battle && battle.Stackable)
                    battle.WhenPlayedEffect?.Invoke(legend, battle, this, battle);
                legend.StackedCards.Add(card);
            }
            legend.LoseHealth(damage);
            return legend;
        }

        /// <summary>A stat change from an earlier turn, credited to <paramref name="source"/> in the legend's details.</summary>
        public void StageBuff(LegendCard legend, int power, int health, string source)
        {
            ThrowIfStarted();
            EffectContext.Push(source);
            try
            {
                if (health != 0) legend.ModifyStat(Stat.Health, health);
                if (power != 0) legend.ModifyStat(Stat.Power, power);
            }
            finally { EffectContext.Pop(); }
        }

        public void StageBurn(LegendCard legend, string source)
        {
            ThrowIfStarted();
            EffectContext.Push(source);
            try { legend.SetBurn(true); }
            finally { EffectContext.Pop(); }
        }
    }
}
