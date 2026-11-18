using System;

namespace BrawlTCG.Core
{
    public static class AttackCatalogue
    {
        /// <summary>Status attacks use this modifier so they never deal damage and never hit the player.</summary>
        const int Status = -1000;

        public static void DefaultAttack(LegendCard attacker, object target, Attack attack, bool burn = false)
        {
            int damage = Attack.CalculateDamage(attacker, attack);
            if (target is LegendCard legend)
            {
                legend.LoseHealth(damage);
                if (burn) legend.SetBurn(true);
            }
            else if (target is Player player)
            {
                player.LoseHealth(damage);
            }
        }

        static void OneHitKO(object target)
        {
            if (target is LegendCard legend)
                legend.LoseHealth(Math.Max(1, legend.CurrentHP));
        }

        static void ModifyStat(object target, Stat stat, int amount)
        {
            if (target is LegendCard legend)
                legend.ModifyStat(stat, amount);
        }

        static void SelfDestruct(LegendCard attacker) => attacker.LoseHealth(Math.Max(1, attacker.CurrentHP));

        static readonly AttackAction Default = (attacker, target, attack, game) => DefaultAttack(attacker, target, attack);

        // ───────────── Default weapon attacks ─────────────
        public static readonly Attack Spear_Stab = new Attack("Spear Stab", 0, Weapon.Spear, 1, Default);
        public static readonly Attack Cannon_Blast = new Attack("Cannon Blast", 1, Weapon.Cannon, 1, Default);
        public static readonly Attack Orb_Throw = new Attack("Orb Throw", 3, Weapon.Orb, 1, Default, weaponOneBurnAmount: 1);
        public static readonly Attack Orb_Swing = new Attack("Orb Swing", 0, Weapon.Orb, 1, Default);
        public static readonly Attack Greatsword_Swing = new Attack("Great Swing", 0, Weapon.Greatsword, 1, Default);
        public static readonly Attack Greatsword_String = new Attack("Stab, Slice, Swing", 5, Weapon.Greatsword, 3, Default);
        public static readonly Attack Scythe_Slash = new Attack("Scythe Slash", 0, Weapon.Scythe, 1, Default);
        public static readonly Attack Scythe_Gimp = new Attack("Scythe Gimp", 0, Weapon.Scythe, 3,
            (a, t, atk, g) => OneHitKO(t), weaponOneBurnAmount: 2, oneHitKO: true);
        public static readonly Attack Lance_Poke = new Attack("Lance Poke", 0, Weapon.RocketLance, 1, Default);
        public static readonly Attack Lance_Flamethrower = new Attack("Flamethrower", 4, Weapon.RocketLance, 2, Default);
        public static readonly Attack Hammer_Swing = new Attack("Hammer Swing", 1, Weapon.Hammer, 1, Default);
        public static readonly Attack Hammer_Gimp = new Attack("Hammer Gimp", 0, Weapon.Hammer, 3,
            (a, t, atk, g) => OneHitKO(t), weaponOneBurnAmount: 2, oneHitKO: true);
        public static readonly Attack Axe_Swing = new Attack("Axe Swing", 1, Weapon.Axe, 1, Default);
        public static readonly Attack Gauntlets_Punch = new Attack("Punch", 0, Weapon.Gauntlets, 1, Default);
        public static readonly Attack Gauntlets_PowerPunch = new Attack("Power Punch", 4, Weapon.Gauntlets, 2, Default);
        public static readonly Attack Katar_Slash = new Attack("Slash", 0, Weapon.Katars, 1, Default);
        public static readonly Attack Sword_Slash = new Attack("Slash", 0, Weapon.Sword, 1, Default);
        public static readonly Attack Blaster_Shot = new Attack("Shot", 0, Weapon.Blasters, 1, Default);
        public static readonly Attack Blaster_DoubleShot = new Attack("Double Shot", 5, Weapon.Blasters, 2, Default);

        // ───────────── Stun ─────────────
        public static readonly Attack Any_BlowAKiss = new Attack("Blow a Kiss", Status, Weapon.Any, 2,
            (a, t, atk, g) => EffectCatalogue.StunLegendCard(t),
            kind: AttackKind.Hex);
        public static readonly Attack Any_Freeze = new Attack("Freeze", 0, Weapon.Any, 1,
            (a, t, atk, g) =>
            {
                DefaultAttack(a, t, atk);
                EffectCatalogue.StunLegendCard(t);
            });

        // ───────────── Insta effects ─────────────
        public static readonly Attack Any_BurnForThreeCard = new Attack("Draw Three Cards", Status, Weapon.Any, 1,
            (a, t, atk, g) => g.DrawCards(g.ActivePlayer, 3), weaponOneBurnAmount: 1, instaEffect: true,
            kind: AttackKind.Draw);
        public static readonly Attack Any_CraftFireCard = new Attack("Craft Fire Card", Status, Weapon.Any, 1,
            (a, t, atk, g) => g.GenerateRandomCards(1, Element.Fire), instaEffect: true,
            kind: AttackKind.Craft);
        public static readonly Attack Any_CraftCosmicCard = new Attack("Craft Cosmic Card", Status, Weapon.Any, 1,
            (a, t, atk, g) => g.GenerateRandomCards(1, Element.Cosmic), instaEffect: true,
            kind: AttackKind.Craft);
        public static readonly Attack Any_CraftBattleCard = new Attack("Craft Battle Card", Status, Weapon.Any, 1,
            (a, t, atk, g) => g.GenerateRandomCards(1, type: CardType.Battle), instaEffect: true,
            kind: AttackKind.Craft);
        public static readonly Attack Any_GenerateAndPlayMatrix = new Attack("Set stage to Matrix", Status, Weapon.Any, 1,
            (a, t, atk, g) => g.GenerateAndPlayStage(EffectCatalogue.MatrixId), instaEffect: true,
            kind: AttackKind.Set);

        // ───────────── Signature attacks ─────────────
        public static readonly Attack Arcadia_PinkRoses = new Attack("Pink Roses", 4, Weapon.Spear, 1, Default,
            weaponTwo: Weapon.Greatsword, weaponTwoAmount: 1);
        public static readonly Attack Artemis_IronLady_MeltDown = new Attack("Meltdown", 7, Weapon.Scythe, 1, Default,
            weaponOneBurnAmount: 1, weaponTwo: Weapon.RocketLance, weaponTwoAmount: 1, weaponTwoBurnAmount: 1);
        public static readonly Attack Enchantress_CursePower = new Attack("Curse Att by 3", Status, Weapon.Any, 1,
            (a, t, atk, g) => ModifyStat(t, Stat.Power, -3), weaponOneBurnAmount: 1,
            kind: AttackKind.Hex);
        public static readonly Attack Enchantress_CurseHealth = new Attack("Curse HP by 4", Status, Weapon.Any, 1,
            (a, t, atk, g) => ModifyStat(t, Stat.Health, -4), weaponOneBurnAmount: 1,
            kind: AttackKind.Hex);
        public static readonly Attack Enchantress_EnchantPower = new Attack("Enchant Att by 3", Status, Weapon.Any, 1,
            (a, t, atk, g) => ModifyStat(t, Stat.Power, 3), weaponOneBurnAmount: 1, friendlyFire: true,
            kind: AttackKind.Boost);
        public static readonly Attack Enchantress_EnchantHealth = new Attack("Enchant HP by 4", Status, Weapon.Any, 1,
            (a, t, atk, g) => ModifyStat(t, Stat.Health, 4), weaponOneBurnAmount: 1, friendlyFire: true,
            kind: AttackKind.Boost);
        public static readonly Attack DeathCap_Storm = new Attack("Storm", 0, Weapon.Orb, 3, Default,
            weaponOneBurnAmount: 3, multiHit: true);
        public static readonly Attack Heatblast_Burn = new Attack("Burn", 0, Weapon.Blasters, 1,
            (a, t, atk, g) => DefaultAttack(a, t, atk, burn: true), weaponOneBurnAmount: 1);
        public static readonly Attack MasterThief_GrabBomb = new Attack("Craft Bouncy Bomb", Status, Weapon.Gauntlets, 1,
            (a, t, atk, g) => g.GenerateCard(500), instaEffect: true,
            kind: AttackKind.Craft);
        public static readonly Attack PlagueKnight_GrabHealingPotion = new Attack("Craft Vial of Crows", Status, Weapon.Gauntlets, 1,
            (a, t, atk, g) => g.GenerateCard(501), instaEffect: true,
            kind: AttackKind.Craft);
        public static readonly Attack Yumiko_GrabOrbs = new Attack("Craft 3 Orbs", Status, Weapon.Any, 1,
            (a, t, atk, g) =>
            {
                g.GenerateCard(504);
                g.GenerateCard(504);
                g.GenerateCard(504);
            }, instaEffect: true,
            kind: AttackKind.Craft);
        public static readonly Attack WuShang_DownSig = new Attack("Gauntlet Dsig", -2, Weapon.Gauntlets, 2, Default, multiHit: true);
        public static readonly Attack NinjaSpirit_PhantomSlash = new Attack("Phantom Slash", 0, Weapon.Sword, 0, Default);
        public static readonly Attack ForestSpirit_Explode = new Attack("Explode", 0, Weapon.Any, 0,
            (a, t, atk, g) =>
            {
                DefaultAttack(a, t, atk);
                SelfDestruct(a);
            });
        public static readonly Attack ForestSpirit_Heal = new Attack("Heal", 0, Weapon.Any, 0,
            (a, t, atk, g) =>
            {
                if (t is LegendCard legend) legend.GainHealth(a.Power);
                SelfDestruct(a);
            }, friendlyFire: true,
            kind: AttackKind.Boost);
        // Alpine Kaya: both count as her attack for the turn (insta = no target needed).
        public static readonly Attack Kaya_CraftSnowball = new Attack("Craft Snowball", Status, Weapon.Any, 1,
            (a, t, atk, g) => g.GenerateCard(502), instaEffect: true,
            kind: AttackKind.Craft);
        public static readonly Attack Kaya_BuildASnowman = new Attack("Build a Snowman", Status, Weapon.Any, 2,
            (a, t, atk, g) => g.GenerateAndPlayLegend(7000), weaponOneBurnAmount: 1, instaEffect: true,
            precondition: (g, legend) => legend.Owner.PlayingField.Count >= Game.MaxLegendsOnField ? $"Already {Game.MaxLegendsOnField} Legends" : null,
            kind: AttackKind.Summon);

        public static readonly Attack Jiro_SpawnAndPlayNinjaSpirit = new Attack("Summon Ninja Spirit", Status, Weapon.Sword, 1,
            (a, t, atk, g) => g.GenerateAndPlayLegend(5003), instaEffect: true,
            precondition: (g, legend) => legend.Owner.PlayingField.Count >= Game.MaxLegendsOnField ? $"Already {Game.MaxLegendsOnField} Legends" : null,
            kind: AttackKind.Summon);
        public static readonly Attack Ada_SpawnAndPlayAtlantis = new Attack("Set stage to Atlantis", Status, Weapon.Blasters, 1,
            (a, t, atk, g) => g.GenerateAndPlayStage(EffectCatalogue.AtlantisId), instaEffect: true,
            weaponTwo: Weapon.Spear, weaponTwoAmount: 1,
            kind: AttackKind.Set);
        public static readonly Attack Seven_CraftWeapons = new Attack("Craft 4 Random Weapons", Status, Weapon.Spear, 1,
            (a, t, atk, g) => g.GenerateRandomWeaponCards(4), weaponOneBurnAmount: 1, instaEffect: true,
            kind: AttackKind.Craft);
    }
}
