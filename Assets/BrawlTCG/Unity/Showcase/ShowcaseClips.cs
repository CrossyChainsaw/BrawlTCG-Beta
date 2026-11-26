using System.Collections.Generic;
using BrawlTCG.Core;

// Pure C# (no UnityEngine): the clips can be checked against the rules engine outside Unity.
namespace BrawlTCG.UnityApp
{
    /// <summary>The trailer clips: staged positions that build up to a big turn.</summary>
    public static class ShowcaseClips
    {
        // Card ids, so the scripts read like the cards.
        const int Essence = 0, Mustafar = 100, Fangwild = 101, Atlantis = 106, SilentGalaxy = 107;
        const int BriarRose = 1000, Deathcap = 1002, Rayman = 1003, Yumiko = 1005, FaerieQueen = 1008;
        const int IronLady = 2000, Heatblast = 2001, Ulgrim = 2002, MoltenKor = 2003, Jhala = 2004, FuryShang = 2006;
        const int Artemis = 3000, Orion = 3001, WuShang = 3002, AuroraBrynn = 3003, AstroCommander = 3004, BreakerShang = 3005;
        const int DarkMage = 4002, Caspian = 5000, Raymesis = 5001, Mastermind = 5002, NinjaSpirit = 5003, DullahanJiro = 5005, KitsuneHattori = 5006;
        const int Minotaur = 6000, FoxSpirit = 6001, TheRock = 6003, SnowmanKor = 7000, AtlanteanAda = 7001;
        const int AxeOfRegrowth = 13000, BuzzAxe = 13001, BoilingPoint = 13002, BlazingFire = 15000, Revolvers = 15001, AquaBlasters = 15002;
        const int SleightOfHand = 17000, Rippers = 17001, LawOfTheLand = 18000, PrimroseMallet = 19000, Chikara = 19001, IceCrusher = 19002;
        const int GalaxyLance = 21000, SacredRelic = 22001, SearingBlade = 23000, StarryScythe = 23002, NecroAura = 23005;
        const int MagmaSpear = 24000, PiercingRegret = 24001, SweetMagi = 24002, ShatteringStar = 24003;
        const int ShadowEdge = 25000, AncestorsFlame = 25003;
        const int BouncyBomb = 500, VialOfCrows = 501, Snowball = 502, SuperSaiyan = 503, Orb = 504, Chest = 506, Heart = 507;
        const int FireNation = 508, WaterTribe = 509, KrabbyPatty = 510, Bubble = 511, CosmicChest = 512, ForgeborneChest = 516;
        const int DarkDuo = 517, PromotionII = 520, Adrenaline = 521, DeathsHour = 522, YmirsBreath = 524, CodyTravis = 525;

        public static IReadOnlyList<ShowcaseClip> All { get; } = new[]
        {
            HourOfDeath(),
            Unchained(),
            Ragnarok(),
            Vanaheim(),
            Trickster(),
        };

        /// <summary>Filler so the decks have a believable thickness.</summary>
        static int[] Filler(int n, params int[] pool)
        {
            var ids = new int[n];
            for (int i = 0; i < n; i++) ids[i] = pool[i % pool.Length];
            return ids;
        }

        static int[] Then(int first, int[] rest)
        {
            var ids = new int[rest.Length + 1];
            ids[0] = first;
            rest.CopyTo(ids, 1);
            return ids;
        }

        // ───────────── 1. Hel vs Thor: Yumiko + Death's Hour, five legends gone, then lethal ─────────────

        static ShowcaseClip HourOfDeath()
        {
            var clip = new ShowcaseClip
            {
                Id = "hour-of-death", Title = "THE HOUR OF DEATH", Subtitle = "HEL  VS  THOR",
                Name0 = "Hel", Name1 = "Thor", Hero = 0, Seed = 11,
                Setup = g =>
                {
                    g.StageHealth(0, 9);
                    g.StageHealth(1, 50);
                    g.StageEssence(0, 10);
                    g.StageEssence(1, 11);

                    g.StageLegend(0, Yumiko, 4, PrimroseMallet);
                    g.StageLegend(0, FoxSpirit, 0, Chikara);
                    g.StageLegend(0, KitsuneHattori, 0, ShadowEdge);
                    g.StageLegend(0, DarkMage, 0, NecroAura);
                    g.StageLegend(0, Raymesis, 0, BuzzAxe);
                    g.StageHand(0, Essence, Adrenaline, VialOfCrows, Snowball, Chest);
                    g.StageDeck(0, Then(DeathsHour, Filler(14, Orb, Caspian, BouncyBomb, ForgeborneChest, NinjaSpirit)));
                    g.StageDiscard(0, Caspian, NinjaSpirit, BouncyBomb, Orb, Orb, Orb, DullahanJiro, Snowball);

                    g.StageLegend(1, AuroraBrynn, 2, SweetMagi);
                    g.StageLegend(1, IronLady, 0, SearingBlade, GalaxyLance);
                    g.StageLegend(1, WuShang, 0, ShatteringStar);
                    g.StageLegend(1, Heatblast, 0, BlazingFire);
                    g.StageHand(1, Essence, AstroCommander, Revolvers, Snowball, CosmicChest, FireNation);
                    g.StageDeck(1, Filler(12, Orion, Artemis, BouncyBomb, MagmaSpear));
                    g.StageDiscard(1, Jhala, FuryShang, Snowball, BouncyBomb, Orion);

                    g.BeginStaged(1, 38);
                },
            };
            return clip
                // Thor presses his advantage.
                .Play(Essence)
                .Play(AstroCommander)
                .PlayOn(Revolvers, AstroCommander)
                .Attack(AstroCommander, "Shot", Raymesis)
                .Attack(AuroraBrynn, "Spear Stab", KitsuneHattori)
                .Attack(Heatblast, "Incinerate", DarkMage)
                .Attack(IronLady, "Scythe Slash", Yumiko)
                .PlayOn(Snowball, FoxSpirit)
                .EndTurn()
                // Hel draws Death's Hour.
                .Play(Essence)
                .Inspect(DeathsHour)
                .Play(DeathsHour, 2.4f)
                .Attack(Yumiko, "Craft 3 Orbs")
                .PlayOn(Orb, AuroraBrynn, pause: 1.25f)
                .PlayOn(Orb, IronLady, pause: 1.25f)
                .PlayOn(Orb, WuShang, pause: 1.25f)
                .Attack(FoxSpirit, "Craft 3 Orbs")
                .PlayOn(Orb, Heatblast, pause: 1.25f)
                .PlayOn(Orb, AstroCommander, pause: 1.6f)
                .Attack(KitsuneHattori, "Slash")
                .Attack(DarkMage, "Scythe Slash")
                .Attack(Raymesis, "Axe Swing")
                .PlayOn(Adrenaline, KitsuneHattori)
                .Attack(KitsuneHattori, "Slash");
        }

        // ───────────── 2. Fenrir vs Tyr: four Super Saiyans on the Minotaur, four Adrenalines ─────────────

        static ShowcaseClip Unchained()
        {
            var clip = new ShowcaseClip
            {
                Id = "unchained", Title = "UNCHAINED", Subtitle = "FENRIR  VS  TYR",
                Name0 = "Fenrir", Name1 = "Tyr", Hero = 0, Seed = 22,
                Setup = g =>
                {
                    g.StageHealth(0, 22);
                    g.StageHealth(1, 50);
                    g.StageEssence(0, 34);
                    g.StageEssence(1, 12);

                    g.StageLegend(0, Minotaur);
                    g.StageHand(0, Essence, SuperSaiyan, Adrenaline, SuperSaiyan, Adrenaline, BuzzAxe, SuperSaiyan, Adrenaline, Adrenaline);
                    g.StageDeck(0, Then(SuperSaiyan, Filler(9, Chikara, TheRock, Adrenaline)));
                    g.StageDiscard(0, TheRock, TheRock, Chikara, 513, Snowball, Chest, 6002, 6002, BuzzAxe);

                    g.StageLegend(1, Artemis, 0, StarryScythe);
                    g.StageLegend(1, Orion, 0, ShatteringStar);
                    g.StageHand(1, TheRock, Snowball, Bubble, WaterTribe, Essence);
                    g.StageDeck(1, Filler(13, SnowmanKor, AtlanteanAda, KrabbyPatty));
                    g.StageDiscard(1, AtlanteanAda, SnowmanKor, KrabbyPatty, Bubble, Snowball, WuShang);

                    g.BeginStaged(1, 31);
                },
            };
            return clip
                // Tyr nearly finishes the wolf.
                .Play(TheRock)
                .PlayOn(Snowball, Minotaur)
                .Attack(Orion, "Spear Stab", Minotaur)
                .Attack(Artemis, "Scythe Slash", Minotaur)
                .EndTurn()
                // One HP left. Fenrir goes all in.
                .Play(Essence)
                .Inspect(Minotaur, seconds: 1.6f)
                .PlayOn(SuperSaiyan, Minotaur, pause: 1.7f)
                .PlayOn(SuperSaiyan, Minotaur, pause: 1.7f)
                .PlayOn(SuperSaiyan, Minotaur, pause: 1.7f)
                .PlayOn(SuperSaiyan, Minotaur, pause: 1.9f)
                .PlayOn(BuzzAxe, Minotaur)
                .Inspect(Minotaur, seconds: 2.6f)
                .Attack(Minotaur, "Axe Swing", TheRock)
                .PlayOn(Adrenaline, Minotaur)
                .Attack(Minotaur, "Axe Swing", Orion)
                .PlayOn(Adrenaline, Minotaur)
                .Attack(Minotaur, "Axe Swing", Artemis)
                .PlayOn(Adrenaline, Minotaur)
                .Attack(Minotaur, "Axe Swing")
                .PlayOn(Adrenaline, Minotaur)
                .Attack(Minotaur, "Axe Swing");
        }

        // ───────────── 3. Odin vs Surtr: turn 89, gold essence, both boards full ─────────────

        static ShowcaseClip Ragnarok()
        {
            var clip = new ShowcaseClip
            {
                Id = "ragnarok", Title = "RAGNARÖK", Subtitle = "ODIN  VS  SURTR  ·  TURN 89",
                Name0 = "Odin", Name1 = "Surtr", Hero = 0, Seed = 33,
                Setup = g =>
                {
                    g.StageHealth(0, 14, 65);
                    g.StageHealth(1, 31, 75);
                    g.StageEssence(0, 52);
                    g.StageEssence(1, 49);
                    g.StageStage(1, Mustafar);

                    g.StageLegend(0, AuroraBrynn, 6, ShatteringStar);
                    g.StageLegend(0, WuShang, 5, SleightOfHand, Rippers);
                    g.StageLegend(0, Artemis, 4, StarryScythe, GalaxyLance);
                    g.StageLegend(0, Orion, 3, GalaxyLance, GalaxyLance);
                    g.StageLegend(0, BreakerShang, 0, ShatteringStar);
                    g.StageHand(0, Essence, SilentGalaxy, PromotionII, CosmicChest, Adrenaline, AstroCommander, Revolvers,
                        AuroraBrynn, ShatteringStar, Adrenaline, Adrenaline, Orb);
                    g.StageDeck(0, Then(Orb, Filler(2, Orb, Snowball)));
                    g.StageDiscard(0, Filler(46, Orion, AstroCommander, Snowball, Adrenaline, 3006, CosmicChest, GalaxyLance, 520, 519, BouncyBomb, 3005));

                    LegendCard iron = g.StageLegend(1, IronLady, 5, SearingBlade, GalaxyLance);
                    g.StageLegend(1, Ulgrim, 4, BoilingPoint);
                    g.StageLegend(1, FuryShang, 2, MagmaSpear);
                    g.StageLegend(1, Jhala, 0, AncestorsFlame);
                    LegendCard heat = g.StageLegend(1, Heatblast, 1, BlazingFire);
                    g.StageBuff(iron, 2, 2, "Fire Nation");
                    g.StageBuff(heat, 2, 2, "Fire Nation");
                    g.StageHand(1, FireNation, BouncyBomb, Essence, 514, MoltenKor, VialOfCrows);
                    g.StageDeck(1, Filler(3, Snowball, 514, Heatblast));
                    g.StageDiscard(1, Filler(51, Jhala, Heatblast, FireNation, Mustafar, 2005, 514, BouncyBomb, 519, 520, MoltenKor, MagmaSpear));

                    g.BeginStaged(1, 88);
                },
            };
            return clip
                // Surtr's fire.
                .Play(Essence)
                .PlayOn(FireNation, IronLady)
                .Attack(Jhala, "Slash", AuroraBrynn)
                .Attack(IronLady, "Meltdown", Artemis)
                .PlayOn(BouncyBomb, BreakerShang)
                .EndTurn()
                // Odin answers: the galaxy goes silent.
                .Play(Essence)
                .Play(SilentGalaxy, 2.4f)
                .Play(PromotionII)
                .Play(CosmicChest)
                .Attack(WuShang, "Gauntlet Dsig")
                .PlayOn(Adrenaline, WuShang)
                .Attack(WuShang, "Gauntlet Dsig")
                .Attack(Orion, "Flamethrower", Ulgrim)
                .Play(AstroCommander)
                .PlayOn(Revolvers, AstroCommander)
                .Attack(AstroCommander, "Shot", IronLady)
                .Play(AuroraBrynn)
                .PlayOn(ShatteringStar, AuroraBrynn)
                .Attack(AuroraBrynn, "Spear Stab")
                .PlayOn(Adrenaline, Orion)
                .Attack(Orion, "Flamethrower")
                .PlayOn(Adrenaline, AstroCommander)
                .Attack(AstroCommander, "Shot");
        }

        // ───────────── 4. Freyja vs Thrym: two Faerie Queens, Fangwild, the nature army ─────────────

        static ShowcaseClip Vanaheim()
        {
            var clip = new ShowcaseClip
            {
                Id = "vanaheim", Title = "BLOOM OF VANAHEIM", Subtitle = "FREYJA  VS  THRYM",
                Name0 = "Freyja", Name1 = "Thrym", Hero = 0, Seed = 44,
                Setup = g =>
                {
                    g.StageHealth(0, 27);
                    g.StageHealth(1, 50);
                    g.StageEssence(0, 15);
                    g.StageEssence(1, 10);
                    g.StageStage(1, Mustafar);

                    g.StageLegend(0, FaerieQueen);
                    g.StageLegend(0, Deathcap);
                    g.StageLegend(0, BriarRose);
                    g.StageLegend(0, Rayman);
                    g.StageHand(0, Essence, Fangwild, SacredRelic, PiercingRegret, SacredRelic, AxeOfRegrowth, SacredRelic, LawOfTheLand, Adrenaline);
                    g.StageDeck(0, Then(FaerieQueen, Filler(11, 1001, 1006, 16000, 1004)));
                    g.StageDiscard(0, 1006, 1006, 1001, 16000, Adrenaline, 1007);

                    g.StageLegend(1, TheRock, 0, Chikara);
                    g.StageLegend(1, MoltenKor);
                    g.StageLegend(1, SnowmanKor, 0, IceCrusher);
                    g.StageLegend(1, Raymesis, 0, BuzzAxe);
                    g.StageLegend(1, AuroraBrynn);
                    g.StageHand(1, Snowball, Bubble, WaterTribe, Essence, YmirsBreath);
                    g.StageDeck(1, Filler(14, TheRock, SnowmanKor, Snowball));
                    g.StageDiscard(1, AtlanteanAda, Snowball, Bubble, 2005);

                    g.BeginStaged(1, 24);
                },
            };
            return clip
                // The frost giants close in.
                .Play(Essence)
                .Attack(SnowmanKor, "Freeze", FaerieQueen)
                .Attack(TheRock, "Hammer Swing", BriarRose)
                .PlayOn(Snowball, Rayman)
                .EndTurn()
                // Freyja's spring.
                .Play(Essence)
                .Play(Fangwild, 2.4f)
                .Play(FaerieQueen)
                .PlayOn(SacredRelic, Deathcap, pause: 1.3f)
                .PlayOn(SacredRelic, Deathcap, pause: 1.3f)
                .PlayOn(SacredRelic, Deathcap, pause: 1.3f)
                .PlayOn(PiercingRegret, BriarRose, pause: 1.3f)
                .PlayOn(AxeOfRegrowth, Rayman, pause: 1.3f)
                .PlayOn(LawOfTheLand, FaerieQueen, 1, pause: 1.3f)
                .Inspect(Deathcap, seconds: 2.4f)
                .Attack(Deathcap, "Storm", pause: 3f)
                .Attack(BriarRose, "Spear Stab")
                .Attack(Rayman, "Axe Swing")
                .PlayOn(Adrenaline, Rayman)
                .Attack(Rayman, "Axe Swing");
        }

        // ───────────── 5. Loki vs Heimdall: Shadow tricks, crafted cards everywhere ─────────────

        static ShowcaseClip Trickster()
        {
            var clip = new ShowcaseClip
            {
                Id = "trickster", Title = "THE TRICKSTER", Subtitle = "LOKI  VS  HEIMDALL",
                Name0 = "Loki", Name1 = "Heimdall", Hero = 0, Seed = 55,
                Setup = g =>
                {
                    g.StageHealth(0, 26);
                    g.StageHealth(1, 44);
                    g.StageEssence(0, 26);
                    g.StageEssence(1, 9);
                    g.StageStage(1, Atlantis);

                    g.StageLegend(0, Caspian, 0, SleightOfHand);
                    g.StageLegend(0, DullahanJiro, 0, ShadowEdge);
                    g.StageHand(0, Essence, ForgeborneChest, DarkDuo, CodyTravis, BouncyBomb, NecroAura);
                    g.StageDeck(0, Then(Mastermind, Filler(15, NinjaSpirit, Orb, Caspian, BouncyBomb)));
                    g.StageDiscard(0, NinjaSpirit, NinjaSpirit, Orb, BouncyBomb, 523, KitsuneHattori);

                    g.StageLegend(1, SnowmanKor, 0, IceCrusher);
                    g.StageLegend(1, AtlanteanAda, 0, AquaBlasters);
                    g.StageLegend(1, TheRock, 0, IceCrusher);
                    g.StageHand(1, WaterTribe, KrabbyPatty, Essence, Bubble);
                    g.StageDeck(1, Filler(15, SnowmanKor, 515, 20001));
                    g.StageDiscard(1, 515, Snowball, 7001, 20001);

                    g.BeginStaged(1, 19);
                },
            };
            return clip
                // Heimdall stands guard.
                .Play(Essence)
                .PlayOn(WaterTribe, SnowmanKor)
                .Attack(SnowmanKor, "Freeze", Caspian)
                .Attack(TheRock, "Hammer Swing", DullahanJiro)
                .EndTurn()
                // Loki's turn: nothing is what it seems.
                .Play(Essence)
                .Play(Mastermind, 2.6f)
                .Attack(DullahanJiro, "Summon Ninja Spirit")
                .Play(ForgeborneChest, 2.2f)
                .Play(DarkDuo, 2.2f)
                .PlayOn(CodyTravis, TheRock)
                .PlayOn(BouncyBomb, AtlanteanAda)
                .Attack(NinjaSpirit, "Phantom Slash", SnowmanKor)
                .PlayOn(NecroAura, Mastermind)
                .Attack(Mastermind, "Scythe Slash", SnowmanKor)
                .PlayPicked(c => c is LegendCard && c.CraftedBy == "Dark Duo", pause: 2.4f);
        }
    }
}
