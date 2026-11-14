using System;
using System.Collections.Generic;
using System.Linq;

namespace BrawlTCG.Core
{
    public abstract class Card
    {
        public int Id { get; }
        public string Name { get; }
        public int Cost { get; }
        public Element Element { get; }
        /// <summary>Art key (file name without extension under Resources/BrawlTCG/Art).</summary>
        public string Art { get; }

        public Player Owner { get; internal set; }
        public bool IsDiscarded { get; internal set; }

        public Effect StartTurnEffect { get; }
        public Effect WhenPlayedEffect { get; }
        public Effect WhenDiscardedEffect { get; }
        public Effect WhileInPlayEffect { get; }

        public abstract CardType Type { get; }

        /// <summary>Short text shown in lists (deck builder) and on the zoomed card.</summary>
        public abstract string Description { get; }

        protected Card(int id, string name, int cost, Element element, string art,
            Effect startTurnEffect = null, Effect whenPlayedEffect = null,
            Effect whenDiscardedEffect = null, Effect whileInPlayEffect = null)
        {
            Id = id;
            Name = name;
            Cost = cost;
            Element = element;
            Art = art;
            StartTurnEffect = startTurnEffect;
            WhenPlayedEffect = whenPlayedEffect;
            WhenDiscardedEffect = whenDiscardedEffect;
            WhileInPlayEffect = whileInPlayEffect;
        }

        /// <summary>A fresh copy in its pristine state (no owner, not discarded, base stats).</summary>
        public Card Clone()
        {
            Card copy = (Card)MemberwiseClone();
            copy.Owner = null;
            copy.IsDiscarded = false;
            copy.ResetRuntimeState();
            return copy;
        }

        protected virtual void ResetRuntimeState() { }

        public string EffectDescription => GenerateEffectDescription(this);

        public static string GenerateEffectDescription(Card c)
        {
            string description = "";
            if (c.StartTurnEffect != null)
                description += $"Start Turn: {c.StartTurnEffect.Description}\n";
            if (c.WhenPlayedEffect != null)
                description += $"When Played: {c.WhenPlayedEffect.Description}\n";
            if (c.WhenDiscardedEffect != null)
                description += $"When Discarded: {c.WhenDiscardedEffect.Description}\n";
            if (c.WhileInPlayEffect != null)
                description += $"While In Play: {c.WhileInPlayEffect.Description}\n";
            return description;
        }

        public override string ToString() => $"{Name}#{Id}";
    }

    public sealed class EssenceCard : Card
    {
        public override CardType Type => CardType.Essence;
        public override string Description => EffectDescription;

        public EssenceCard(int id, string name, int cost, Element element, string art, Effect startTurnEffect)
            : base(id, name, cost, element, art, startTurnEffect: startTurnEffect) { }
    }

    public sealed class StageCard : Card
    {
        public override CardType Type => CardType.Stage;
        public override string Description => EffectDescription;

        public StageCard(int id, string name, int cost, Element element, string art,
            Effect startTurnEffect = null, Effect whenPlayedEffect = null,
            Effect whenDiscardedEffect = null, Effect whileInPlayEffect = null)
            : base(id, name, cost, element, art, startTurnEffect, whenPlayedEffect, whenDiscardedEffect, whileInPlayEffect) { }
    }

    public sealed class WeaponCard : Card
    {
        public Weapon Weapon { get; }
        public override CardType Type => CardType.Weapon;
        public override string Description => $"Play this on {GetArticle(Weapon.ToString())} {Weapon} Legend to unlock attacks";

        public WeaponCard(int id, string name, int cost, Element element, string art, Weapon weapon)
            : base(id, name, cost, element, art)
        {
            Weapon = weapon;
        }

        public static string GetArticle(string word)
        {
            word = word.Trim().ToLowerInvariant();
            return word.Length > 0 && "aeiou".IndexOf(word[0]) >= 0 ? "an" : "a";
        }
    }

    public sealed class BattleCard : Card
    {
        readonly string _description;

        public bool OneTimeUse { get; }
        public bool Stackable { get; }
        public bool FriendlyFire { get; }
        public bool TargetRequired { get; }
        public int Damage { get; }
        public int PowerModifier { get; }
        public int HealthModifier { get; }
        public Element[] TargetElements { get; }
        public bool MultiTarget { get; }

        public override CardType Type => CardType.Battle;
        public override string Description => _description;

        public BattleCard(int id, string name, int cost, string description, Element element, string art,
            bool oneTimeUse, bool stackable, bool friendlyFire, Effect whenPlayedEffect = null,
            int damage = 0, int powerModifier = 0, int healthModifier = 0,
            Element[] targetElements = null, bool multiTarget = false, bool targetRequired = true)
            : base(id, name, cost, element, art, whenPlayedEffect: whenPlayedEffect)
        {
            if (oneTimeUse == stackable)
                throw new ArgumentException($"Battle card {name} must be either one-time-use or stackable.");
            _description = description;
            OneTimeUse = oneTimeUse;
            Stackable = stackable;
            FriendlyFire = friendlyFire;
            Damage = damage;
            PowerModifier = powerModifier;
            HealthModifier = healthModifier;
            TargetElements = targetElements ?? Array.Empty<Element>();
            MultiTarget = multiTarget;
            TargetRequired = targetRequired;
        }
    }

    public sealed class LegendCard : Card
    {
        public const int BurnDamage = 1;

        readonly int _baseHealth;
        readonly int _basePower;
        readonly Attack[] _attacks;

        public int BaseHealth { get; internal set; }
        public int CurrentHP { get; internal set; }
        public int Power { get; internal set; }
        public Weapon PrimaryWeapon { get; }
        public Weapon SecondaryWeapon { get; }
        public List<Card> StackedCards { get; private set; } = new List<Card>();
        public IReadOnlyList<Attack> Attacks => _attacks;

        /// <summary>Tapped legends are shown face down, cannot attack and cannot be targeted by attacks.</summary>
        public bool IsTapped { get; internal set; }
        public bool IsBurned { get; internal set; }
        public bool CanAttack { get; internal set; } = true;
        public bool AttackedThisTurn { get; internal set; }
        public bool OnPlayingField { get; internal set; }

        public override CardType Type => CardType.Legend;
        public override string Description => $"{_baseHealth}hp  {_basePower}att  {PrimaryWeapon} / {SecondaryWeapon}";

        public LegendCard(int id, string name, int cost, Element element, string art,
            int power, int health, Weapon primaryWeapon, Weapon secondaryWeapon,
            Attack[] attacks = null,
            Effect startTurnEffect = null, Effect whenPlayedEffect = null,
            Effect whenDiscardedEffect = null, Effect whileInPlayEffect = null)
            : base(id, name, cost, element, art, startTurnEffect, whenPlayedEffect, whenDiscardedEffect, whileInPlayEffect)
        {
            _basePower = power;
            _baseHealth = health;
            PrimaryWeapon = primaryWeapon;
            SecondaryWeapon = secondaryWeapon;
            _attacks = (attacks ?? Array.Empty<Attack>()).Where(a => a != null).ToArray();
            ResetRuntimeState();
        }

        protected override void ResetRuntimeState()
        {
            BaseHealth = _baseHealth;
            CurrentHP = _baseHealth;
            Power = _basePower;
            StackedCards = new List<Card>();
            IsTapped = false;
            IsBurned = false;
            CanAttack = true;
            AttackedThisTurn = false;
            OnPlayingField = false;
        }

        public bool HasWeapon(Weapon weapon) => PrimaryWeapon == weapon || SecondaryWeapon == weapon;

        public IEnumerable<WeaponCard> StackedWeapons => StackedCards.OfType<WeaponCard>();

        public void LoseHealth(int damage)
        {
            if (damage <= 0) return;
            CurrentHP = (int)Math.Max(int.MinValue / 2, (long)CurrentHP - damage);
        }

        public void GainHealth(int healing)
        {
            if (healing <= 0) return;
            CurrentHP = (int)Math.Min(BaseHealth, (long)CurrentHP + healing);
        }

        public void ModifyStat(Stat stat, int amount)
        {
            if (stat == Stat.Health)
            {
                BaseHealth += amount;
                CurrentHP += amount;
            }
            else
            {
                Power += amount;
            }
        }

        public void SetBurn(bool burn)
        {
            if (Element != Element.Fire)
                IsBurned = burn;
        }

        public void TakeBurnDamage()
        {
            if (IsBurned)
                LoseHealth(BurnDamage);
        }

        public void TapOut() => IsTapped = true;

        /// <summary>Removes up to <paramref name="amount"/> weapon cards of the given type and returns them.</summary>
        internal List<WeaponCard> BurnWeapon(Weapon? weapon, int amount)
        {
            var burned = new List<WeaponCard>();
            if (weapon == null || amount <= 0) return burned;
            for (int i = 0; i < StackedCards.Count && burned.Count < amount; i++)
            {
                if (StackedCards[i] is WeaponCard w && (w.Weapon == weapon || weapon == Weapon.Any))
                {
                    StackedCards.RemoveAt(i);
                    burned.Add(w);
                    i--;
                }
            }
            return burned;
        }
    }
}
