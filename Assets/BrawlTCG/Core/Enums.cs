namespace BrawlTCG.Core
{
    public enum Element
    {
        Fire,
        Magic,
        Nature,
        Cosmic,
        Arctic,
        Wild,
        Shadow,
    }

    public enum Weapon
    {
        Any,
        Blasters,
        RocketLance,
        Spear,
        Katars,
        Axe,
        Bow,
        Gauntlets,
        Scythe,
        Sword,
        Cannon,
        Orb,
        Greatsword,
        BattleBoots,
        Hammer,
    }

    /// <summary>What an attack does. Only Strikes deal damage.</summary>
    public enum AttackKind
    {
        /// <summary>Deals damage (the default for every attack with a damage modifier).</summary>
        Strike,
        /// <summary>Puts new cards into your hand.</summary>
        Craft,
        /// <summary>Draws cards from your deck.</summary>
        Draw,
        /// <summary>Puts a stage into play.</summary>
        Set,
        /// <summary>Puts a legend onto your field.</summary>
        Summon,
        /// <summary>Helps one of your own legends (buffs, healing).</summary>
        Boost,
        /// <summary>Targets an enemy legend without dealing damage (curses, stuns).</summary>
        Hex,
    }

    public enum Stat
    {
        Health,
        Power,
    }

    public enum CardType
    {
        Essence,
        Stage,
        Legend,
        Weapon,
        Battle,
    }

    /// <summary>How an attack picks its target(s), derived from the attack and the board.</summary>
    public enum AttackMode
    {
        /// <summary>No valid way to use the attack right now.</summary>
        None,
        /// <summary>Resolves immediately without a target (crafting, drawing, spawning).</summary>
        Insta,
        /// <summary>Hits the opposing player directly (their field is empty).</summary>
        Player,
        /// <summary>Requires one of your own legends as target (stunned ones too).</summary>
        OwnLegend,
        /// <summary>Requires one opposing legend as target (stunned ones too).</summary>
        EnemyLegend,
        /// <summary>Hits every opposing legend.</summary>
        AllEnemies,
    }
}
