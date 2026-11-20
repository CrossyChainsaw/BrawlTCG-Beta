using System;
using System.Collections.Generic;

namespace BrawlTCG.Core
{
    /// <summary>
    /// Search copies for the bot: a full, independent copy of the match it can play moves on to see what they do.
    /// Never used for real or network matches.
    /// </summary>
    public sealed partial class Game
    {
        Game(Player[] players, Random rng)
        {
            _players = players;
            Rng = rng;
        }

        /// <summary>
        /// A deep copy of the whole match. The copy has its own random generator (<paramref name="seed"/>) and both
        /// decks are shuffled, so a bot looking ahead in it can't learn the real draw order or the real random results.
        /// The copy raises no notifications.
        /// </summary>
        /// <param name="hideHandOf">A player whose hand the searcher must not see (-1 for none): their hand is shuffled
        /// into their deck and they get a new hand of the same size from it.</param>
        public Game CloneForSearch(int seed, int hideHandOf = -1)
        {
            var cards = new Dictionary<Card, Card>();
            var players = new Player[2];
            var copy = new Game(players, new Random(seed));

            for (int i = 0; i < 2; i++)
            {
                Player src = _players[i];
                var p = new Player(src.Index, src.Name);
                p.SetHealth(src.Health, src.MaxHealth);
                p.Essence = src.Essence;
                p.PlayedEssenceCardThisTurn = src.PlayedEssenceCardThisTurn;
                players[i] = p;
            }

            Player MapPlayer(Player p) => p == null ? null : players[p.Index];
            Card MapCard(Card c)
            {
                if (c == null) return null;
                if (cards.TryGetValue(c, out Card mapped)) return mapped;
                Card fresh = c.CopyForSearch();
                cards.Add(c, fresh);
                fresh.Owner = MapPlayer(c.Owner);
                if (fresh is LegendCard legend)
                {
                    List<Card> stacked = legend.StackedCards;
                    legend.ReplaceStackedCards(new List<Card>(stacked.Count));
                    foreach (Card s in stacked) legend.StackedCards.Add(MapCard(s));
                }
                return fresh;
            }

            for (int i = 0; i < 2; i++)
            {
                Player src = _players[i], p = players[i];
                foreach (Card c in src.Deck) p.Deck.Add(MapCard(c));
                foreach (Card c in src.Hand) p.Hand.Add(MapCard(c));
                foreach (LegendCard c in src.PlayingField) p.PlayingField.Add((LegendCard)MapCard(c));
                foreach (Card c in src.DiscardPile) p.DiscardPile.Add(MapCard(c));
                foreach (Card c in src.EssenceField) p.EssenceField.Add(MapCard(c));
                // Hidden information: the copy doesn't know the real order of either deck (nor, if asked, this hand).
                int handSize = p.Hand.Count;
                if (i == hideHandOf)
                {
                    p.Deck.AddRange(p.Hand);
                    p.Hand.Clear();
                }
                List<Card> deck = p.Deck;
                for (int k = deck.Count - 1; k > 0; k--)
                {
                    int j = copy.Rng.Next(k + 1);
                    (deck[k], deck[j]) = (deck[j], deck[k]);
                }
                if (i == hideHandOf)
                    for (int k = 0; k < handSize; k++) p.DrawCardFromDeck();
            }

            copy.ActiveIndex = ActiveIndex;
            copy.ActiveStage = (StageCard)MapCard(ActiveStage);
            copy.ActiveStageOwner = MapPlayer(ActiveStageOwner);
            copy.DrawCardStartTurn = DrawCardStartTurn;
            copy.IsStarted = IsStarted;
            copy.IsOver = IsOver;
            copy.Winner = MapPlayer(Winner);
            copy.TurnNumber = TurnNumber;
            copy.StartingHandRedraw = StartingHandRedraw;
            copy.ChoosingStartingHands = ChoosingStartingHands;
            copy._chosenStartingHand[0] = _chosenStartingHand[0];
            copy._chosenStartingHand[1] = _chosenStartingHand[1];
            if (_redrawRng != null)
                copy._redrawRng = new[] { new Random(seed ^ 0x5bd1e995), new Random(seed ^ 0x27d4eb2d) };
            return copy;
        }
    }
}
