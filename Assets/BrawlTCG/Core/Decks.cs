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
    /// The decks that come with the game (<see cref="Premade"/>). Players never get them as editable files: each player
    /// owns <see cref="StarterDeckCount"/> starter decks picked from <see cref="StarterPool"/>, and unlocks the rest by
    /// beating the bot while it plays them (see <see cref="Starter.UnlockLevel"/>). <see cref="All"/> holds the original
    /// per-element starter decks (40 cards, 14 Essence each); they are no longer handed out, but tests and the deck
    /// simulator still use them as a benchmark.
    /// </summary>
    public static class StarterDecks
    {
        public sealed class Starter
        {
            public string Name;
            public Element Element;
            public List<int> Cards;
            /// <summary>Place in the 75-deck Hard-bot round robin (1 = strongest).</summary>
            public int Rank;
            /// <summary>Win rate in that round robin, in percent.</summary>
            public float WinRate;
            /// <summary>One line on how the deck wins.</summary>
            public string Description;

            /// <summary>Strength in stars from the win rate: 4 for 70%+, 3 for 60-70%, 2 for 50-60%, 1 below 50%.</summary>
            public int Stars => WinRate >= 70 ? 4 : WinRate >= 60 ? 3 : WinRate >= 50 ? 2 : 1;

            /// <summary>A starter-pool deck: handed out as a starter, never played by the bot and never unlocked.</summary>
            public bool IsStarterPool => Rank >= StarterPoolFirstRank;

            /// <summary>
            /// The bot difficulty that plays this deck, and that you beat to unlock it: ranks 1-16 Hard, 17-32 Normal,
            /// 33-48 Easy. Starter-pool decks report Easy but are in no bot's pool.
            /// </summary>
            public Bot.Level UnlockLevel => Rank <= DecksPerBot ? Bot.Level.Hard : Rank <= 2 * DecksPerBot ? Bot.Level.Normal : Bot.Level.Easy;
        }

        /// <summary>Starter decks are picked from these ranks of <see cref="Premade"/>.</summary>
        public const int DecksPerBot = 16, StarterPoolFirstRank = 3 * DecksPerBot + 1, StarterPoolLastRank = 64, StarterDeckCount = 3;

        public static IEnumerable<Starter> StarterPool => Premade.Where(s => s.Rank >= StarterPoolFirstRank && s.Rank <= StarterPoolLastRank);

        /// <summary>The premade deck with this exact name, or null.</summary>
        public static Starter Find(string name) => Premade.FirstOrDefault(s => s.Name == name);

        /// <summary>The premade decks the bot plays at this difficulty, strongest first.</summary>
        public static List<Starter> ForLevel(Bot.Level level) => Premade.Where(s => !s.IsStarterPool && s.UnlockLevel == level).ToList();

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
        /// The 64 decks that ship with the game, strongest first: the top 64 of a 75-deck round robin with the Hard bot
        /// (13,875 games, v1.5.0 rules). docs/DECK_ANALYSIS_75.md has the strategies, results and how the
        /// decks were built. Ranks 1-48 are bot decks (16 per difficulty), ranks 49-64 the starter pool.
        /// </summary>
        public static readonly IReadOnlyList<Starter> Premade = new[]
        {
            // #1, 80.0%
            Make(1, 80.0f, "Galaxy Fans", Element.Cosmic, "Silent Galaxy lock with Cosmic spear legends, boosted by Superfan Brynn.",
                (0, 13), (107, 3), (512, 2), (531, 3), (3001, 3), (3002, 3), (3003, 4), (3005, 3), (24002, 2), (24003, 4)),
            // #2, 79.2%
            Make(2, 79.2f, "Cosmic Lockdown", Element.Cosmic, "All-Cosmic deck: Silent Galaxy shuts off non-Cosmic attackers, then Aurora Brynn and Artemis beat down.",
                (0, 13), (107, 3), (500, 1), (506, 4), (512, 1), (525, 1), (526, 1), (3000, 3), (3001, 2), (3002, 1), (3003, 4), (3005, 1), (23001, 1), (24002, 1), (24003, 3)),
            // #3, 72.7%
            Make(3, 72.7f, "Hideout Rush", Element.Shadow, "Fast Shadow and Wild aggro pumped by Evil Hideout's +3 power.",
                (0, 12), (104, 3), (500, 2), (516, 4), (517, 1), (5000, 3), (5003, 4), (5006, 1), (6000, 1), (6002, 2), (13002, 1), (17001, 1), (20000, 4), (25000, 1)),
            // #4, 71.6%
            Make(4, 71.6f, "Hideout Ninjas", Element.Shadow, "Evil Hideout turns 1-HP Ninja Spirits into 6-power attackers. Dullahan Jiro keeps summoning more.",
                (0, 12), (104, 4), (500, 2), (516, 2), (523, 1), (528, 2), (5003, 4), (5004, 3), (5005, 4), (5006, 2), (25000, 4)),
            // #5, 71.6%
            Make(5, 71.6f, "Cosmic Lancers", Element.Cosmic, "Artemis and Orion Flamethrower with Galaxy Lances. Aurora Brynn and Silent Galaxy close it out.",
                (0, 11), (107, 2), (512, 4), (3000, 4), (3001, 4), (3003, 2), (3005, 2), (21000, 4), (23001, 2), (24002, 2), (24003, 3)),
            // #6, 71.1%
            Make(6, 71.1f, "Galaxy Fists", Element.Cosmic, "Silent Galaxy plus Wu Shang's multi-hit Down Sig. Breaker Shang crafts extra Cosmic cards.",
                (0, 13), (107, 4), (512, 2), (525, 1), (3002, 4), (3003, 4), (3005, 3), (17003, 3), (17004, 2), (24003, 3), (24005, 1)),
            // #7, 71.0%
            Make(7, 71.0f, "Galaxy Lock", Element.Cosmic, "Four Silent Galaxies, so only Cosmic legends deal damage. Aurora Brynn and Wu Shang's Down Sig win.",
                (0, 13), (107, 4), (512, 2), (3000, 3), (3002, 3), (3003, 4), (3005, 3), (17003, 3), (23002, 2), (24003, 3)),
            // #8, 70.4%
            Make(8, 70.4f, "Treasure Hunt", Element.Magic, "Card advantage: chests, Witch Party and crafting legends outlast the opponent.",
                (0, 16), (506, 3), (513, 3), (514, 1), (516, 3), (518, 3), (521, 1), (525, 1), (2002, 1), (3005, 3), (4004, 2), (23004, 1), (24002, 2)),
            // #9, 69.0%
            Make(9, 69.0f, "Cursed Kunai", Element.Shadow, "Katar legends and Cursed Kunai crafting under four Evil Hideouts.",
                (0, 11), (104, 4), (500, 1), (516, 2), (522, 1), (523, 4), (5000, 1), (5003, 2), (6002, 4), (6003, 4), (20000, 3), (20001, 3)),
            // #10, 68.9%
            Make(10, 68.9f, "Wild Axe", Element.Wild, "Danders pressure early, The Minotaur and Raymesis take over late. Chests refill the hand.",
                (0, 14), (500, 2), (506, 4), (513, 4), (521, 1), (525, 1), (527, 1), (2001, 1), (2004, 1), (5001, 1), (6000, 2), (6002, 3), (13001, 1), (13002, 1), (20000, 3)),
            // #11, 67.6%
            Make(11, 67.6f, "Matrix Masters", Element.Shadow, "Mastermind and Witch Scarlet set Matrix to erase enemy stages. Dark Mage and Kitsune Hattori hit hard.",
                (0, 13), (500, 3), (516, 3), (523, 1), (528, 1), (4002, 4), (5001, 1), (5002, 4), (5006, 3), (23005, 4), (25000, 3)),
            // #12, 67.3%
            Make(12, 67.3f, "Galaxy Lancers", Element.Cosmic, "Four Silent Galaxies. Artemis and Orion Flamethrower, Aurora Brynn finishes with spears.",
                (0, 12), (107, 4), (512, 3), (3000, 2), (3001, 3), (3003, 3), (3005, 3), (21000, 4), (24002, 2), (24003, 4)),
            // #13, 67.0%
            Make(13, 67.0f, "Tombstone Control", Element.Shadow, "Big Shadow bodies (Raymesis, Dark Mage) behind removal: Tombstone, Bouncy Bomb and Death's Hour.",
                (0, 13), (500, 2), (516, 3), (517, 2), (528, 3), (3000, 1), (4002, 3), (5000, 1), (5001, 3), (5002, 1), (13002, 2), (17000, 2), (23005, 4)),
            // #14, 65.1%
            Make(14, 65.1f, "Hideout Brutes", Element.Shadow, "Raymesis and The Minotaur swing their axes for 13 or more under Evil Hideout.",
                (0, 13), (104, 4), (500, 3), (516, 2), (1003, 2), (5001, 4), (6000, 4), (13000, 3), (13001, 4), (13002, 1)),
            // #15, 65.1%
            Make(15, 65.1f, "Scythe Executioners", Element.Cosmic, "Stack three scythes for Scythe Gimp one-hit KOs. Scythe Forever crafts the scythes.",
                (0, 14), (512, 3), (526, 2), (2000, 2), (3000, 4), (4002, 4), (5002, 1), (6003, 1), (21000, 1), (23001, 4), (23002, 4)),
            // #16, 64.5%
            Make(16, 64.5f, "Scorched Earth", Element.Fire, "All Fire: Mustafar melts non-Fire legends while the Fire legends attack.",
                (0, 12), (100, 2), (500, 1), (503, 1), (506, 2), (514, 3), (2000, 2), (2001, 1), (2003, 3), (2004, 1), (2005, 4), (2006, 2), (2007, 1), (23000, 1), (23003, 2), (24000, 1), (25003, 1)),
            // #17, 64.2%
            Make(17, 64.2f, "Superfan Spears", Element.Arctic, "Superfan Brynn buffs every spear legend and hands out weapons: Aurora Brynn, Orion, Alpine Kaya and Ada.",
                (0, 13), (509, 2), (512, 1), (531, 4), (3001, 3), (3002, 3), (3003, 3), (7001, 2), (7002, 3), (24003, 3), (24004, 3)),
            // #18, 63.6%
            Make(18, 63.6f, "Galaxy Commanders", Element.Cosmic, "Silent Galaxy stops non-Cosmic attackers, so Astro Commander's 14-attack glass cannon survives.",
                (0, 12), (107, 4), (500, 1), (512, 2), (516, 1), (517, 1), (525, 1), (3002, 3), (3004, 4), (3005, 3), (15001, 4), (17003, 4)),
            // #19, 63.5%
            Make(19, 63.5f, "Frozen Tide", Element.Arctic, "Arctic control: Atlantis weakens and drains, Snowman Kor freezes, walls hold.",
                (0, 13), (106, 3), (500, 2), (506, 4), (515, 4), (525, 1), (5004, 1), (6003, 3), (7000, 2), (7001, 3), (19002, 3), (24004, 1)),
            // #20, 63.2%
            Make(20, 63.2f, "Fallout Control", Element.Fire, "Molten Kor walls and Iron Lady grind; Fallout resets the board whenever the opponent gets ahead.",
                (0, 14), (514, 2), (530, 3), (2000, 3), (2001, 3), (2002, 1), (2003, 3), (2006, 2), (23000, 3), (23003, 2), (24000, 4)),
            // #21, 63.0%
            Make(21, 63.0f, "Kitsune Blades", Element.Shadow, "Sword and blaster midrange: Hellshot Hattori double shots, Kitsune Hattori finishes, Tombstone removes walls.",
                (0, 12), (500, 3), (516, 2), (528, 2), (4002, 2), (5003, 1), (5004, 3), (5005, 3), (5006, 2), (15001, 4), (23005, 2), (25000, 4)),
            // #22, 62.4%
            Make(22, 62.4f, "Fangwild Garden", Element.Nature, "Fangwild grows Nature and Magic legends every turn. Arcadia's Pink Roses hit, Briar Rose stuns.",
                (0, 12), (101, 4), (1000, 3), (1001, 3), (1008, 3), (4000, 4), (18000, 4), (24001, 4), (24006, 3)),
            // #23, 61.2%
            Make(23, 61.2f, "Galaxy Reapers", Element.Cosmic, "Silent Galaxy stops the enemy while Artemis Scythe Gimps anything. Scythe Forever refills.",
                (0, 13), (107, 4), (526, 2), (3000, 4), (3001, 3), (3003, 4), (23001, 2), (23002, 4), (24002, 4)),
            // #24, 59.5%
            Make(24, 59.5f, "Ninja Swarm", Element.Shadow, "Cheap Ninja Spirits and Danders flood the board under Evil Hideout. Caspian refills.",
                (0, 11), (104, 2), (500, 3), (516, 1), (528, 1), (1008, 1), (5000, 1), (5003, 4), (5005, 3), (5006, 2), (6002, 4), (20000, 3), (25000, 4)),
            // #25, 59.1%
            Make(25, 59.1f, "Briar Garden", Element.Nature, "Faerie Queen grows every Nature legend as Nature cards are played. Briar Rose stuns.",
                (0, 10), (101, 2), (521, 1), (1000, 4), (1001, 3), (1003, 4), (1008, 4), (13000, 2), (17004, 2), (18000, 4), (24001, 4)),
            // #26, 56.5%
            Make(26, 56.5f, "Space Time Giants", Element.Shadow, "Space Time digs to Kitsune Hattori and Aurora Brynn faster. Swords and spears.",
                (0, 14), (105, 4), (5006, 4), (3003, 4), (5004, 3), (3002, 1), (24003, 4), (25000, 4), (528, 2)),
            // #27, 56.2%
            Make(27, 56.2f, "Matrix Witches", Element.Cosmic, "Matrix erases enemy stages. Witch Party arms Artemis and Iron Lady for Meltdown.",
                (0, 12), (102, 4), (506, 3), (518, 3), (2000, 2), (3000, 4), (3006, 1), (4004, 2), (21000, 3), (23002, 2), (23004, 4)),
            // #28, 55.1%
            Make(28, 55.1f, "Sozin's Inferno", Element.Fire, "Ramp with Iron Lady, Ulgrim and Heatblast into Sozin's Comet (+5/+5 to Fire).",
                (0, 14), (108, 1), (514, 3), (2000, 4), (2001, 3), (2002, 1), (2003, 3), (2004, 1), (2006, 2), (13002, 2), (15000, 3), (23000, 3)),
            // #29, 53.6%
            Make(29, 53.6f, "Rock Wall", Element.Wild, "The Rock (20 HP) times seven: Did Shaw Win Yet? crafts more. Super Saiyan and Cody Travis.",
                (0, 14), (500, 2), (503, 2), (510, 2), (525, 2), (527, 3), (6000, 1), (6001, 2), (6003, 4), (19001, 4), (19002, 3), (20000, 1)),
            // #30, 53.5%
            Make(30, 53.5f, "Space Time Rush", Element.Shadow, "An extra card every turn for a cheap Dander, Ninja Spirit and Jiro flood.",
                (0, 11), (105, 4), (516, 1), (523, 1), (526, 1), (2005, 3), (5003, 4), (5004, 3), (6002, 4), (20000, 4), (25000, 4)),
            // #31, 52.7%
            Make(31, 52.7f, "Last Hour", Element.Shadow, "Death's Hour sets every legend to 1 HP, then 0-cost Orbs and Ninja Spirits sweep up.",
                (0, 10), (104, 2), (500, 3), (504, 2), (516, 2), (517, 1), (522, 3), (523, 1), (528, 3), (1006, 3), (5003, 4), (6002, 4), (20000, 2)),
            // #32, 52.6%
            Make(32, 52.6f, "Mustafar Burn", Element.Fire, "Molten Kor sets Mustafar and Heatblast burns. Fire legends are immune while the rest melt.",
                (0, 11), (100, 2), (514, 4), (2001, 3), (2003, 3), (2004, 2), (2005, 3), (2006, 3), (15000, 3), (17002, 2), (25003, 4)),
            // #33, 52.4%
            Make(33, 52.4f, "Workshop Forge", Element.Fire, "Ulgrim's Workshop crafts every turn. Seven's cannons and crafted weapons arm the board.",
                (0, 14), (514, 2), (2000, 1), (2001, 2), (2002, 4), (2003, 2), (2007, 4), (13002, 4), (24000, 4), (26000, 3)),
            // #34, 52.3%
            Make(34, 52.3f, "Reef Ninjas", Element.Shadow, "Mako's Reef with Ninja Spirits and Dullahan Jiro: every 1-HP ninja kills whatever it hits.",
                (0, 12), (110, 4), (516, 3), (532, 2), (5000, 3), (5003, 4), (5005, 4), (23005, 2), (25000, 4), (25001, 2)),
            // #35, 52.2%
            Make(35, 52.2f, "Atlantis Snowfall", Element.Arctic, "Atlantis slows the enemy while Snowballs, Sunken Chests and Kaya's crafted Snowballs hit the player.",
                (0, 11), (106, 4), (500, 3), (502, 3), (509, 1), (515, 4), (5006, 3), (7001, 4), (7002, 3), (24004, 4)),
            // #36, 50.5%
            Make(36, 50.5f, "Matrix Reapers", Element.Shadow, "Cheap Matrix wipes the enemy stage. Dark Mage and Mastermind slash with scythes.",
                (0, 12), (102, 4), (500, 1), (4002, 4), (5002, 3), (5003, 3), (5005, 4), (23003, 3), (23005, 3), (25000, 3)),
            // #37, 50.5%
            Make(37, 50.5f, "Treasure Engine", Element.Magic, "Chests, Witch Party and crafting and drawing legends bury the opponent in cards.",
                (0, 12), (506, 4), (518, 3), (519, 2), (2000, 2), (3005, 3), (4003, 2), (4004, 4), (5000, 2), (17000, 2), (23004, 4)),
            // #38, 49.2%
            Make(38, 49.2f, "Deathcap Storm", Element.Nature, "Orb weapons for Deathcap's board-wide Storm. Fait draws, Forest Spirits trade.",
                (0, 13), (101, 2), (1000, 3), (1002, 4), (1006, 1), (1008, 3), (4004, 4), (22000, 2), (22001, 4), (24001, 4)),
            // #39, 49.0%
            Make(39, 49.0f, "Mustafar Walls", Element.Fire, "Iron Lady and Molten Kor tank while Mustafar melts the enemy. Scythes and lances finish.",
                (0, 14), (100, 4), (510, 1), (514, 2), (2000, 4), (2003, 4), (2005, 2), (21000, 2), (23000, 4), (23003, 3)),
            // #40, 47.7%
            Make(40, 47.7f, "Enchanted Court", Element.Magic, "Arcadia's Pink Roses, Enchantress buffs and curses, Promotion for extra health.",
                (0, 11), (506, 4), (507, 2), (518, 1), (519, 1), (4000, 4), (4001, 2), (4004, 4), (18000, 2), (22000, 4), (23004, 2), (24006, 3)),
            // #41, 44.9%
            Make(41, 44.9f, "Fire Nation", Element.Fire, "Fire swarm: Fury Shang and Fire Nation grow every Fire legend. Ancestor's Flame swords.",
                (0, 10), (100, 1), (508, 3), (2001, 2), (2004, 3), (2005, 4), (2006, 4), (2007, 1), (5003, 2), (17002, 1), (23003, 2), (24000, 3), (25003, 4)),
            // #42, 44.3%
            Make(42, 44.3f, "Workshop Smiths", Element.Cosmic, "The Workshop plus crafting legends: Breaker Shang, Seven and Wu Shang with spears and gauntlets.",
                (0, 12), (103, 4), (2006, 4), (2007, 3), (3002, 3), (3005, 4), (17003, 4), (24000, 3), (24003, 3)),
            // #43, 44.3%
            Make(43, 44.3f, "Space Time Chests", Element.Cosmic, "Space Time and Cosmic Chests flood the hand. Breaker Shang crafts more Cosmic cards.",
                (0, 13), (105, 4), (3005, 4), (3001, 3), (3000, 3), (3003, 2), (512, 4), (506, 3), (21000, 2), (24003, 2)),
            // #44, 43.2%
            Make(44, 43.2f, "Wild Stampede", Element.Wild, "Dander rush into The Minotaur. A-Oooooo and Evil Hideout pump the pack.",
                (0, 13), (104, 1), (513, 3), (521, 2), (529, 2), (6000, 4), (6001, 1), (6002, 3), (6003, 2), (13001, 3), (17001, 1), (19001, 1), (20000, 4)),
            // #45, 43.0%
            Make(45, 43.0f, "Petra Hideout", Element.Shadow, "Evil Hideout pumps Danders, Caspians and Raymesis; Petra comes down as a 21-power finisher.",
                (0, 13), (104, 3), (500, 1), (528, 2), (5000, 3), (5001, 3), (5007, 2), (6002, 4), (13001, 1), (17000, 2), (17001, 3), (20000, 3)),
            // #46, 42.7%
            Make(46, 42.7f, "Astro Blasters", Element.Cosmic, "Glass cannons: Astro Commander (14 attack) and Hellshot Hattori, with Adrenaline for double shots.",
                (0, 12), (500, 3), (515, 1), (525, 1), (3003, 1), (3004, 4), (3005, 4), (5004, 4), (15001, 4), (15003, 2), (24002, 2), (25000, 2)),
            // #47, 42.4%
            Make(47, 42.4f, "Lava Blasters", Element.Fire, "Mustafar melts non-Fire boards while Heatblast burns and Jhala slashes.",
                (0, 10), (100, 4), (515, 2), (527, 1), (2001, 3), (2003, 4), (2004, 4), (2005, 4), (2006, 1), (15000, 3), (25003, 4)),
            // #48, 42.2%
            Make(48, 42.2f, "Hour Hideout", Element.Shadow, "Evil Hideout aggro with Danders and Ninjas; Death's Hour breaks walls.",
                (0, 14), (104, 3), (504, 1), (513, 2), (522, 2), (5000, 3), (5003, 4), (5005, 1), (6002, 4), (20000, 4), (25000, 2)),
            // #49, 42.0%
            Make(49, 42.0f, "Fallout Blaze", Element.Fire, "Cheap Fire aggro with Fire Nation; Fallout clears the way when the board stalls.",
                (0, 12), (100, 2), (508, 2), (514, 1), (530, 3), (2001, 2), (2002, 1), (2003, 1), (2004, 1), (2005, 2), (2006, 3), (2007, 2), (24000, 4), (25003, 4)),
            // #50, 41.6%
            Make(50, 41.6f, "Hideout Claws", Element.Wild, "Evil Hideout makes Dander a 9-power 1-drop. The Rock and The Minotaur hold the line with hammers.",
                (0, 13), (104, 4), (513, 2), (529, 2), (6000, 3), (6002, 4), (6003, 3), (19001, 3), (20000, 4), (20001, 2)),
            // #51, 40.9%
            Make(51, 40.9f, "Art School", Element.Magic, "Chests and Art Class flood the board with cheap legends; Fait and Arcadia carry the weapons.",
                (0, 13), (101, 2), (506, 4), (518, 1), (532, 4), (3006, 2), (4000, 3), (4004, 3), (18000, 1), (22000, 2), (23004, 2), (24006, 3)),
            // #52, 40.7%
            Make(52, 40.7f, "Final Form", Element.Shadow, "Shadow ramp: lots of Essence, Raymesis and Caspian hold the board, Final Form Petra ends it.",
                (0, 15), (501, 2), (517, 2), (520, 1), (528, 2), (5000, 3), (5001, 3), (5007, 4), (6003, 1), (17000, 3), (17001, 1), (22000, 3)),
            // #53, 40.4%
            Make(53, 40.4f, "Workshop Thieves", Element.Magic, "The Workshop crafts while Caspian and Fait draw three. Dark Mage and Raymesis finish.",
                (0, 12), (103, 4), (500, 1), (4002, 3), (4003, 2), (4004, 4), (5000, 3), (5001, 1), (17000, 4), (23003, 1), (23004, 1), (23005, 4)),
            // #54, 40.4%
            Make(54, 40.4f, "Shadow Giants", Element.Shadow, "Big Shadow bodies: Kitsune Hattori, Dark Mage, then Final Form Petra, with Tombstone and Vials.",
                (0, 15), (501, 3), (506, 1), (516, 1), (519, 2), (522, 1), (528, 2), (4002, 3), (4003, 1), (5006, 2), (5007, 2), (17000, 2), (23005, 3), (25000, 2)),
            // #55, 40.1%
            Make(55, 40.1f, "Art Garden", Element.Nature, "Faerie Queen grows the Nature board with every Nature card; Art Class adds bodies.",
                (0, 12), (101, 2), (521, 3), (532, 3), (1000, 3), (1001, 3), (1006, 3), (1008, 3), (18000, 4), (24001, 4)),
            // #56, 39.9%
            Make(56, 39.9f, "Ulgrim's Forge", Element.Fire, "The Workshop crafts a card every turn. Ulgrim, Seven and Heatblast with spears and axes.",
                (0, 14), (103, 4), (514, 1), (2001, 2), (2002, 4), (2003, 1), (2006, 2), (2007, 4), (13002, 4), (24000, 4)),
            // #57, 39.7%
            Make(57, 39.7f, "Hour Sweep", Element.Shadow, "Death's Hour at 5 sets everything to 1 HP; Orbs, Ninja Spirits and Hellshot sweep up.",
                (0, 13), (504, 3), (522, 4), (5000, 3), (5003, 4), (5004, 3), (5005, 3), (5007, 1), (15001, 2), (25000, 4)),
            // #58, 39.2%
            Make(58, 39.2f, "Axe Fans", Element.Wild, "Axe legends with Superfan Brynn and Evil Hideout: The Minotaur, Rayman and Jhala.",
                (0, 13), (104, 2), (521, 2), (529, 1), (531, 4), (1003, 3), (2004, 2), (6000, 4), (13000, 3), (13001, 4), (13002, 2)),
            // #59, 38.6%
            Make(59, 38.6f, "Hideout Bombers", Element.Shadow, "Caspian draws and crafts Bouncy Bombs while Dander and Raymesis punch.",
                (0, 10), (104, 4), (500, 1), (514, 1), (516, 1), (528, 1), (5000, 3), (5001, 1), (6002, 3), (6003, 3), (17000, 4), (17001, 4), (20000, 4)),
            // #60, 38.1%
            Make(60, 38.1f, "Blaster Barrage", Element.Shadow, "Every blaster legend (Hellshot Hattori, Heatblast, Astro Commander) with Revolvers and Blazing Fire.",
                (0, 13), (500, 3), (521, 1), (2001, 2), (3004, 4), (5004, 4), (5006, 1), (15000, 4), (15001, 4), (15002, 2), (25000, 2)),
            // #61, 37.2%
            Make(61, 37.2f, "Frozen Fortress", Element.Arctic, "Alpine Kaya crafts Snowballs and Snowman Kor. Atlantis drains the rest.",
                (0, 11), (106, 4), (109, 1), (502, 2), (509, 2), (515, 2), (7000, 2), (7001, 3), (7002, 4), (15002, 3), (19002, 2), (24004, 4)),
            // #62, 36.1%
            Make(62, 36.1f, "Fangwild Brawlers", Element.Nature, "Rayman Power Punches twice with Adrenaline. Fangwild keeps the punchers alive.",
                (0, 13), (101, 4), (521, 2), (1003, 4), (1004, 2), (1006, 2), (1008, 3), (4003, 2), (13000, 4), (17004, 4)),
            // #63, 35.9%
            Make(63, 35.9f, "Fangwild Witches", Element.Magic, "Fait draws, Enchantress curses and Deathcap storms under Fangwild.",
                (0, 13), (101, 4), (506, 4), (1002, 2), (4001, 2), (4004, 4), (22000, 4), (22001, 3), (23004, 4)),
            // #64, 35.1%
            Make(64, 35.1f, "Mustafar Spears", Element.Fire, "Fury Shang, Heatblast and Seven with spears and gauntlets under Mustafar. Fire Nation pumps.",
                (0, 12), (100, 4), (505, 1), (508, 4), (516, 1), (2001, 2), (2003, 2), (2006, 4), (2007, 3), (17002, 4), (24000, 3)),
        };

        static Starter Make(int rank, float winRate, string name, Element element, string description, params (int id, int count)[] entries)
        {
            Starter s = Make(name, element, entries);
            s.Rank = rank;
            s.WinRate = winRate;
            s.Description = description;
            return s;
        }

        static Starter Make(string name, Element element, params (int id, int count)[] entries)
        {
            var cards = new List<int>();
            foreach (var (id, count) in entries)
                cards.AddRange(Enumerable.Repeat(id, count));
            return new Starter { Name = name, Element = element, Cards = cards };
        }
    }
}
