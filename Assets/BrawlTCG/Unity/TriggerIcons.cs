using System;
using System.Collections.Generic;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Small badges on legend and stage cards that show they have an ability and when it fires:
    /// bolt = when played, infinity = while in play, arrow = start of turn.
    /// </summary>
    public static class TriggerIcons
    {
        public enum Trigger { WhenPlayed, WhileInPlay, StartOfTurn }

        public enum BadgeStyle
        {
            /// <summary>Sits on the frame between the name and the art, covering the least art.</summary>
            FrameEdge,
            /// <summary>Sits inside the top-right corner of the art.</summary>
            DarkDisc,
        }

        /// <summary>Which look to use. Both are kept; switch here to change every card.</summary>
        public static BadgeStyle Style = BadgeStyle.FrameEdge;

        /// <summary>Badge size and opacity, in card-face units (the face is 300 x 400).</summary>
        public const float Size = 36, Opacity = 0.9f;

        public static readonly Color PlayedColor = new Color32(245, 192, 74, 255);
        public static readonly Color WhileColor = new Color32(176, 120, 255, 255);
        public static readonly Color StartColor = new Color32(110, 220, 150, 255);

        public static Color ColorFor(Trigger t) => t == Trigger.WhenPlayed ? PlayedColor : t == Trigger.WhileInPlay ? WhileColor : StartColor;
        public static string LabelFor(Trigger t) => t == Trigger.WhenPlayed ? "When played" : t == Trigger.WhileInPlay ? "While in play" : "Start of turn";

        static bool HasText(Effect e) => e != null && !string.IsNullOrWhiteSpace(e.Description) && e.Description != "N/A";

        /// <summary>
        /// The triggers worth showing. Only legends and stages: a battle card's "when played" effect is the card itself,
        /// and Essence's +1 is the basic rule. Effects without card text (internal bookkeeping) are left out.
        /// </summary>
        public static List<Trigger> For(Card card)
        {
            var list = new List<Trigger>();
            if (!(card is LegendCard) && !(card is StageCard)) return list;
            if (HasText(card.WhenPlayedEffect)) list.Add(Trigger.WhenPlayed);
            if (HasText(card.WhileInPlayEffect)) list.Add(Trigger.WhileInPlay);
            if (HasText(card.StartTurnEffect)) list.Add(Trigger.StartOfTurn);
            return list;
        }

        public static bool Any(Card card) => For(card).Count > 0;

        /// <summary>Draws the badges on a card face (300 x 400 design space).</summary>
        public static void Paint(RectTransform face, Card card)
        {
            List<Trigger> triggers = For(card);
            for (int i = 0; i < triggers.Count; i++)
            {
                Color c = ColorFor(triggers[i]);
                var holder = Badge(face, "Trigger " + triggers[i], i, c);
                float pad = Size * 0.2f;
                Glyph(holder, GlyphFor(triggers[i]), c, pad);
            }
        }

        public static readonly Color NoStrikesColor = new Color32(255, 84, 84, 255);

        /// <summary>
        /// "Can't deal damage" debuff (Silent Galaxy): a "not allowed" sign, a white sword crossed by a red bar inside
        /// a red ring. It joins the badge row, to the left of the ability badges.
        /// </summary>
        public static void PaintNoStrikes(RectTransform face, Card card)
        {
            int slot = For(card).Count + (card is LegendCard l && l.IsBurned ? 1 : 0); // after the burn badge
            var holder = Badge(face, "NoStrikes", slot, NoStrikesColor);
            Glyph(holder, _sword ??= Glyph(SwordCut), new Color32(238, 232, 240, 255), Size * 0.12f);
            Glyph(holder, _slash ??= Glyph(Slash), NoStrikesColor, 1);
        }

        public static readonly Color FireColor = new Color32(255, 128, 36, 255);

        /// <summary>Burned (-1 HP each turn): an orange flame, in the badge row left of the ability badges.</summary>
        public static void PaintBurned(RectTransform face, Card card)
        {
            var holder = Badge(face, "Burned", For(card).Count, FireColor);
            Glyph(holder, Flame, FireColor, Size * 0.14f);
        }

        // ───────────── Effects on a legend (detail panel): small inline icons ─────────────

        public enum EffectIcon { NoStrikes, Stunned, Burned, AttackUp, AttackDown, HealthUp, HealthDown, Attacked }

        public static readonly Color BuffColor = new Color32(110, 220, 130, 255);
        public static readonly Color DebuffColor = new Color32(255, 84, 84, 255);
        public static readonly Color StunnedColor = new Color32(245, 192, 74, 255);
        static readonly Color SpentColor = new Color32(150, 156, 184, 255);

        /// <summary>A small round icon (coloured ring, dark disc, glyph) for an effect on a legend, for use next to text.</summary>
        public static void MiniEffect(Transform parent, EffectIcon kind, float x, float y, float size)
        {
            if (kind == EffectIcon.NoStrikes) { MiniNoStrikes(parent, x, y, size); return; }
            Color c; Sprite s; float pad = 0.22f;
            switch (kind)
            {
                case EffectIcon.Stunned: c = StunnedColor; s = StunnedCard; pad = 0.18f; break;
                case EffectIcon.Burned: c = FireColor; s = Flame; pad = 0.2f; break;
                case EffectIcon.AttackUp: c = BuffColor; s = FullSword; pad = 0.18f; break;
                case EffectIcon.AttackDown: c = DebuffColor; s = FullSword; pad = 0.18f; break;
                case EffectIcon.HealthUp: c = BuffColor; s = Heart; break;
                case EffectIcon.HealthDown: c = DebuffColor; s = Heart; break;
                default: c = SpentColor; s = FullSword; pad = 0.18f; break;
            }
            var holder = MiniDisc(parent, "Icon " + kind, c, x, y, size);
            var glyph = UI.Panel(holder, c, size * pad, size * pad, size * (1 - 2 * pad), size * (1 - 2 * pad), "Glyph");
            glyph.sprite = s;
            glyph.preserveAspect = true;
            glyph.raycastTarget = false;
        }

        static RectTransform MiniDisc(Transform parent, string name, Color ring, float x, float y, float size)
        {
            var holder = UI.Place(UI.Rect(name, parent), x, y, size, size);
            float r = Mathf.Max(2, size * 0.09f), disc = size - 2 * r;
            Quiet(Theme.RoundedPanel(holder, ring, 0, 0, size, size, size / 2, "Ring"));
            Quiet(Theme.RoundedPanel(holder, new Color(0.04f, 0.05f, 0.14f, 0.95f), r, r, disc, disc, disc / 2, "Disc"));
            return holder;
        }

        /// <summary>One round badge (coloured ring, dark disc) at a slot of the badge row, counted from the right.</summary>
        static RectTransform Badge(RectTransform face, string name, int slot, Color ring)
        {
            float x, y;
            if (Style == BadgeStyle.FrameEdge)
            {
                x = 300 - 8 - Size - slot * (Size + 4);
                y = 78 - Size * 0.5f; // straddles the top edge of the art window
            }
            else
            {
                x = 300 - 16 - 10 - Size - slot * (Size + 6);
                y = 78 + 10;
            }
            var holder = UI.Place(UI.Rect(name, face), x - 4, y - 4, Size + 8, Size + 8);
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.alpha = Opacity;
            group.blocksRaycasts = false;
            group.interactable = false;
            Quiet(Theme.RoundedPanel(holder, ring, 1, 1, Size + 6, Size + 6, (Size + 6) / 2, "Ring"));
            Quiet(Theme.RoundedPanel(holder, new Color(0.04f, 0.05f, 0.14f, 0.95f), 4, 4, Size, Size, Size / 2, "Disc"));
            return holder;
        }

        static void Glyph(RectTransform holder, Sprite sprite, Color color, float pad)
        {
            var glyph = UI.Panel(holder, color, 4 + pad, 4 + pad, Size - 2 * pad, Size - 2 * pad, "Glyph");
            glyph.sprite = sprite;
            glyph.preserveAspect = true;
            glyph.raycastTarget = false;
        }

        /// <summary>
        /// Splits a card's text the same way for every card type: <c>abilities</c> holds the trigger lines
        /// ("Start Turn: ...", "When Played: ...", "While In Play: ..."), shown under ABILITIES with their icons;
        /// <c>info</c> holds the rest (targets, how weapons and Essence work). Legends' info (attacks, stats) is drawn elsewhere.
        /// </summary>
        public static (string abilities, string info) SplitCardText(Card card)
        {
            string text = card is BattleCard ? card.Description : card.EffectDescription;
            var abilities = new List<string>();
            var rest = new List<string>();
            foreach (string raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.EndsWith("N/A")) continue;
                (LineTrigger(line) != null ? abilities : rest).Add(line);
            }

            switch (card)
            {
                case StageCard _:
                    rest.Add("Stages affect both players. Playing a new stage replaces the current one.");
                    break;
                case EssenceCard _:
                    rest.Add("You may play one Essence card per turn. Decks need at least 10.");
                    break;
                case WeaponCard weapon:
                    string w = weapon.Weapon == Weapon.RocketLance ? "Rocket Lance" : weapon.Weapon == Weapon.BattleBoots ? "Battle Boots" : weapon.Weapon.ToString();
                    rest.Add(card.Description + ".");
                    rest.Add($"It counts towards attacks that need a {w} (and attacks that need any weapon). Matching the legend's element adds bonus damage.");
                    break;
                case BattleCard battle:
                    rest.Add(!battle.TargetRequired ? "No target needed." : battle.FriendlyFire ? "Target: one of your legends." : "Target: an enemy legend.");
                    rest.Add(battle.Stackable ? "Stays attached to the legend." : "Discarded after use.");
                    break;
            }
            return (string.Join("\n", abilities), string.Join("\n", rest));
        }

        /// <summary>The trigger a text line starts with ("Start Turn: ...", "When Played: ...", "While In Play: ..."), if any.</summary>
        public static Trigger? LineTrigger(string line)
        {
            line = line.TrimStart();
            if (line.StartsWith("Start Turn:", StringComparison.OrdinalIgnoreCase)) return Trigger.StartOfTurn;
            if (line.StartsWith("When Played:", StringComparison.OrdinalIgnoreCase)) return Trigger.WhenPlayed;
            if (line.StartsWith("While In Play:", StringComparison.OrdinalIgnoreCase)) return Trigger.WhileInPlay;
            return null;
        }

        /// <summary>
        /// Effect text with the matching badge in front of every trigger line. Lines without a trigger are lined up with
        /// the text next to the icons; an empty line adds a small gap. The font shrinks from <paramref name="maxSize"/>
        /// towards <paramref name="minSize"/> until it fits in <paramref name="h"/>. Returns the height used.
        /// </summary>
        public static float PaintAbilities(Transform parent, string text, float x, float y, float w, float h, int maxSize, int minSize, Color color)
        {
            string[] lines = (text ?? "").Split('\n');
            bool anyIcon = false;
            foreach (string l in lines) anyIcon |= LineTrigger(l) != null;

            for (int size = maxSize; ; size--)
            {
                var block = UI.Place(UI.Rect("Abilities", parent), x, y, w, h);
                float icon = Mathf.Round(size * 1.3f), indent = anyIcon ? icon + 8 : 0, cy = 0;
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0) { cy += size * 0.6f; continue; }
                    var label = Theme.Label(block, line, size, color, indent, cy, w - indent, size * 2);
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    float lh = Mathf.Max(label.preferredHeight, size * 1.2f);
                    ((RectTransform)label.transform).sizeDelta = new Vector2(w - indent, lh);
                    Trigger? t = LineTrigger(line);
                    if (t != null) MiniBadge(block, t.Value, 0, cy + (size * 1.2f - icon) / 2, icon);
                    cy += lh + size * 0.25f;
                }
                if (cy <= h + 1 || size <= minSize) return Mathf.Min(cy, h);
                UnityEngine.Object.DestroyImmediate(block.gameObject); // too tall: try a smaller font
            }
        }

        /// <summary>Inline version of the "can't deal damage" badge (<see cref="PaintNoStrikes"/>), for use next to text.</summary>
        public static void MiniNoStrikes(Transform parent, float x, float y, float size)
        {
            var holder = MiniDisc(parent, "Icon NoStrikes", NoStrikesColor, x, y, size);
            float ring = Mathf.Max(2, size * 0.09f), disc = size - 2 * ring;
            float pad = ring + disc * 0.12f;
            var sword = UI.Panel(holder, new Color32(238, 232, 240, 255), pad, pad, size - 2 * pad, size - 2 * pad, "Glyph");
            sword.sprite = _sword ??= Glyph(SwordCut);
            sword.preserveAspect = true;
            sword.raycastTarget = false;
            var slash = UI.Panel(holder, NoStrikesColor, ring, ring, disc, disc, "Slash");
            slash.sprite = _slash ??= Glyph(Slash);
            slash.preserveAspect = true;
            slash.raycastTarget = false;
        }

        /// <summary>A small round badge (coloured ring, dark disc, glyph) for inline use next to text.</summary>
        public static void MiniBadge(Transform parent, Trigger t, float x, float y, float size)
        {
            Color c = ColorFor(t);
            var holder = UI.Place(UI.Rect("Icon " + t, parent), x, y, size, size);
            float ring = Mathf.Max(2, size * 0.09f);
            Quiet(Theme.RoundedPanel(holder, c, 0, 0, size, size, size / 2, "Ring"));
            Quiet(Theme.RoundedPanel(holder, new Color(0.04f, 0.05f, 0.14f, 0.95f), ring, ring, size - 2 * ring, size - 2 * ring, (size - 2 * ring) / 2, "Disc"));
            float pad = size * 0.24f;
            var glyph = UI.Panel(holder, c, pad, pad, size - 2 * pad, size - 2 * pad, "Glyph");
            glyph.sprite = GlyphFor(t);
            glyph.preserveAspect = true;
            glyph.raycastTarget = false;
        }

        static Image Quiet(Image img)
        {
            img.raycastTarget = false;
            return img;
        }

        // ───────────── Glyphs, drawn once into small textures ─────────────

        static Sprite _bolt, _infinity, _cycle, _sword, _slash, _hammer, _fullSword, _heart, _flame, _solidFlame, _stunned;

        // No domain reload on Play: the glyph sprites from the previous run are destroyed, so make them again.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() =>
            _bolt = _infinity = _cycle = _sword = _slash = _hammer = _fullSword = _heart = _flame = _solidFlame = _stunned = null;

        /// <summary>Whole sword, tip top-right (ATK changes).</summary>
        static Sprite FullSword => _fullSword ??= Glyph((x, y) => { Vector2 p = SwordSpace(x, y); return Sword(p.x, p.y); });

        /// <summary>Heart (max HP changes): the classic (u² + v² − 1)³ − u²v³ ≤ 0 curve.</summary>
        static Sprite Heart => _heart ??= Glyph((x, y) =>
        {
            float u = (x - 0.5f) * 2.5f, v = (0.5f - y) * 2.5f + 0.1f;
            float a = u * u + v * v - 1;
            return a * a * a - u * u * v * v * v <= 0;
        });

        /// <summary>Flame (burned): a teardrop that flickers to a point at the top, with a hollow core.</summary>
        static Sprite Flame => _flame ??= Glyph((x, y) => FlameDrop(0.5f, 0.66f, 0.28f, 0.04f, x, y) && !FlameDrop(0.5f, 0.76f, 0.12f, 0.46f, x, y));

        /// <summary>The same flame without the hollow core, so a number fits in its round base (weapon burns).</summary>
        static Sprite SolidFlame => _solidFlame ??= Glyph((x, y) => FlameDrop(0.5f, 0.66f, 0.28f, 0.04f, x, y));

        static bool FlameDrop(float cx, float cy, float r, float top, float px, float py)
        {
            if (Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) < r) return true;
            if (py < top || py > cy) return false;
            float t = (py - top) / (cy - top);
            float bend = 0.07f * Mathf.Sin(t * Mathf.PI) * (1 - t); // tip leans right
            return Mathf.Abs(px - cx - bend) < r * Mathf.Pow(t, 0.75f);
        }

        /// <summary>How many weapons an attack burns: a solid orange flame with the number in its round base.</summary>
        public static void BurnCount(Transform parent, int amount, float x, float y, float size)
        {
            var holder = UI.Place(UI.Rect("Burn", parent), x, y, size, size);
            var flame = UI.Panel(holder, FireColor, 0, 0, size, size, "Flame");
            flame.sprite = SolidFlame;
            flame.preserveAspect = true;
            flame.raycastTarget = false;
            var label = Theme.Label(holder, amount.ToString(), Mathf.RoundToInt(size * 0.46f), Theme.Ink, 0, size * 0.41f, size, size * 0.5f, TextAnchor.MiddleCenter, display: true);
            label.raycastTarget = false;
        }

        /// <summary>Stunned: a card outline turned onto its side.</summary>
        static Sprite StunnedCard => _stunned ??= Glyph((x, y) =>
        {
            const float c = 0.34202014f, s = 0.93969262f; // turned 70°, so it reads as turned rather than just wide
            float dx = x - 0.5f, dy = y - 0.5f;
            float u = Mathf.Abs(dx * c + dy * s), v = Mathf.Abs(-dx * s + dy * c);
            const float hw = 0.25f, hh = 0.34f, line = 0.085f;
            bool outer = u < hw && v < hh, inner = u < hw - line && v < hh - line;
            bool art = u < hw - line * 2 && v < hh - line * 2; // filled "art" window so it reads as a card
            return outer && !inner || art;
        });

        // ───────────── Crafted cards: copper hammer ─────────────

        public static readonly Color CraftedColor = new Color32(236, 138, 70, 255);
        public static readonly Color CraftedFill = new Color32(58, 30, 16, 255);

        /// <summary>Hammer turned 45° (head top-right), white: tint it.</summary>
        public static Sprite Hammer => _hammer ??= Glyph((x, y) =>
        {
            Vector2 p = SwordSpace(x, y); // same 45° turn as the sword
            bool head = p.x > 0.18f && p.x < 0.82f && p.y > 0.08f && p.y < 0.36f && !(p.x > 0.70f && p.y < 0.16f);
            bool handle = Mathf.Abs(p.x - 0.5f) < 0.075f && p.y >= 0.36f && p.y < 0.94f;
            return head || handle;
        });

        /// <summary>Card detail: "CRAFTED BY" and the source on a copper chip. Returns the bottom edge.</summary>
        public static float PaintCraftedChip(Transform parent, string source, float x, float y, float w)
        {
            const float h = 48;
            var chip = Quiet(Theme.RoundedPanel(parent, CraftedFill, x, y, w, h, 14, "Crafted"));
            Quiet(Theme.RoundedPanel(chip.transform, CraftedColor, 0, 0, w, h, 14, "Line", outline: true));
            var icon = UI.Panel(chip.transform, CraftedColor, 12, 11, 26, 26, "Hammer");
            icon.sprite = Hammer;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Theme.Label(chip.transform, "CRAFTED BY", 12, CraftedColor, 48, 5, w - 58, 16, TextAnchor.MiddleLeft, display: true);
            var label = Theme.Label(chip.transform, source, 15, Theme.Text, 48, 21, w - 58, 22, TextAnchor.MiddleLeft);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 15;
            return y + h;
        }

        public static Sprite GlyphFor(Trigger t) =>
            t == Trigger.WhenPlayed ? (_bolt ??= Glyph(Bolt)) :
            t == Trigger.WhileInPlay ? (_infinity ??= Glyph(Infinity)) :
            (_cycle ??= Glyph(Cycle));

        static readonly float[] BoltShape = { 0.58f, 0.06f, 0.22f, 0.56f, 0.46f, 0.56f, 0.36f, 0.94f, 0.80f, 0.42f, 0.55f, 0.42f, 0.70f, 0.06f };
        static readonly float[] ArrowHead = { 0.60f, 0.10f, 0.95f, 0.30f, 0.66f, 0.47f };

        static bool Bolt(float x, float y) => InPolygon(x, y, BoltShape);

        static bool Infinity(float x, float y)
        {
            float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(0.31f, 0.5f));
            float d2 = Vector2.Distance(new Vector2(x, y), new Vector2(0.69f, 0.5f));
            return Mathf.Abs(d1 - 0.17f) < 0.065f || Mathf.Abs(d2 - 0.17f) < 0.065f;
        }

        static bool Cycle(float x, float y)
        {
            var v = new Vector2(x - 0.5f, y - 0.5f);
            float a = Mathf.Atan2(-v.y, v.x) * Mathf.Rad2Deg; // 0 = right, positive = up
            bool ring = Mathf.Abs(v.magnitude - 0.3f) < 0.08f && !(a > -10 && a < 55);
            return ring || InPolygon(x, y, ArrowHead);
        }

        /// <summary>Maps a point into sword space: the sword stands upright (tip up), turned 45° so the tip points top-right.</summary>
        static Vector2 SwordSpace(float x, float y)
        {
            const float c = 0.70710678f;
            float dx = x - 0.5f, dy = y - 0.5f;
            return new Vector2(dx * c + dy * c + 0.5f, -dx * c + dy * c + 0.5f);
        }

        static bool Sword(float u, float v)
        {
            float a = Mathf.Abs(u - 0.5f);
            if (v > 0.03f && v < 0.17f && a < 0.095f * (v - 0.03f) / 0.14f) return true; // tip
            if (v >= 0.17f && v < 0.64f && a < 0.095f) return true;                       // blade
            if (v >= 0.64f && v < 0.73f && a < 0.24f) return true;                        // guard
            if (v >= 0.73f && v < 0.87f && a < 0.055f) return true;                       // grip
            return Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.915f)) < 0.07f; // pommel
        }

        // The bar runs across the sword (top-left to bottom-right); the sword is cut away around it so the two read apart.
        static bool Bar(float u, float v, float half) => Mathf.Abs(v - 0.47f) < half && Mathf.Abs(u - 0.5f) < 0.46f;

        static bool SwordCut(float x, float y) { Vector2 p = SwordSpace(x, y); return Sword(p.x, p.y) && !Bar(p.x, p.y, 0.13f); }
        static bool Slash(float x, float y) { Vector2 p = SwordSpace(x, y); return Bar(p.x, p.y, 0.068f); }

        /// <summary>Renders a white shape (x, y in 0..1, y down) with 4x4 supersampling.</summary>
        static Sprite Glyph(Func<float, float, bool> inside)
        {
            const int n = 96, ss = 4;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                            if (inside((x + (sx + 0.5f) / ss) / n, 1 - (y + (sy + 0.5f) / ss) / n)) hits++;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(255 * hits / (ss * ss)));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        static bool InPolygon(float x, float y, float[] p)
        {
            bool c = false;
            for (int i = 0, j = p.Length / 2 - 1; i < p.Length / 2; j = i++)
            {
                float xi = p[2 * i], yi = p[2 * i + 1], xj = p[2 * j], yj = p[2 * j + 1];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) c = !c;
            }
            return c;
        }
    }
}
