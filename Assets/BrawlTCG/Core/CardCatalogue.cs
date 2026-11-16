using System;
using System.Collections.Generic;
using System.Linq;
using AC = BrawlTCG.Core.AttackCatalogue;
using EC = BrawlTCG.Core.EffectCatalogue;

namespace BrawlTCG.Core
{
    public static class CardCatalogue
    {
        static LegendCard Legend(int id, string name, int cost, Element element, string art, int power, int health,
            Weapon primary, Weapon secondary, Attack[] attacks, Effect whenPlayed = null, Effect whileInPlay = null)
            => new LegendCard(id, name, cost, element, art, power, health, primary, secondary, attacks,
                whenPlayedEffect: whenPlayed, whileInPlayEffect: whileInPlay);

        static Attack[] A(params Attack[] attacks) => attacks;

        static WeaponCard W(int id, string name, Element element, string art, Weapon weapon)
            => new WeaponCard(id, name, 1, element, art, weapon);

        /// <summary>Card prototypes in catalogue order. Never handed out directly; <see cref="Create"/> clones them.</summary>
        static readonly Card[] Prototypes =
        {
            // Essence
            new EssenceCard(0, "Essence", 0, Element.Magic, "Essence", EC.Essence),

            // Stages
            new StageCard(100, "Mustafar", 2, Element.Fire, "Mustafar", startTurnEffect: EC.Mustafar),
            new StageCard(101, "Fangwild", 3, Element.Nature, "Fangwild", startTurnEffect: EC.Fangwild),
            new StageCard(102, "Matrix", 1, Element.Cosmic, "Matrix"),
            new StageCard(103, "The Workshop", 2, Element.Fire, "TheWorkshop", startTurnEffect: EC.Workshop_StartTurn, whenPlayedEffect: EC.Workshop_WhenPlayed, whenDiscardedEffect: EC.Workshop_Discarded),
            new StageCard(104, "Evil Hideout", 3, Element.Fire, "Evil_Hideout", whenPlayedEffect: EC.EvilHideout_WhenPlayed, whileInPlayEffect: EC.EvilHideout_WhileInPlay, whenDiscardedEffect: EC.EvilHideout_WhenDiscarded),
            new StageCard(105, "Space Time", 1, Element.Cosmic, "SpaceTime", startTurnEffect: EC.SpaceTime),
            new StageCard(106, "Atlantis", 3, Element.Arctic, "Atlantis", startTurnEffect: EC.Atlantis_StartTurn, whenPlayedEffect: EC.Atlantis_WhenPlayed, whileInPlayEffect: EC.Atlantis_WhileInPlay, whenDiscardedEffect: EC.Atlantis_WhenDiscarded),
            new StageCard(107, "Silent Galaxy", 5, Element.Cosmic, "SilentGalaxy", startTurnEffect: EC.SilentGalaxy_StartTurn, whenPlayedEffect: EC.SilentGalaxy_WhenPlayed, whileInPlayEffect: EC.SilentGalaxy_WhilePlay, whenDiscardedEffect: EC.SilentGalaxy_WhenDiscarded),

            // Nature legends                                              power hp
            Legend(1000, "Briar Rose", 3, Element.Nature, "BriarRose", 6, 9, Weapon.Spear, Weapon.Greatsword, A(AC.Spear_Stab, AC.Any_BlowAKiss)),
            Legend(1001, "Forest Guardian", 2, Element.Nature, "ElvenhollowMagyar", 1, 13, Weapon.Hammer, Weapon.Greatsword, A(AC.Greatsword_Swing, AC.Greatsword_String)),
            Legend(1002, "Deathcap", 5, Element.Nature, "DeathCap", 10, 5, Weapon.Spear, Weapon.Orb, A(AC.Spear_Stab, AC.Orb_Throw, AC.DeathCap_Storm)),
            Legend(1003, "Rayman", 3, Element.Nature, "Rayman", 7, 7, Weapon.Gauntlets, Weapon.Axe, A(AC.Axe_Swing, AC.Gauntlets_Punch, AC.Gauntlets_PowerPunch)),
            Legend(1004, "Kor", 2, Element.Nature, "Kor", 0, 16, Weapon.Gauntlets, Weapon.Hammer, A()),
            Legend(1005, "Yumiko", 4, Element.Nature, "Yumiko", 3, 15, Weapon.Bow, Weapon.Hammer, A(AC.Hammer_Swing, AC.Hammer_Gimp, AC.Yumiko_GrabOrbs)),
            Legend(1006, "Forest Spirit", 1, Element.Nature, "Forest_Spirit", 3, 1, Weapon.Orb, Weapon.Gauntlets, A(AC.ForestSpirit_Explode, AC.ForestSpirit_Heal)),
            Legend(1007, "Demon Bride", 3, Element.Nature, "Demon_Bride", 4, 9, Weapon.Sword, Weapon.Spear, A(AC.Sword_Slash, AC.Any_BlowAKiss)),
            Legend(1008, "Faerie Queen", 3, Element.Nature, "RealQueen", 1, 7, Weapon.Spear, Weapon.Greatsword, A(AC.Spear_Stab, AC.Any_BlowAKiss), whileInPlay: EC.BoostAllNatureLegendsStats_WhileInPlay),
            // Fire legends
            Legend(2000, "Iron Lady", 5, Element.Fire, "IronLady", 7, 15, Weapon.RocketLance, Weapon.Scythe, A(AC.Scythe_Slash, AC.Scythe_Gimp, AC.Lance_Flamethrower, AC.Artemis_IronLady_MeltDown)),
            Legend(2001, "Heatblast", 4, Element.Fire, "Heatblast", 9, 6, Weapon.Blasters, Weapon.Spear, A(AC.Spear_Stab, AC.Heatblast_Burn)),
            Legend(2002, "Ulgrim", 5, Element.Fire, "Ulgrim", 5, 16, Weapon.Axe, Weapon.RocketLance, A(AC.Axe_Swing, AC.Lance_Flamethrower, AC.Any_CraftFireCard), whenPlayed: EC.GenerateAndPlayWorkshop),
            Legend(2003, "Molten Kor", 3, Element.Fire, "MoltenKor", 0, 16, Weapon.Gauntlets, Weapon.Hammer, A(), whenPlayed: EC.GenerateAndPlayMustafar),
            Legend(2004, "Jhala", 3, Element.Fire, "Jhala", 11, 1, Weapon.Axe, Weapon.Sword, A(AC.Sword_Slash, AC.Axe_Swing)),
            Legend(2005, "Hothead Jiro", 2, Element.Fire, "Hothead_Jiro", 4, 4, Weapon.Sword, Weapon.Scythe, A(AC.Sword_Slash, AC.Scythe_Slash)),
            Legend(2006, "Fury Shang", 3, Element.Fire, "FuryShang", 6, 6, Weapon.Gauntlets, Weapon.Spear, A(AC.Spear_Stab, AC.Gauntlets_Punch), whileInPlay: EC.BoostFireLegendStats_WhileInPlay),
            Legend(2007, "Seven", 2, Element.Fire, "Seven", 4, 4, Weapon.Spear, Weapon.Cannon, A(AC.Cannon_Blast, AC.Seven_CraftWeapons)),
            // Cosmic legends
            Legend(3000, "Artemis", 3, Element.Cosmic, "Artemis", 5, 12, Weapon.RocketLance, Weapon.Scythe, A(AC.Scythe_Slash, AC.Scythe_Gimp, AC.Lance_Flamethrower)),
            Legend(3001, "Orion", 3, Element.Cosmic, "Orion", 4, 12, Weapon.RocketLance, Weapon.Spear, A(AC.Spear_Stab, AC.Lance_Flamethrower)),
            Legend(3002, "Wu Shang", 4, Element.Cosmic, "Spyrox_WuShang", 7, 13, Weapon.Gauntlets, Weapon.Spear, A(AC.Spear_Stab, AC.Gauntlets_Punch, AC.WuShang_DownSig)),
            Legend(3003, "Aurora Brynn", 5, Element.Cosmic, "AuroraBrynn", 10, 16, Weapon.Axe, Weapon.Spear, A(AC.Axe_Swing, AC.Spear_Stab)),
            Legend(3004, "Astro Commander", 4, Element.Cosmic, "AstralCoreAda", 14, 3, Weapon.Blasters, Weapon.Spear, A(AC.Blaster_Shot)),
            Legend(3005, "Breaker Shang", 2, Element.Cosmic, "Breaker_Shang", 5, 4, Weapon.Gauntlets, Weapon.Spear, A(AC.Spear_Stab, AC.Gauntlets_Punch, AC.Any_CraftCosmicCard)),
            Legend(3006, "Witch Scarlet", 2, Element.Cosmic, "WitchScarlet", 1, 6, Weapon.Hammer, Weapon.RocketLance, A(AC.Hammer_Swing, AC.Lance_Poke, AC.Any_GenerateAndPlayMatrix)),
            // Magic legends
            Legend(4000, "Arcadia", 3, Element.Magic, "FaerieQueen", 6, 8, Weapon.Spear, Weapon.Greatsword, A(AC.Spear_Stab, AC.Greatsword_Swing, AC.Greatsword_String, AC.Arcadia_PinkRoses)),
            Legend(4001, "Enchantress", 4, Element.Magic, "Enchantress", 0, 15, Weapon.Scythe, Weapon.Orb, A(AC.Enchantress_EnchantHealth, AC.Enchantress_EnchantPower, AC.Enchantress_CurseHealth, AC.Enchantress_CursePower)),
            Legend(4003, "Plague Knight", 4, Element.Magic, "PlagueKnight", 1, 14, Weapon.Gauntlets, Weapon.Katars, A(AC.PlagueKnight_GrabHealingPotion, AC.Any_CraftBattleCard)),
            Legend(4004, "Fait", 3, Element.Magic, "Fait", 6, 6, Weapon.Scythe, Weapon.Orb, A(AC.Scythe_Slash, AC.Orb_Swing, AC.Any_BurnForThreeCard)),
            // Shadow legends
            Legend(4002, "Dark Mage", 5, Element.Shadow, "DarkMage", 11, 12, Weapon.Scythe, Weapon.Orb, A(AC.Scythe_Slash)),
            Legend(5000, "Caspian", 2, Element.Shadow, "MasterThief", 4, 4, Weapon.Gauntlets, Weapon.Katars, A(AC.Any_BurnForThreeCard, AC.MasterThief_GrabBomb)),
            Legend(5001, "Raymesis", 5, Element.Shadow, "Raymesis", 9, 16, Weapon.Gauntlets, Weapon.Axe, A(AC.Axe_Swing)),
            Legend(5002, "Mastermind", 4, Element.Shadow, "Loki", 5, 10, Weapon.Scythe, Weapon.Orb, A(AC.Scythe_Slash), whenPlayed: EC.GenerateAndPlayMatrix),
            Legend(5003, "Ninja Spirit", 2, Element.Shadow, "Ninja_Spirit", 3, 1, Weapon.Sword, Weapon.Scythe, A(AC.NinjaSpirit_PhantomSlash)),
            Legend(5004, "Hellshot Hattori", 3, Element.Shadow, "Hellshot_Hattori", 8, 6, Weapon.Sword, Weapon.Blasters, A(AC.Sword_Slash, AC.Blaster_Shot, AC.Blaster_DoubleShot)),
            Legend(5005, "Dullahan Jiro", 3, Element.Shadow, "DullahanJiro", 5, 7, Weapon.Sword, Weapon.Scythe, A(AC.Sword_Slash, AC.Scythe_Slash, AC.Jiro_SpawnAndPlayNinjaSpirit)),
            Legend(5006, "Kitsune Hattori", 6, Element.Shadow, "Kitsune", 15, 13, Weapon.Sword, Weapon.Spear, A(AC.Sword_Slash, AC.Spear_Stab)),
            // Wild legends
            Legend(6000, "The Minotaur", 5, Element.Wild, "Teros", 10, 15, Weapon.Axe, Weapon.Hammer, A(AC.Axe_Swing, AC.Hammer_Swing)),
            Legend(6001, "Fox Spirit", 2, Element.Wild, "Fox_Spirit", 1, 6, Weapon.Bow, Weapon.Hammer, A(AC.Hammer_Swing, AC.Yumiko_GrabOrbs)),
            Legend(6002, "Dander", 1, Element.Wild, "Dander", 6, 2, Weapon.Gauntlets, Weapon.Katars, A(AC.Katar_Slash)),
            Legend(6003, "The Rock", 5, Element.Wild, "TheRock", 4, 20, Weapon.Hammer, Weapon.Katars, A(AC.Hammer_Swing, AC.Katar_Slash)),
            // Arctic legends
            Legend(7000, "Snowman Kor", 4, Element.Arctic, "SnowmanKor", 0, 16, Weapon.Gauntlets, Weapon.Hammer, A(AC.Any_Freeze)),
            Legend(7001, "Atlantean Ada", 2, Element.Arctic, "Atlantean_Ada", 2, 5, Weapon.Blasters, Weapon.Spear, A(AC.Blaster_Shot, AC.Spear_Stab, AC.Ada_SpawnAndPlayAtlantis)),

            // Weapons
            W(13000, "Axe of Regrowth", Element.Nature, "Axe_of_Regrowth", Weapon.Axe),
            W(13001, "Buzz Axe", Element.Wild, "BuzzAxe", Weapon.Axe),
            W(13002, "Boiling Point", Element.Fire, "BoilingPoint", Weapon.Axe),
            W(14000, "Molten Boots", Element.Fire, "MoltenBoots", Weapon.BattleBoots),
            W(15000, "Blazing Fire", Element.Fire, "BlazingFire", Weapon.Blasters),
            W(15001, "Revolvers", Element.Shadow, "Revolvers", Weapon.Blasters),
            W(15002, "Aqua Blasters", Element.Arctic, "Aqua_Blasters", Weapon.Blasters),
            W(15003, "Splish Splash", Element.Arctic, "Splish_Splash", Weapon.Blasters),
            W(16000, "Sakura Strike", Element.Nature, "Sakura_Strike", Weapon.Bow),
            W(26000, "Molten Cannon", Element.Fire, "MoltenCannon", Weapon.Cannon),
            W(17000, "Sleight of Hand", Element.Shadow, "Sleight_of_Hand", Weapon.Gauntlets),
            W(17001, "Rippers", Element.Wild, "MordexGaunts", Weapon.Gauntlets),
            W(18000, "Law of the Land", Element.Nature, "LawOfTheLand", Weapon.Greatsword),
            W(19000, "Primrose Mallet", Element.Nature, "Primrose_Mallet", Weapon.Hammer),
            W(19001, "Chikara", Element.Wild, "Chikara", Weapon.Hammer),
            W(19002, "Ice Crusher", Element.Arctic, "IceCrusher", Weapon.Hammer),
            W(20000, "Nightmare Claws", Element.Wild, "Nightmare_Claws", Weapon.Katars),
            W(20001, "Winter Daggers", Element.Arctic, "Winter_Daggers", Weapon.Katars),
            W(21000, "Galaxy Lance", Element.Cosmic, "GalaxyLance", Weapon.RocketLance),
            W(22000, "Remnant of Fate", Element.Magic, "RemnantOfFate", Weapon.Orb),
            W(22001, "Sacred Relic", Element.Nature, "SacredRelic", Weapon.Orb),
            W(22002, "Scrying Glass", Element.Cosmic, "ScryingGlass", Weapon.Orb),
            W(23000, "Searing Blade", Element.Fire, "SearingBlade", Weapon.Scythe),
            W(23001, "Shooting Star", Element.Cosmic, "ShootingStar", Weapon.Scythe),
            W(23002, "Starry Scythe", Element.Cosmic, "StarryScythe", Weapon.Scythe),
            W(23003, "Candlelit Scythe", Element.Fire, "Candlelit_Scythe", Weapon.Scythe),
            W(23004, "Amethyst Scythe", Element.Magic, "Constellation_Carver", Weapon.Scythe),
            W(23005, "Necro Aura", Element.Shadow, "NecromanticAuraBlack", Weapon.Scythe),
            W(24000, "Magma Spear", Element.Fire, "MagmaSpear", Weapon.Spear),
            W(24001, "Piercing Regret", Element.Nature, "PiercingRegret", Weapon.Spear),
            W(24002, "Sweet Magi", Element.Cosmic, "SweetMagi", Weapon.Spear),
            W(24003, "Shattering Star", Element.Cosmic, "ShatteringStar", Weapon.Spear),
            W(24004, "Glacier's Grace", Element.Arctic, "GlaciersGrace", Weapon.Spear),
            W(24005, "Hand Saw", Element.Wild, "HandSaw", Weapon.Spear),
            W(24006, "Holy Covenant", Element.Magic, "HolyCovenant", Weapon.Spear),
            W(25000, "Shadow Edge", Element.Shadow, "ShadowEdge", Weapon.Sword),
            W(25001, "Mahou Shoujo", Element.Cosmic, "MahouShoujo", Weapon.Sword),
            W(25002, "Frozen Edge", Element.Arctic, "FrozenEdge", Weapon.Sword),
            W(25003, "Ancestor's Flame", Element.Fire, "Ancestors_Flame", Weapon.Sword),

            // Battle cards
            new BattleCard(500, "Bouncy Bomb", 3, "When Played: Deals direct damage 7", Element.Shadow, "BouncyBomb", oneTimeUse: true, stackable: false, friendlyFire: false, whenPlayedEffect: EC.BattleCardDirectDamageWhenPlayed, damage: 7),
            new BattleCard(501, "Vial of Crows", 1, "When Played: Heals legend by 6", Element.Shadow, "Vial_of_Crows", true, false, true, EC.BattleCardHealWhenPlayed, healthModifier: 6),
            new BattleCard(502, "Snowball", 2, "When Played: Deals direct damage 3", Element.Arctic, "Snowball", true, false, false, EC.BattleCardDirectDamageWhenPlayed, damage: 3),
            new BattleCard(503, "Super Saiyan", 6, "When Played: Boosts legend stats +5/+5", Element.Wild, "SuperSaiyan", false, true, true, EC.BoostHealthAndPower, powerModifier: 5, healthModifier: 5),
            new BattleCard(504, "Orb", 0, "When Played: Deals direct damage 1", Element.Magic, "Orb", true, false, false, EC.BattleCardDirectDamageWhenPlayed, damage: 1),
            new BattleCard(505, "Xull's Fury", 3, "When Played: Boosts legend stats +5 Attack", Element.Fire, "XullsFury", false, true, true, EC.BoostHealthAndPower, powerModifier: 5, healthModifier: 0),
            new BattleCard(506, "Chest", 1, "When Played: Craft 3 random cards", Element.Magic, "CardChest", true, false, true, EC.CardChest, targetRequired: false),
            new BattleCard(507, "<3", 2, "When Played: Boosts legend stats +5 HP", Element.Magic, "Avatar_Heart", true, false, true, EC.BoostHealthAndPower, powerModifier: 0, healthModifier: 5),
            new BattleCard(508, "Fire Nation", 2, "When Played: Boosts all your fire legend stats +2/+2", Element.Fire, "Avatar_Fire", true, false, true, EC.BoostHealthAndPowerAllYourLegends, powerModifier: 2, healthModifier: 2, targetElements: new[] { Element.Fire }, multiTarget: true),
            new BattleCard(509, "Water Tribe", 2, "When Played: Boosts all your Arctic legend stats +2/+2", Element.Arctic, "Avatar_Water", true, false, true, EC.BoostHealthAndPowerAllYourLegends, powerModifier: 2, healthModifier: 2, targetElements: new[] { Element.Arctic }, multiTarget: true),
            new BattleCard(510, "Krabby Patty", 2, "When Played: Fully heals legend", Element.Arctic, "Avatar_Krabby_Patty", true, false, true, EC.BattleCardHealWhenPlayed, healthModifier: 1000),
            new BattleCard(511, "Bubble", 1, "When Played: Tap a card", Element.Arctic, "Bubble", oneTimeUse: true, stackable: false, friendlyFire: false, whenPlayedEffect: EC.Bubble),
            new BattleCard(512, "Cosmic Chest", 3, "When Played: Craft 3 random Cosmic cards", Element.Cosmic, "Cosmic_Chest", true, false, true, EC.CosmicChest, targetRequired: false),
            new BattleCard(513, "Wild Chest", 3, "When Played: Craft 3 random Wild cards", Element.Wild, "Wild_Chest", true, false, true, EC.WildChest, targetRequired: false),
            new BattleCard(514, "Dragon's Chest", 3, "When Played: Craft 3 random Fire cards", Element.Fire, "Dragon_Chest", true, false, true, EC.DragonChest, targetRequired: false),
            new BattleCard(515, "Sunken Chest", 3, "When Played: Craft 3 random Arctic cards", Element.Arctic, "Sunken_Chest", true, false, true, EC.SunkenChest, targetRequired: false),
            new BattleCard(516, "Forgeborne Chest", 3, "When Played: Craft 3 random Shadow cards", Element.Shadow, "Shadow_Chest", true, false, true, EC.ShadowChest, targetRequired: false),
            new BattleCard(517, "Dark Duo", 4, "When Played: Craft 2 random Shadow legends", Element.Shadow, "DarkDuo", true, false, true, EC.DarkDuo, targetRequired: false),
            new BattleCard(518, "Witch Party", 6, "When Played: Craft Fait, Witch Scarlet, Amethyst Scythe and Galaxy Lance", Element.Magic, "WitchParty", true, false, true, EC.WitchParty, targetRequired: false),
            new BattleCard(519, "Promotion I", 5, "When Played: Player Max health goes up by 5", Element.Magic, "Avatar_CollectorsPackI", true, false, false, EC.PromotionI, targetRequired: false),
            new BattleCard(520, "Promotion II", 10, "When Played: Player Max health goes up by 10", Element.Magic, "Avatar_CollectorsPack", true, false, false, EC.PromotionII, targetRequired: false),
            new BattleCard(521, "Adrenaline", 2, "When Played: Legend can attack again", Element.Nature, "Adrenaline", true, false, true, EC.Adrenaline),
            new BattleCard(522, "Death's Hour", 7, "When Played: Every legend's HP becomes 1", Element.Shadow, "DeathsHour", true, false, false, EC.DeathsHour, targetRequired: false),
            new BattleCard(523, "Cursed Kunai", 3, "When Played: Craft a random Katars legend and a random Katars weapon", Element.Shadow, "CursedKunai", true, false, false, EC.CursedKunai, targetRequired: false),
            new BattleCard(524, "Ymir's Breath", 5, "When Played: Tap all opposing legends", Element.Arctic, "YmirsBreath", true, false, false, EC.YmirsBreath, targetRequired: false),
            new BattleCard(525, "Cody Travis", 5, "When Played: A legend's power becomes 0", Element.Wild, "finger", oneTimeUse: true, stackable: false, friendlyFire: false, whenPlayedEffect: EC.The_Finger),
            new BattleCard(526, "Scythe Forever", 3, "When Played: Craft 3 random Scythe weapons", Element.Cosmic, "sandstorm", true, false, false, EC.ScytheForever, targetRequired: false),
            new BattleCard(527, "Did Shaw Win Yet?", 3, "When Played: Craft 3x The Rock", Element.Wild, "shaw", true, false, false, EC.DidShawWinYet, targetRequired: false),
        };

        static readonly Dictionary<int, Card> ById = BuildIndex();

        static Dictionary<int, Card> BuildIndex()
        {
            var index = new Dictionary<int, Card>();
            foreach (Card card in Prototypes)
            {
                if (index.ContainsKey(card.Id))
                    throw new InvalidOperationException($"Duplicate card id {card.Id}");
                index.Add(card.Id, card);
            }
            return index;
        }

        /// <summary>Read-only prototypes, for browsing (deck builder). Do not mutate.</summary>
        public static IReadOnlyList<Card> All => Prototypes;

        public static bool Exists(int id) => ById.ContainsKey(id);

        public static Card Get(int id)
        {
            if (!ById.TryGetValue(id, out Card card))
                throw new KeyNotFoundException($"Card with ID {id} not found.");
            return card;
        }

        /// <summary>A fresh, playable copy of the card.</summary>
        public static Card Create(int id) => Get(id).Clone();

        public static Card CreateRandom(Random rng, Element? element = null, CardType? type = null)
        {
            List<Card> pool = Prototypes
                .Where(c => (element == null || c.Element == element) && (type == null || c.Type == type))
                .ToList();
            if (pool.Count == 0)
                throw new InvalidOperationException($"No cards for element '{element?.ToString() ?? "Any"}' and type '{type?.ToString() ?? "Any"}'.");
            return pool[rng.Next(pool.Count)].Clone();
        }

        /// <summary>Picks a random weapon type (not Any), then a random weapon card of that type.</summary>
        public static Card CreateRandomWeapon(Random rng)
        {
            Weapon[] types = ((Weapon[])Enum.GetValues(typeof(Weapon))).Where(w => w != Weapon.Any).ToArray();
            return CreateRandomWeapon(rng, types[rng.Next(types.Length)]);
        }

        public static Card CreateRandomWeapon(Random rng, Weapon weapon)
        {
            List<WeaponCard> pool = Prototypes.OfType<WeaponCard>().Where(w => w.Weapon == weapon).ToList();
            if (pool.Count == 0)
                throw new InvalidOperationException($"No cards available for weapon type {weapon}.");
            return pool[rng.Next(pool.Count)].Clone();
        }

        public static Card CreateRandomLegendWithWeapon(Random rng, Weapon weapon)
        {
            List<LegendCard> pool = Prototypes.OfType<LegendCard>().Where(l => l.HasWeapon(weapon)).ToList();
            if (pool.Count == 0)
                throw new InvalidOperationException($"No legends available with the weapon {weapon}.");
            return pool[rng.Next(pool.Count)].Clone();
        }
    }
}
