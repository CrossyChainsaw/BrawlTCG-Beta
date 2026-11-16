using System;
using System.Linq;

namespace BrawlTCG.Core
{
    /// <param name="attacker">The attacking legend.</param>
    /// <param name="target">A Player, a LegendCard or null (insta effects).</param>
    public delegate void AttackAction(LegendCard attacker, object target, Attack attack, Game game);

    public sealed class Attack
    {
        readonly AttackAction _execute;

        public string Name { get; }
        public int AttackModifier { get; }
        public Weapon WeaponOne { get; }
        public int WeaponOneAmount { get; }
        public int WeaponOneBurnAmount { get; }
        public Weapon? WeaponTwo { get; }
        public int WeaponTwoAmount { get; }
        public int WeaponTwoBurnAmount { get; }
        public bool FriendlyFire { get; }
        /// <summary>Hits every opposing legend.</summary>
        public bool MultiHit { get; }
        /// <summary>Resolves immediately without a target.</summary>
        public bool InstaEffect { get; }

        /// <summary>What kind of attack this is; Silent Galaxy only blocks <see cref="AttackKind.Strike"/>.</summary>
        public AttackKind Kind { get; }
        /// <summary>Knocks out the target legend outright. Can never hit a player.</summary>
        public bool OneHitKO { get; }
        /// <summary>Optional extra requirement. Returns an error message when the attack cannot be used.</summary>
        public Func<Game, LegendCard, string> Precondition { get; }

        public Attack(string name, int attackModifier, Weapon weaponOne, int weaponOneAmount, AttackAction execute,
            int weaponOneBurnAmount = 0, Weapon? weaponTwo = null, int weaponTwoAmount = 0, int weaponTwoBurnAmount = 0,
            bool friendlyFire = false, bool multiHit = false, bool instaEffect = false, bool oneHitKO = false,
            Func<Game, LegendCard, string> precondition = null, AttackKind kind = AttackKind.Strike)
        {
            Name = name;
            AttackModifier = attackModifier;
            WeaponOne = weaponOne;
            WeaponOneAmount = weaponOneAmount;
            _execute = execute;
            WeaponOneBurnAmount = weaponOneBurnAmount;
            WeaponTwo = weaponTwo;
            WeaponTwoAmount = weaponTwo == null ? 0 : weaponTwoAmount;
            WeaponTwoBurnAmount = weaponTwoBurnAmount;
            FriendlyFire = friendlyFire;
            MultiHit = multiHit;
            InstaEffect = instaEffect;
            OneHitKO = oneHitKO;
            Precondition = precondition;
            Kind = kind;
        }

        public void Execute(LegendCard attacker, object target, Game game)
        {
            if (_execute == null) return;
            EffectContext.Push($"{attacker?.Name} · {Name}");
            try { _execute(attacker, target, this, game); }
            finally { EffectContext.Pop(); }
        }

        /// <summary>Number of stacked weapons that count for the given requirement ("Any" counts every weapon).</summary>
        public static int CountWeapons(LegendCard legend, Weapon weapon)
        {
            return weapon == Weapon.Any
                ? legend.StackedWeapons.Count()
                : legend.StackedWeapons.Count(w => w.Weapon == weapon);
        }

        public bool MeetsWeaponRequirements(LegendCard legend)
        {
            if (CountWeapons(legend, WeaponOne) < WeaponOneAmount) return false;
            if (WeaponTwo != null && CountWeapons(legend, WeaponTwo.Value) < WeaponTwoAmount) return false;
            return true;
        }

        /// <summary>Power + modifier, never below zero.</summary>
        public static int CalculateBaseDamage(LegendCard attacker, Attack attack)
        {
            long damage = (long)attacker.Power + attack.AttackModifier;
            return (int)Math.Max(0, Math.Min(int.MaxValue, damage));
        }

        /// <summary>
        /// Elemental bonus: for each weapon requirement, if the legend holds at least the required amount of that weapon
        /// in its own element, the attack deals that amount of extra damage.
        /// </summary>
        public static int CalculateElementalBonus(LegendCard attacker, Attack attack)
        {
            int bonus = 0;
            bonus += BonusFor(attacker, attack.WeaponOne, attack.WeaponOneAmount);
            if (attack.WeaponTwo != null)
                bonus += BonusFor(attacker, attack.WeaponTwo.Value, attack.WeaponTwoAmount);
            return bonus;

            static int BonusFor(LegendCard legend, Weapon weapon, int required)
            {
                if (required <= 0) return 0;
                int matches = legend.StackedWeapons.Count(w => w.Weapon == weapon && w.Element == legend.Element);
                return matches >= required ? required : 0;
            }
        }

        public static int CalculateDamage(LegendCard attacker, Attack attack)
        {
            long total = (long)CalculateBaseDamage(attacker, attack) + CalculateElementalBonus(attacker, attack);
            return (int)Math.Min(int.MaxValue, total);
        }

        /// <summary>Whether this attack is allowed to hit the opposing player directly.</summary>
        public bool CanHitPlayer(LegendCard attacker) => !OneHitKO && CalculateDamage(attacker, this) > 0;

        /// <summary>"1x Spear (burn 1) + 1x Greatsword - Hits All"</summary>
        public string WeaponDescription
        {
            get
            {
                string s = $"{WeaponOneAmount}x {WeaponOne}{BurnText(WeaponOneBurnAmount)}";
                if (WeaponTwo != null)
                    s += $" + {WeaponTwoAmount}x {WeaponTwo}{BurnText(WeaponTwoBurnAmount)}";
                if (MultiHit) s += " - Hits All";
                return s;
            }
        }

        static string BurnText(int n) => n > 0 ? $" (burns {n})" : "";
    }
}
