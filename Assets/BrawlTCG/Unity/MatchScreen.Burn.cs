using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Attacks that burn weapons: when the attacker has more fitting weapons than the attack burns (and they aren't all
    /// copies of one card), the player picks which ones go. Otherwise the attack goes straight through.
    /// </summary>
    public sealed partial class MatchScreen
    {
        sealed class BurnChoice
        {
            public LegendCard Legend;
            public int AttackIndex;
            public int Target;
            /// <summary>Picked stack indices, oldest first (a new pick in a full requirement replaces its oldest).</summary>
            public List<int> Picked;
        }

        /// <summary>The burn picker being shown (null when it is closed).</summary>
        BurnChoice _burn;

        /// <summary>Sends the attack, first asking which weapons to burn when there is a choice.</summary>
        void SubmitAttack(LegendCard legend, int attackIndex, int target)
        {
            int player = legend.Owner.Index, index = legend.Owner.PlayingField.IndexOf(legend);
            Attack attack = legend.AttackAt(attackIndex);
            if (!Game.BurnChoiceNeeded(legend, attack))
            {
                Submit(GameCommand.Attack(player, index, attackIndex, target));
                return;
            }
            string error = _game.Validate(GameCommand.Attack(player, index, attackIndex, target));
            if (error != null)
            {
                _app.Toast(error);
                return;
            }

            int mask = Game.DefaultBurnMask(legend, attack);
            var picked = new List<int>();
            for (int i = 0; i < legend.StackedCards.Count; i++)
                if ((mask & (1 << i)) != 0) picked.Add(i);

            CancelTargeting();
            CloseDetail();
            Refresh();
            _burn = new BurnChoice { Legend = legend, AttackIndex = attackIndex, Target = target, Picked = picked };
            ShowBurnPicker();
        }

        void ShowBurnPicker()
        {
            BurnChoice b = _burn;
            var dim = ShowOverlay(0.94f); // resets _burn (any other overlay replaces the picker)
            _burn = b;

            LegendCard legend = b.Legend;
            Attack attack = legend.AttackAt(b.AttackIndex);
            List<Game.BurnSlot> slots = Game.BurnSlots(legend, attack);

            Theme.Label(dim, legend.Name.ToUpperInvariant() + "  ·  " + attack.Name.ToUpperInvariant(), 18, Theme.Gold,
                0, 112, 1920, 26, TextAnchor.MiddleCenter, display: true);
            var title = Theme.Label(dim, "CHOOSE WHAT TO BURN", 58, Theme.Text, 0, 142, 1920, 80, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(title, 3, 0.6f);
            string burns = string.Join(" and ", slots.Where(s => s.Amount > 0).Select(s => Count(s.Amount, s.Weapon)));
            Theme.Label(dim, $"This attack burns {burns}. Click a weapon to burn it instead.",
                20, Theme.TextDim, 0, 226, 1920, 30, TextAnchor.MiddleCenter);

            // Every weapon the attack could burn, in stack order.
            var candidates = slots.SelectMany(s => s.Candidates.Select(i => (slot: s, index: i))).OrderBy(c => c.index).ToList();
            int n = candidates.Count;
            const float gap = 18;
            float scale = Mathf.Min(1.3f, (1760 - (n - 1) * gap) / (n * CardView.W));
            float w = CardView.W * scale;
            float start = 960 - (n * w + (n - 1) * gap) / 2;
            for (int k = 0; k < n; k++)
            {
                (Game.BurnSlot slot, int index) = candidates[k];
                var weapon = (WeaponCard)legend.StackedCards[index];
                bool picked = b.Picked.Contains(index);
                float x = start + k * (w + gap), y = 300 + (picked ? 36 : 0);
                CardView v = CardView.Create(dim, weapon, true, x, y, scale);
                v.Clicked = _ =>
                {
                    TogglePick(b, slot, index);
                    Sfx.Play("ui_click", 0.5f);
                    ShowBurnPicker();
                };
                if (picked)
                {
                    v.SetDimmed(true, 0.85f);
                    v.AddCenterBanner("BURN", Theme.Danger);
                }
                if (weapon.Element == legend.Element)
                    Theme.Label(dim, $"+{weapon.ElementalBonus} ELEMENTAL", 15, Theme.Gold, x, y + CardView.H * scale + 6, w, 22,
                        TextAnchor.MiddleCenter, display: true);
            }

            int missing = slots.Sum(s => s.Amount - s.Candidates.Count(b.Picked.Contains));
            string label = missing > 0 ? $"PICK {missing} MORE" : "BURN & ATTACK";
            Theme.Cta(dim, label, 960 - 210, 680, 420, 76, ConfirmBurn, 30);
            Theme.Ghost(dim, "CANCEL", 960 - 110, 776, 220, 52, CancelBurn);
        }

        /// <summary>Unpicks a picked weapon; picks an unpicked one, replacing the oldest pick of its requirement if that is full.</summary>
        static void TogglePick(BurnChoice b, Game.BurnSlot slot, int index)
        {
            if (b.Picked.Remove(index)) return;
            List<int> inSlot = b.Picked.Where(slot.Candidates.Contains).ToList();
            if (inSlot.Count >= slot.Amount && inSlot.Count > 0) b.Picked.Remove(inSlot[0]);
            b.Picked.Add(index);
        }

        void ConfirmBurn()
        {
            BurnChoice b = _burn;
            if (b == null) return;
            int mask = 0;
            foreach (int i in b.Picked) mask |= 1 << i;
            if (!Game.IsValidBurnMask(b.Legend, b.Legend.AttackAt(b.AttackIndex), mask))
            {
                _app.Toast("Pick the weapons to burn first.");
                return;
            }
            CloseOverlay();
            Submit(GameCommand.Attack(b.Legend.Owner.Index, b.Legend.Owner.PlayingField.IndexOf(b.Legend), b.AttackIndex, b.Target, mask));
        }

        void CancelBurn()
        {
            CloseOverlay();
            Refresh();
        }

        static string Count(int amount, Weapon weapon)
        {
            if (weapon == Weapon.Any) return amount == 1 ? "1 weapon" : $"{amount} weapons";
            string name = Pretty(weapon);
            return amount == 1 || name.EndsWith("s") ? $"{amount} {name}" : $"{amount} {name}s";
        }
    }
}
