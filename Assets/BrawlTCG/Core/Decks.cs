using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BrawlTCG.Core
{
    public static class DeckRules
    {
        public const int MinimumCardsToPlay = 40;
        public const int MinimumCardsToSave = 20;
        public const int MinimumEssenceToSave = 10;
        public const int MaxCopies = 4;
        const int EssenceId = 0;

        public static int CountCopies(IEnumerable<int> deck, int cardId) => deck.Count(id => id == cardId);

        /// <summary>Returns null if another copy may be added, otherwise why not.</summary>
        public static string CanAddCard(IList<int> deck, int cardId)
        {
            if (!CardCatalogue.Exists(cardId)) return $"Unknown card {cardId}";
            if (cardId != EssenceId && CountCopies(deck, cardId) >= MaxCopies)
                return $"You can only add up to {MaxCopies} copies of this card!";
            return null;
        }

        public static string ValidateForSave(IList<int> deck)
        {
            if (deck.Count(id => id == EssenceId) < MinimumEssenceToSave)
                return $"Your deck must contain at least {MinimumEssenceToSave} Essence cards!";
            if (deck.Count < MinimumCardsToSave)
                return $"Your deck must contain at least {MinimumCardsToSave} cards!";
            return ValidateCards(deck);
        }

        public static string ValidateForPlay(IList<int> deck)
        {
            if (deck == null || deck.Count == 0)
                return "Your deck is empty! Please select or build a deck before playing.";
            if (deck.Count < MinimumCardsToPlay)
                return $"Your deck does not contain at least {MinimumCardsToPlay} cards. Your deck: {deck.Count}";
            return ValidateCards(deck);
        }

        static string ValidateCards(IList<int> deck)
        {
            foreach (int id in deck.Distinct())
            {
                if (!CardCatalogue.Exists(id)) return $"Unknown card id {id} in deck.";
                if (id != EssenceId && CountCopies(deck, id) > MaxCopies)
                    return $"{CardCatalogue.Get(id).Name}: more than {MaxCopies} copies.";
            }
            return null;
        }

        public static List<int> Shuffle(IList<int> deck, Random rng)
        {
            var list = deck.ToList();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }

    /// <summary>Deck files: one card id per line (same format as the WinForms version).</summary>
    public static class DeckFormat
    {
        public static List<int> Parse(string text, List<string> errors = null)
        {
            var ids = new List<int>();
            if (string.IsNullOrEmpty(text)) return ids;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) && CardCatalogue.Exists(id))
                    ids.Add(id);
                else
                    errors?.Add($"Invalid card ID found in deck file: {line}");
            }
            return ids;
        }

        public static string Serialize(IEnumerable<int> ids) => string.Join("\n", ids.Select(i => i.ToString(CultureInfo.InvariantCulture)));
    }
}
