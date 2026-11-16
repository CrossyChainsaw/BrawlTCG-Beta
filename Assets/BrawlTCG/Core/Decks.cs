using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BrawlTCG.Core
{
    public static class DeckRules
    {
        public const int MinimumCardsToPlay = 40;
        public const int MinimumCardsToSave = 40; // was 20 in the WinForms version; raised to match the play minimum
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
    /// The decks that come with the game (<see cref="Premade"/>, the only ones handed out) and the ones that used to
    /// (<see cref="Retired"/>). <see cref="All"/> holds the original per-element starter decks (40 cards, 14 Essence each);
    /// they are no longer handed out, but tests and the deck simulator still use them as a benchmark.
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

        /// <summary>
        /// The six decks the game hands out: the top five of a 33-deck bot tournament (16 tactic decks, each tuned by
        /// simulation, against each other, the starters and the alpha decks) plus Frozen Tide. Named after how they win.
        /// </summary>
        public static readonly IReadOnlyList<Starter> Premade = new[]
        {
            // All Cosmic, so Silent Galaxy stops only the opponent's damage. Aurora Brynn and Artemis hit hard; chests refill.
            Make("Cosmic Lockdown", Element.Cosmic, (0, 13), (107, 3), (500, 1), (506, 4), (512, 1), (525, 1), (526, 1), (3000, 3), (3001, 2),
                (3002, 1), (3003, 4), (3005, 1), (23001, 1), (24002, 1), (24003, 3)),
            // Card advantage: chests, Witch Party and crafting legends (Breaker Shang, Fait, Ulgrim) bury the opponent in cards.
            Make("Treasure Hunt", Element.Magic, (0, 16), (506, 3), (513, 3), (514, 1), (516, 3), (518, 3), (521, 1), (525, 1), (2002, 1),
                (3005, 3), (4004, 2), (23004, 1), (24002, 2)),
            // Shadow/Wild aggro under Evil Hideout (+3 ATK): Ninja Spirit needs no weapon, Caspian and Dander are cheap,
            // Forgeborne Chests and Bouncy Bombs keep the pressure up. The fastest deck.
            Make("Hideout Rush", Element.Shadow, (0, 12), (104, 3), (500, 2), (516, 4), (517, 1), (5000, 3), (5003, 4), (5006, 1), (6000, 1),
                (6002, 2), (13002, 1), (17001, 1), (20000, 4), (25000, 1)),
            // Axes and katars: Dander early, The Minotaur and Raymesis late; Wild Chests and Adrenaline for reach.
            Make("Wild Axe", Element.Wild, (0, 14), (500, 2), (506, 4), (513, 4), (521, 1), (525, 1), (527, 1), (2001, 1), (2004, 1), (5001, 1),
                (6000, 2), (6002, 3), (13001, 1), (13002, 1), (20000, 3)),
            // All Fire under Mustafar: every non-Fire legend loses 2 HP each turn while Fire legends are immune.
            Make("Scorched Earth", Element.Fire, (0, 12), (100, 2), (500, 1), (503, 1), (506, 2), (514, 3), (2000, 2), (2001, 1), (2003, 3),
                (2004, 1), (2005, 4), (2006, 2), (2007, 1), (23000, 1), (23003, 2), (24000, 1), (25003, 1)),
            // Arctic control under Atlantis: non-Arctic legends lose 1 HP a turn and deal 2 less; The Rock and Snowman Kor
            // wall up, Sunken Chests refill. Slow and grinding (#10 in the tournament, kept because it plays so differently).
            Make("Frozen Tide", Element.Arctic, (0, 13), (106, 3), (500, 2), (506, 4), (515, 4), (525, 1), (5004, 1), (6003, 3), (7000, 2),
                (7001, 3), (19002, 3), (24004, 1)),
        };

        /// <summary>
        /// Decks that earlier versions handed out and that are gone or changed now: the alpha decks, Claude I-III under their
        /// old names, Overgrowth, and the earlier tunings of the kept decks. A player's copy is removed only while it is
        /// still exactly as it was handed out (edited decks are kept); a re-tuned deck is then handed out again.
        /// </summary>
        public static readonly IReadOnlyList<Starter> Retired = new[]
        {
            Make("Ninja Spirit Aggro", Element.Shadow, (0, 14), (104, 4), (500, 2), (503, 3), (504, 2), (5000, 4), (5003, 4), (6000, 2), (13000, 2), (17000, 4)),
            Make("Dark Duo", Element.Shadow, (0, 20), (500, 4), (502, 4), (516, 4), (517, 4), (519, 4), (1004, 4), (23004, 4)),
            Make("Faerie Queen", Element.Nature, (0, 20), (101, 4), (1000, 4), (1006, 4), (1008, 4), (18000, 4), (24001, 4)),
            Make("Katars", Element.Wild, (0, 20), (503, 2), (522, 1), (523, 4), (4003, 4), (5000, 4), (6002, 4), (20000, 4), (20001, 4)),
            Make("Fire Axe", Element.Fire, (0, 22), (100, 2), (104, 2), (505, 4), (2001, 4), (2002, 4), (2003, 4), (2004, 4), (13001, 2), (13002, 4), (15000, 2), (24000, 2)),
            Make("Fury Shang", Element.Fire, (0, 20), (100, 4), (104, 2), (508, 4), (2001, 4), (2006, 4), (5000, 4), (17000, 4), (24000, 4)),
            Make("Arctic Control", Element.Arctic, (0, 28), (105, 4), (106, 4), (509, 4), (510, 4), (511, 2), (515, 4), (5000, 4), (7000, 4), (7001, 4), (17000, 4), (24004, 4)),
            Make("Nature Magic", Element.Nature, (0, 30), (101, 4), (503, 4), (1000, 4), (1001, 4), (1002, 4), (4000, 4), (4002, 4), (18000, 4), (22000, 4), (22001, 4), (24000, 4), (24001, 4)),
            Make("Yumiko Gauntlet Control", Element.Nature, (0, 30), (101, 4), (1004, 4), (1005, 4), (1006, 4), (3002, 4), (4003, 4), (5000, 4), (5003, 4), (6001, 4), (17000, 4), (17001, 4), (19000, 4), (24000, 4)),
            Make("Sword & Scythe", Element.Shadow, (0, 42), (104, 4), (105, 4), (506, 4), (2000, 4), (2003, 4), (2004, 4), (2005, 4), (4001, 4), (4002, 4), (5004, 4), (5005, 4), (5006, 2), (23000, 4), (23001, 4), (23002, 4), (25000, 4), (25001, 4)),
            Make("Claude I", Element.Shadow, (0, 12), (104, 3), (500, 2), (516, 3), (517, 1), (5000, 4), (5003, 4), (5005, 1), (5006, 1),
                (6000, 1), (6002, 2), (13002, 1), (17001, 1), (20000, 2), (25000, 2)),
            Make("Claude II", Element.Cosmic, (0, 13), (107, 3), (506, 1), (512, 1), (526, 1), (3000, 3), (3001, 2), (3002, 2), (3003, 3),
                (3005, 2), (21000, 2), (23001, 2), (24002, 3), (24003, 2)),
            Make("Claude III", Element.Nature, (0, 12), (521, 1), (1000, 2), (1001, 4), (1008, 4), (3002, 1), (3003, 2), (4000, 2),
                (18000, 4), (24001, 4), (24002, 1), (24005, 1), (24006, 2)),
            Make("Hideout Rush", Element.Shadow, (0, 12), (104, 3), (500, 2), (516, 3), (517, 1), (5000, 4), (5003, 4), (5005, 1), (5006, 1),
                (6000, 1), (6002, 2), (13002, 1), (17001, 1), (20000, 2), (25000, 2)),
            Make("Cosmic Lockdown", Element.Cosmic, (0, 13), (107, 3), (506, 1), (512, 1), (526, 1), (3000, 3), (3001, 2), (3002, 2), (3003, 3),
                (3005, 2), (21000, 2), (23001, 2), (24002, 3), (24003, 2)),
            Make("Overgrowth", Element.Nature, (0, 12), (521, 1), (1000, 2), (1001, 4), (1008, 4), (3002, 1), (3003, 2), (4000, 2),
                (18000, 4), (24001, 4), (24002, 1), (24005, 1), (24006, 2)),
            Make("Wild Axe", Element.Wild, (0, 13), (500, 3), (513, 2), (521, 2), (2002, 2), (2004, 2), (5001, 2), (6000, 1), (6002, 4),
                (13000, 1), (13001, 2), (13002, 2), (20000, 4)),
        }.Concat(All).ToArray(); // the seven starter decks are no longer handed out either

        static Starter Make(string name, Element element, params (int id, int count)[] entries)
        {
            var cards = new List<int>();
            foreach (var (id, count) in entries)
                cards.AddRange(Enumerable.Repeat(id, count));
            return new Starter { Name = name, Element = element, Cards = cards };
        }
    }
}
