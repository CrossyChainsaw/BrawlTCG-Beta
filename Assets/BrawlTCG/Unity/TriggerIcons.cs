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
            int slot = For(card).Count;
            var holder = Badge(face, "NoStrikes", slot, NoStrikesColor);
            Glyph(holder, _sword ??= Glyph(SwordCut), new Color32(238, 232, 240, 255), Size * 0.12f);
            Glyph(holder, _slash ??= Glyph(Slash), NoStrikesColor, 1);

            // A burned legend without ability badges has its BURNED tag just under the badge row: make room.
            if (slot == 0 && face.Find("Burned") is RectTransform burned)
                UI.Place(burned, 300 - 112, 78 + 30, 86, 30);
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

        static Image Quiet(Image img)
        {
            img.raycastTarget = false;
            return img;
        }

        // ───────────── Glyphs, drawn once into small textures ─────────────

        static Sprite _bolt, _infinity, _cycle, _sword, _slash;

        // No domain reload on Play: the glyph sprites from the previous run are destroyed, so make them again.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _bolt = _infinity = _cycle = _sword = _slash = null;

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
