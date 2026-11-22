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

        /// <summary>
        /// Null for cards that came from a deck. For cards made during the match, what made them: a card
        /// ("Dark Duo", "The Workshop") or an attack ("Yumiko · Craft 3 Orbs"). Display only.
        /// </summary>
        public string CraftedBy { get; internal set; }

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
            copy.CraftedBy = null;
            copy.ResetRuntimeState();
            return copy;
        }

        protected virtual void ResetRuntimeState() { }

        /// <summary>For <see cref="Game.CloneForSearch"/>: an exact copy, runtime state included. The caller re-points
        /// <see cref="Owner"/> and a legend's stacked cards at the copied match.</summary>
        internal Card CopyForSearch()
        {
            Card copy = (Card)MemberwiseClone();
            copy.CopyMutableState();
            return copy;
        }

        protected virtual void CopyMutableState() { }

        public string EffectDescription => GenerateEffectDescription(this);

        public static string GenerateEffectDescription(Card c)
        {
            string description = "";
            if (c.StartTurnEffect != null)
                description += $"Start Turn: {c.StartTurnEffect.Description}\n";
            if (c.WhenPlayedEffect != null)
                description += $"When Played: {c.WhenPlayedEffect.Description}\n";
            if (c.WhenDiscardedEffect != null)
                description += $"{(c is LegendCard ? "When Eliminated" : "When Discarded")}: {c.WhenDiscardedEffect.Description}\n";
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

        /// <summary>The power printed on the card (before buffs / debuffs).</summary>
        public int PrintedPower => _basePower;
        /// <summary>The health printed on the card (before buffs / debuffs).</summary>
        public int PrintedHealth => _baseHealth;

        public int BaseHealth { get; internal set; }
        public int CurrentHP { get; internal set; }
        public int Power { get; internal set; }
        public Weapon PrimaryWeapon { get; }
        public Weapon SecondaryWeapon { get; }
        public List<Card> StackedCards { get; private set; } = new List<Card>();
        public IReadOnlyList<Attack> Attacks => _attacks;

        /// <summary>Stunned legends cannot attack and their abilities are off. They can still be attacked and have cards played on them.</summary>
        public bool IsStunned { get; internal set; }
        public bool IsBurned { get; internal set; }
        public bool CanAttack { get; internal set; } = true;
        public bool AttackedThisTurn { get; internal set; }
        public bool OnPlayingField { get; internal set; }

        /// <summary>Mako's Reef: the HP this legend had when the reef set it to 1, given back when the reef leaves.
        /// Null when the reef is not holding it. While held, healing and max-HP gains go to this value instead.</summary>
        public int? ReefSavedHP { get; private set; }
        public bool HeldByReef => ReefSavedHP != null;

        /// <summary>A net stat change from one source (a card, or "Legend · Attack"), for showing what affects this legend.</summary>
        public sealed class StatModifier
        {
            public string Source { get; internal set; }
            public int Power { get; internal set; }
            public int Health { get; internal set; }
        }

        List<StatModifier> _modifiers = new List<StatModifier>(); // replaced (not cleared) on reset: clones must not share it
        /// <summary>Net ATK / max-HP changes per source, oldest first. Changes that cancel out are dropped.</summary>
        public IReadOnlyList<StatModifier> Modifiers => _modifiers;
        /// <summary>What stunned / burned this legend most recently (only meaningful while it is stunned / burned).</summary>
        public string StunnedBy { get; private set; }
        public string BurnedBy { get; private set; }

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
            IsStunned = false;
            IsBurned = false;
            CanAttack = true;
            AttackedThisTurn = false;
            OnPlayingField = false;
            ReefSavedHP = null;
            _modifiers = new List<StatModifier>();
            StunnedBy = null;
            BurnedBy = null;
        }

        protected override void CopyMutableState()
        {
            var modifiers = new List<StatModifier>(_modifiers.Count);
            foreach (StatModifier m in _modifiers)
                modifiers.Add(new StatModifier { Source = m.Source, Power = m.Power, Health = m.Health });
            _modifiers = modifiers;
        }

        internal void ReplaceStackedCards(List<Card> cards) => StackedCards = cards;

        void Record(int power, int health)
        {
            if (power == 0 && health == 0) return;
            string source = EffectContext.Current ?? "Unknown";
            StatModifier m = _modifiers.Find(x => x.Source == source);
            if (m == null) _modifiers.Add(m = new StatModifier { Source = source });
            m.Power += power;
            m.Health += health;
            if (m.Power == 0 && m.Health == 0) _modifiers.Remove(m);
        }

        /// <summary>Sets ATK to a value (e.g. "power becomes 0"), remembering the change like a modifier.</summary>
        internal void SetPower(int value)
        {
            Record(value - Power, 0);
            Power = value;
        }

        public bool HasWeapon(Weapon weapon) => PrimaryWeapon == weapon || SecondaryWeapon == weapon;

        public IEnumerable<WeaponCard> StackedWeapons => StackedCards.OfType<WeaponCard>();

        public void LoseHealth(int damage)
        {
            if (damage <= 0) return;
            CurrentHP = (int)Math.Max(int.MinValue / 2, (long)CurrentHP - damage);
        }

        /// <summary>Mako's Reef comes into play (or this legend arrives while it is in play): HP becomes 1, the rest is kept.</summary>
        internal void EnterReef()
        {
            if (HeldByReef || CurrentHP <= 0) return;
            ReefSavedHP = CurrentHP;
            CurrentHP = 1;
        }

        /// <summary>Mako's Reef leaves play: the legend gets back the HP it had (never above its max, never below 1).</summary>
        internal void LeaveReef()
        {
            if (!HeldByReef) return;
            if (CurrentHP > 0)
                CurrentHP = Math.Max(1, Math.Min(BaseHealth, ReefSavedHP.Value));
            ReefSavedHP = null;
        }

        /// <summary>Sets HP to a value (Death's Hour). Under Mako's Reef it also sets the HP that comes back.</summary>
        internal void SetHealth(int value)
        {
            if (HeldByReef) ReefSavedHP = value;
            else CurrentHP = value;
        }

        public void GainHealth(int healing)
        {
            if (healing <= 0) return;
            if (HeldByReef)
            {
                ReefSavedHP = (int)Math.Min(BaseHealth, (long)ReefSavedHP.Value + healing);
                return;
            }
            CurrentHP = (int)Math.Min(BaseHealth, (long)CurrentHP + healing);
        }

        public void ModifyStat(Stat stat, int amount)
        {
            if (stat == Stat.Health)
            {
                BaseHealth += amount;
                if (HeldByReef)
                {
                    // Under the reef, gains wait for it to leave; losses still hurt now.
                    ReefSavedHP += amount;
                    if (amount < 0) CurrentHP += amount;
                }
                else
                    CurrentHP += amount;
                Record(0, amount);
            }
            else
            {
                Power += amount;
                Record(amount, 0);
            }
        }

        public void SetBurn(bool burn)
        {
            if (Element != Element.Fire)
            {
                IsBurned = burn;
                if (burn) BurnedBy = EffectContext.Current;
            }
        }

        public void TakeBurnDamage()
        {
            if (IsBurned)
                LoseHealth(BurnDamage);
        }

        public void Stun()
        {
            IsStunned = true;
            StunnedBy = EffectContext.Current;
        }

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
