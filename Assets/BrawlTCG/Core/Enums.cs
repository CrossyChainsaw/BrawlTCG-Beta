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
        /// <summary>Requires one of your own untapped legends as target.</summary>
        OwnLegend,
        /// <summary>Requires one opposing untapped legend as target.</summary>
        EnemyLegend,
        /// <summary>Hits every opposing legend.</summary>
        AllEnemies,
    }
}
