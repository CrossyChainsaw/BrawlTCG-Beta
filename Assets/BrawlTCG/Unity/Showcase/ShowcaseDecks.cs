using System.Collections.Generic;
using System.Linq;

// Pure C# (no UnityEngine): the decks can be checked against the deck rules outside Unity.
namespace BrawlTCG.UnityApp
{
    /// <summary>How the scripted cursor finds a card in the collection.</summary>
    public enum FindBy
    {
        /// <summary>Clicks the card's element chip, then scrolls to it.</summary>
        Element,
        /// <summary>Types (part of) its name into the search box.</summary>
        Search,
        /// <summary>Clicks its type chip (ESSENCE, STAGE...).</summary>
        Type,
    }

    public sealed class DeckPick
    {
        public int CardId;
        public int Copies;
        public FindBy Find;
        /// <summary>Text typed into the search box (FindBy.Search).</summary>
        public string Search;
        /// <summary>Pops up over the card when it goes in ("WIN CONDITION").</summary>
        public string Callout;
    }

    /// <summary>A deck-building clip: name a new deck, find and add each card, save.</summary>
    public sealed class DeckClip : ShowcaseItem
    {
        public string DeckName;
        public readonly List<DeckPick> Picks = new List<DeckPick>();

        public IEnumerable<int> Cards => Picks.SelectMany(p => Enumerable.Repeat(p.CardId, p.Copies));

        DeckClip Add(int id, int copies, FindBy find, string search = null, string callout = null)
        {
            Picks.Add(new DeckPick { CardId = id, Copies = copies, Find = find, Search = search, Callout = callout });
            return this;
        }

        public DeckClip Element(int id, int copies, string callout = null) => Add(id, copies, FindBy.Element, null, callout);
        public DeckClip Search(int id, int copies, string search, string callout = null) => Add(id, copies, FindBy.Search, search, callout);
        public DeckClip Type(int id, int copies, string callout = null) => Add(id, copies, FindBy.Type, null, callout);
    }

    public static class ShowcaseDecks
    {
        const int Essence = 0, Fangwild = 101, EvilHideout = 104, SilentGalaxy = 107;
        const int BriarRose = 1000, Deathcap = 1002, Rayman = 1003, Yumiko = 1005, FaerieQueen = 1008;
        const int Orion = 3001, WuShang = 3002, AuroraBrynn = 3003, AstroCommander = 3004;
        const int DarkMage = 4002, KitsuneHattori = 5006;
        const int Minotaur = 6000, FoxSpirit = 6001, Dander = 6002, TheRock = 6003;
        const int AxeOfRegrowth = 13000, BuzzAxe = 13001, Revolvers = 15001, SleightOfHand = 17000, Rippers = 17001;
        const int LawOfTheLand = 18000, PrimroseMallet = 19000, Chikara = 19001, GalaxyLance = 21000, SacredRelic = 22001;
        const int NecroAura = 23005, PiercingRegret = 24001, ShadowEdge = 25000;
        const int SuperSaiyan = 503, CosmicChest = 512, WildChest = 513, PromotionII = 520, Adrenaline = 521, DeathsHour = 522;

        public static IReadOnlyList<DeckClip> All { get; } = new[]
        {
            HourOfDeath(),
            SaiyanStampede(),
            CosmicLockdown(),
            FaerieCourt(),
        };

        static DeckClip HourOfDeath() =>
            new DeckClip
            {
                Id = "deck-hour-of-death", Title = "DECK LAB: HOUR OF DEATH", Subtitle = "WIPE THE BOARD  ·  ORB THE SURVIVORS",
                DeckName = "Hour of Death",
            }
            .Search(DeathsHour, 4, "death", "WIN CONDITION")
            .Element(Yumiko, 4, "ORB FACTORY")
            .Element(FoxSpirit, 4, "MORE ORBS")
            .Element(PrimroseMallet, 4)
            .Element(Adrenaline, 4, "ATTACK AGAIN")
            .Element(KitsuneHattori, 2, "FINISHER")
            .Element(DarkMage, 2)
            .Element(ShadowEdge, 2)
            .Element(NecroAura, 2)
            .Type(Essence, 12);

        static DeckClip SaiyanStampede() =>
            new DeckClip
            {
                Id = "deck-saiyan-stampede", Title = "DECK LAB: SAIYAN STAMPEDE", Subtitle = "ONE MINOTAUR  ·  FOUR TRANSFORMATIONS",
                DeckName = "Saiyan Stampede",
            }
            .Element(Minotaur, 4, "THE CARRY")
            .Search(SuperSaiyan, 4, "saiyan", "+5 / +5 EACH")
            .Search(Adrenaline, 4, "adren", "RAMPAGE")
            .Element(BuzzAxe, 4, "WILD AXE BONUS")
            .Element(Chikara, 2)
            .Element(Dander, 4, "EARLY PRESSURE")
            .Element(TheRock, 2, "THE WALL")
            .Element(WildChest, 4)
            .Type(EvilHideout, 2, "+3 POWER")
            .Type(Essence, 14);

        static DeckClip CosmicLockdown() =>
            new DeckClip
            {
                Id = "deck-cosmic-lockdown", Title = "DECK LAB: COSMIC LOCKDOWN", Subtitle = "SILENCE THE GALAXY  ·  ONLY COSMIC STRIKES",
                DeckName = "Cosmic Lockdown",
            }
            .Type(SilentGalaxy, 3, "LOCK THEM OUT")
            .Element(WuShang, 4, "HITS ALL")
            .Element(AstroCommander, 3, "14 POWER")
            .Element(AuroraBrynn, 3)
            .Element(Orion, 2)
            .Element(GalaxyLance, 4, "COSMIC BONUS")
            .Search(Rippers, 4, "rippers", "DSIG FUEL")
            .Search(SleightOfHand, 2, "sleight")
            .Search(Revolvers, 2, "revolv")
            .Element(CosmicChest, 3)
            .Search(PromotionII, 2, "promotion", "+25 MAX HP")
            .Type(Essence, 14);

        static DeckClip FaerieCourt() =>
            new DeckClip
            {
                Id = "deck-faerie-court", Title = "DECK LAB: FAERIE COURT", Subtitle = "EVERY NATURE CARD BOOSTS THE WHOLE ARMY",
                DeckName = "Faerie Court",
            }
            .Element(FaerieQueen, 4, "+1 / +1 PER NATURE CARD")
            .Element(Deathcap, 3, "STORM HITS ALL")
            .Element(BriarRose, 3)
            .Element(Rayman, 2)
            .Element(SacredRelic, 4, "STORM FUEL")
            .Element(PiercingRegret, 4)
            .Element(AxeOfRegrowth, 2)
            .Element(LawOfTheLand, 2)
            .Element(Adrenaline, 3)
            .Type(Fangwild, 3, "HEAL EVERY TURN")
            .Type(Essence, 12);
    }
}
