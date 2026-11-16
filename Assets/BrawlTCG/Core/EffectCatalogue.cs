using System;
using System.Collections.Generic;
using System.Linq;

namespace BrawlTCG.Core
{
    public static class EffectCatalogue
    {
        // ───────────── Start turn (stages) ─────────────
        // Stage start-turn effects receive the ACTIVE player's legends as target, so each player's legends are
        // affected once per round (at the start of their own turn).

        const int MustafarDamage = 2;
        public static readonly Effect Mustafar = new Effect(
            $"All non-Fire Legends lose {MustafarDamage} health.",
            (target, card, game, played) => StartTurnDamage(target, MustafarDamage, Element.Fire));

        const int FangwildHeal = 2;
        public static readonly Effect Fangwild = new Effect(
            $"All Magic and Nature Legends will gain {FangwildHeal} health.",
            (target, card, game, played) => StartTurnHeal(target, FangwildHeal, Element.Nature, Element.Magic));

        public static readonly Effect Workshop_StartTurn = new Effect(
            "Instead of drawing a card, craft a random card",
            (target, card, game, played) =>
            {
                game.GenerateRandomCards(1);
                game.DrawCardStartTurn = false;
            });

        // Turns drawing off as soon as the Workshop enters play, so the very next turn already gets
        // only the random card (instead of the normal draw plus the random card).
        public static readonly Effect Workshop_WhenPlayed = new Effect(
            "N/A",
            (target, card, game, played) => game.DrawCardStartTurn = false);

        public static readonly Effect Workshop_Discarded = new Effect(
            "N/A",
            (target, card, game, played) => game.DrawCardStartTurn = true);

        const int SpaceTimeExtraDrawnCards = 1;
        public static readonly Effect SpaceTime = new Effect(
            $"Both players draw {SpaceTimeExtraDrawnCards} additional card",
            (target, card, game, played) =>
            {
                game.DrawCards(game.ActivePlayer, SpaceTimeExtraDrawnCards, startTurn: true);
                game.DrawCards(game.InactivePlayer, SpaceTimeExtraDrawnCards, startTurn: true);
            });

        /// <summary>Flavour only: essence is recalculated from the number of Essence cards each turn.</summary>
        public static readonly Effect Essence = new Effect(
            "Gain 1 Essence",
            (target, card, game, played) => { if (target is Player p) p.GainEssence(1); });

        // ───────────── Player ─────────────

        public static readonly Effect PromotionI = new Effect(
            "Boost player max HP by 5",
            (target, card, game, played) => game.ActivePlayer.BoostMaxHealth(5));

        public static readonly Effect PromotionII = new Effect(
            "Boost player max HP by 10",
            (target, card, game, played) => game.ActivePlayer.BoostMaxHealth(10));

        // ───────────── Legend while-in-play triggers ─────────────

        public static readonly Effect BoostFireLegendStats_WhileInPlay = new Effect(
            "When you play a Fire legend, that Fire legend gets +2/+2",
            (target, card, game, played) =>
            {
                if (played is LegendCard legend && played != card && card.Owner == played.Owner && legend.Element == Element.Fire)
                {
                    legend.ModifyStat(Stat.Power, 2);
                    legend.ModifyStat(Stat.Health, 2);
                }
            });

        public static readonly Effect BoostAllNatureLegendsStats_WhileInPlay = new Effect(
            "When you play a Nature card, all nature legends get +1/+1",
            (target, card, game, played) =>
            {
                if (played.Element != Element.Nature || played == card || card.Owner != played.Owner) return;
                foreach (LegendCard legend in game.ActivePlayer.PlayingField.ToList())
                {
                    if (legend.Element == Element.Nature)
                    {
                        legend.ModifyStat(Stat.Power, 1);
                        legend.ModifyStat(Stat.Health, 1);
                    }
                }
            });

        // ───────────── Evil Hideout ─────────────

        static readonly Element[] EvilHideoutElements = { Element.Fire, Element.Wild, Element.Shadow };

        public static readonly Effect EvilHideout_WhenPlayed = new Effect(
            "N/A",
            (target, card, game, played) => ModifyStatOfAllLegends(game, EvilHideoutElements, Stat.Power, 3));

        public static readonly Effect EvilHideout_WhileInPlay = new Effect(
            "Shadow, Wild and Fire Legends get +3 Power",
            (target, card, game, played) =>
            {
                if (target is LegendCard legend && EvilHideoutElements.Contains(legend.Element))
                    legend.ModifyStat(Stat.Power, 3);
            });

        public static readonly Effect EvilHideout_WhenDiscarded = new Effect(
            "N/A",
            (target, card, game, played) => ModifyStatOfAllLegends(game, EvilHideoutElements, Stat.Power, -3));

        // ───────────── Silent Galaxy ─────────────

        public static readonly Effect SilentGalaxy_WhenPlayed = new Effect(
            "N/A",
            (target, card, game, played) => DisableNonCosmicAttacks(game));

        public static readonly Effect SilentGalaxy_WhilePlay = new Effect(
            "Only Cosmic legends can deal damage",
            (target, card, game, played) =>
            {
                if (target is LegendCard legend && legend.Element != Element.Cosmic)
                    legend.CanAttack = false;
            });

        public static readonly Effect SilentGalaxy_WhenDiscarded = new Effect(
            "N/A",
            (target, card, game, played) =>
            {
                foreach (LegendCard legend in game.GetAllLegendsOnPlayingField())
                    legend.CanAttack = true;
            });

        public static readonly Effect SilentGalaxy_StartTurn = new Effect(
            "N/A",
            (target, card, game, played) => DisableNonCosmicAttacks(game));

        static void DisableNonCosmicAttacks(Game game)
        {
            foreach (LegendCard legend in game.GetAllLegendsOnPlayingField())
            {
                if (legend.Element != Element.Cosmic)
                    legend.CanAttack = false;
            }
        }

        // ───────────── Atlantis ─────────────

        static readonly Element[] NonArctic = ((Element[])Enum.GetValues(typeof(Element))).Where(e => e != Element.Arctic).ToArray();
        const int AtlantisDamage = 1;

        public static readonly Effect Atlantis_WhenPlayed = new Effect(
            "N/A",
            (target, card, game, played) => ModifyStatOfAllLegends(game, NonArctic, Stat.Power, -2));

        public static readonly Effect Atlantis_WhileInPlay = new Effect(
            "Non-Arctic legends do 2 damage less",
            (target, card, game, played) =>
            {
                if (target is LegendCard legend && legend.Element != Element.Arctic)
                    legend.ModifyStat(Stat.Power, -2);
            });

        public static readonly Effect Atlantis_WhenDiscarded = new Effect(
            "N/A",
            (target, card, game, played) => ModifyStatOfAllLegends(game, NonArctic, Stat.Power, 2));

        public static readonly Effect Atlantis_StartTurn = new Effect(
            $"All non-Arctic Legends lose {AtlantisDamage} health.",
            (target, card, game, played) => StartTurnDamage(target, AtlantisDamage, Element.Arctic));

        // ───────────── Legend when-played ─────────────

        public const int MustafarId = 100;
        public const int MatrixId = 102;
        public const int WorkshopId = 103;
        public const int AtlantisId = 106;

        public static readonly Effect GenerateAndPlayMustafar = new Effect(
            "Set stage to Mustafar",
            (target, card, game, played) => game.GenerateAndPlayStage(MustafarId));

        public static readonly Effect GenerateAndPlayMatrix = new Effect(
            "Set stage to Matrix",
            (target, card, game, played) => game.GenerateAndPlayStage(MatrixId));

        public static readonly Effect GenerateAndPlayWorkshop = new Effect(
            "Set stage to The Workshop",
            (target, card, game, played) => game.GenerateAndPlayStage(WorkshopId));

        // ───────────── Battle cards ─────────────

        public static readonly Effect BattleCardDirectDamageWhenPlayed = new Effect(
            "Deals direct damage to opposing Legend",
            (target, card, game, played) =>
            {
                if (card is BattleCard battle && target is LegendCard legend)
                    legend.LoseHealth(battle.Damage);
            });

        public static readonly Effect BattleCardHealWhenPlayed = new Effect(
            "Heals your Legend",
            (target, card, game, played) =>
            {
                if (card is BattleCard battle && target is LegendCard legend)
                    legend.GainHealth(battle.HealthModifier);
            });

        public static readonly Effect BoostHealthAndPower = new Effect(
            "Modifies Health and Power",
            (target, card, game, played) =>
            {
                if (card is BattleCard battle && target is LegendCard legend)
                {
                    legend.ModifyStat(Stat.Health, battle.HealthModifier);
                    legend.ModifyStat(Stat.Power, battle.PowerModifier);
                }
            });

        public static readonly Effect BoostHealthAndPowerAllYourLegends = new Effect(
            "Modifies Health and Power",
            (target, card, game, played) =>
            {
                if (!(card is BattleCard battle)) return;
                foreach (LegendCard legend in game.ActivePlayer.PlayingField.ToList())
                {
                    if (battle.TargetElements.Contains(legend.Element))
                    {
                        legend.ModifyStat(Stat.Health, battle.HealthModifier);
                        legend.ModifyStat(Stat.Power, battle.PowerModifier);
                    }
                }
            });

        const int ChestCards = 3;

        public static readonly Effect CardChest = new Effect(
            $"Craft {ChestCards} random cards",
            (target, card, game, played) => game.GenerateRandomCards(ChestCards));

        public static readonly Effect Bubble = new Effect(
            "Tap legend Card",
            (target, card, game, played) => TapLegendCard(target));

        public static readonly Effect DragonChest = ElementChest(Element.Fire);
        public static readonly Effect WildChest = ElementChest(Element.Wild);
        public static readonly Effect SunkenChest = ElementChest(Element.Arctic);
        public static readonly Effect ShadowChest = ElementChest(Element.Shadow);
        public static readonly Effect CosmicChest = ElementChest(Element.Cosmic);

        static Effect ElementChest(Element element) => new Effect(
            $"Craft {ChestCards} random {element} cards",
            (target, card, game, played) => game.GenerateRandomCards(ChestCards, element));

        public static readonly Effect DarkDuo = new Effect(
            "Craft 2 random Shadow legends",
            (target, card, game, played) => game.GenerateRandomCards(2, Element.Shadow, CardType.Legend));

        public static readonly Effect WitchParty = new Effect(
            "Craft Fait, Witch Scarlet, Amethyst Scythe and Galaxy Lance",
            (target, card, game, played) =>
            {
                game.GenerateCard(4004);  // Fait
                game.GenerateCard(23004); // Amethyst Scythe
                game.GenerateCard(3006);  // Witch Scarlet
                game.GenerateCard(21000); // Galaxy Lance
            });

        public static readonly Effect Adrenaline = new Effect(
            "Allow legend to attack again",
            (target, card, game, played) =>
            {
                if (target is LegendCard legend)
                    legend.AttackedThisTurn = false;
            });

        public static readonly Effect DeathsHour = new Effect(
            "Every legend's HP becomes 1",
            (target, card, game, played) =>
            {
                foreach (LegendCard legend in game.GetAllLegendsOnPlayingField())
                    legend.CurrentHP = 1;
            });

        public static readonly Effect CursedKunai = new Effect(
            "Craft a random Katars legend and a random Katars weapon",
            (target, card, game, played) =>
            {
                game.GenerateRandomLegendsWithWeapon(1, Weapon.Katars);
                game.GenerateRandomWeaponCards(1, Weapon.Katars);
            });

        public static readonly Effect YmirsBreath = new Effect(
            "Tap all opposing legends",
            (target, card, game, played) =>
            {
                foreach (LegendCard legend in game.InactivePlayer.PlayingField)
                    legend.TapOut();
            });

        public static readonly Effect The_Finger = new Effect(
            "Legend attack becomes 0",
            (target, card, game, played) =>
            {
                if (target is LegendCard legend)
                    legend.SetPower(0);
            });

        public static readonly Effect ScytheForever = new Effect(
            "Craft 3 random Scythe weapons",
            (target, card, game, played) => game.GenerateRandomWeaponCards(3, Weapon.Scythe));

        public static readonly Effect DidShawWinYet = new Effect(
            "Craft 3x The Rock",
            (target, card, game, played) =>
            {
                for (int i = 0; i < 3; i++)
                    game.GenerateCard(6003); // The Rock
            });

        // ───────────── Shared helpers ─────────────

        public static void TapLegendCard(object target)
        {
            if (target is LegendCard legend)
                legend.TapOut();
        }

        static void StartTurnHeal(object target, int heal, params Element[] targetElements)
        {
            if (!(target is IEnumerable<LegendCard> legends)) return;
            foreach (LegendCard legend in legends)
            {
                if (targetElements.Contains(legend.Element))
                    legend.GainHealth(heal);
            }
        }

        static void StartTurnDamage(object target, int damage, params Element[] immuneElements)
        {
            if (!(target is IEnumerable<LegendCard> legends)) return;
            foreach (LegendCard legend in legends)
            {
                if (!immuneElements.Contains(legend.Element))
                    legend.LoseHealth(damage);
            }
        }

        static void ModifyStatOfAllLegends(Game game, IList<Element> elements, Stat stat, int amount)
        {
            foreach (Player p in game.Players)
            {
                foreach (LegendCard legend in p.PlayingField)
                {
                    if (elements.Contains(legend.Element))
                        legend.ModifyStat(stat, amount);
                }
            }
        }
    }
}
