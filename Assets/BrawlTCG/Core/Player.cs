using System;
using System.Collections.Generic;

namespace BrawlTCG.Core
{
    public sealed class Player
    {
        public const int StartingHealth = 50; // was 30 in the WinForms version

        /// <summary>0 = the player who takes the first turn.</summary>
        public int Index { get; }
        public string Name { get; }
        public int Health { get; private set; } = StartingHealth;
        public int MaxHealth { get; private set; } = StartingHealth;
        public int Essence { get; internal set; }
        public bool PlayedEssenceCardThisTurn { get; internal set; }

        /// <summary>Top of the deck is the last element.</summary>
        public List<Card> Deck { get; } = new List<Card>();
        public List<Card> Hand { get; } = new List<Card>();
        public List<LegendCard> PlayingField { get; } = new List<LegendCard>();
        public List<Card> DiscardPile { get; } = new List<Card>();
        public List<Card> EssenceField { get; } = new List<Card>();

        public Player(int index, string name)
        {
            Index = index;
            Name = name;
        }

        internal Card DrawCardFromDeck()
        {
            if (Deck.Count == 0) return null;
            Card card = Deck[Deck.Count - 1];
            Deck.RemoveAt(Deck.Count - 1);
            Hand.Add(card);
            return card;
        }

        public void GainEssence(int gain) => Essence += gain;

        /// <summary>Staging only (<see cref="Game.StageHealth"/>).</summary>
        internal void SetHealth(int health, int maxHealth)
        {
            MaxHealth = maxHealth;
            Health = health;
        }

        public void BoostMaxHealth(int amount)
        {
            MaxHealth += amount;
            Health += amount;
        }

        public void GainHealth(int heal)
        {
            if (heal <= 0) return;
            Health = Math.Min(MaxHealth, Health + heal);
        }

        public void LoseHealth(int damage)
        {
            if (damage <= 0) return;
            Health = (int)Math.Max(int.MinValue / 2, (long)Health - damage);
        }

        public override string ToString() => Name;
    }
}
