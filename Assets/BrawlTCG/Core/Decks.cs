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

    /// <summary>
    /// One ready-to-play deck per element. Each is 40 cards with 14 Essence, built from that element's cards
    /// (Nature keeps its original Vial of Crows; Arctic borrows two legends), with weapons the deck's own legends can equip.
    /// </summary>
    public static class StarterDecks
    {
        public sealed class Starter
        {
            public string Name;
            public Element Element;
            public List<int> Cards;
        }

        public static readonly IReadOnlyList<Starter> All = new[]
        {
            Make("Starter - Fire", Element.Fire,
                (0, 14), (2004, 2), (2005, 3), (2006, 2), (2001, 2), (2007, 2),
                (25003, 4), (24000, 4), (15000, 3), (23000, 2), (508, 2)),
            Make("Starter - Nature", Element.Nature,
                (0, 14), (1000, 3), (1003, 2), (1007, 2), (1008, 2), (1001, 2),
                (24001, 4), (13000, 3), (18000, 3), (19000, 2), (501, 2), (101, 1)),
            // Arcadia and Fait hit; Enchantress buffs/curses; Plague Knight crafts battle cards.
            Make("Starter - Magic", Element.Magic,
                (0, 14), (4000, 4), (4004, 4), (4001, 1), (4003, 1),
                (24006, 4), (22000, 3), (23004, 4), (507, 3), (518, 1), (519, 1)),
            // Rocket lances and spears; Galaxy Lance x4 powers Flamethrower.
            Make("Starter - Cosmic", Element.Cosmic,
                (0, 14), (3000, 3), (3001, 3), (3005, 3), (3002, 2), (3006, 1),
                (21000, 4), (24002, 3), (23002, 2), (24003, 1), (23001, 1), (512, 2), (105, 1)),
            // Only two Arctic legends exist, so Hellshot Hattori and The Rock join to wield the Arctic weapons
            // (blasters, sword, hammer, katars). No Atlantis: it would weaken those two.
            Make("Starter - Arctic", Element.Arctic,
                (0, 14), (7001, 4), (7000, 3), (5004, 2), (6003, 2),
                (15002, 3), (15003, 1), (24004, 2), (19002, 2), (20001, 1), (25002, 1),
                (502, 2), (509, 1), (510, 1), (524, 1)),
            // Cheap Dander pressure into Minotaur and The Rock.
            Make("Starter - Wild", Element.Wild,
                (0, 14), (6002, 3), (6001, 4), (6000, 1), (6003, 2),
                (19001, 4), (20000, 3), (13001, 2), (17001, 1), (513, 3), (503, 1), (525, 1), (527, 1)),
            // Swords, blasters and scythes; Dark Mage and Mastermind as finishers.
            Make("Starter - Shadow", Element.Shadow,
                (0, 14), (5000, 3), (5003, 2), (5004, 3), (5005, 3), (5002, 1), (4002, 1),
                (25000, 4), (15001, 3), (23005, 2), (501, 2), (500, 1), (516, 1)),
        };

        static Starter Make(string name, Element element, params (int id, int count)[] entries)
        {
            var cards = new List<int>();
            foreach (var (id, count) in entries)
                cards.AddRange(Enumerable.Repeat(id, count));
            return new Starter { Name = name, Element = element, Cards = cards };
        }
    }
}
