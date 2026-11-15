using System;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    public enum BoardZone
    {
        Deck,
        Hand,
        DiscardPile,
        Stage,
        PlayingField,
        EssenceField,
        Stacked,
        Preview,
    }

    /// <summary>
    /// A card (the WinForms CardControl): rendering, clicking and dragging.
    /// The card occupies 150x200 layout units; its face is drawn at 2x (300x400) and scaled down so it stays crisp
    /// when shown large (showcase, previews).
    /// </summary>
    public sealed class CardView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float W = 150, H = 200;
        const float FW = 300, FH = 400; // face design size

        public Card Card { get; private set; }
        public bool FaceUp { get; private set; }
        public int PlayerIndex;
        public BoardZone Zone;
        public int Index;

        public Func<CardView, bool> CanDrag;
        /// <summary>Called with the screen position where the card was released.</summary>
        public Action<CardView, Vector2> Dropped;
        public Action<CardView> Clicked;
        public Action<CardView> DragStarted, DragEnded;
        public RectTransform DragLayer;

        GameObject _highlight;
        ScrollRect _forwardScroll; // non-draggable cards inside a scroll view pass drags to it
        Image _glow, _ring;
        RectTransform _face;
        int _statusCount;
        CanvasGroup _group;
        bool _dragging;
        Transform _originalParent;
        int _originalSibling;
        Vector3 _originalLocalPosition;
        Vector2 _grabOffset;

        public RectTransform Rect => (RectTransform)transform;

        public static CardView Create(Transform parent, Card card, bool faceUp, float x, float y, float scale = 1f,
            bool tapped = false)
        {
            var rt = UI.Place(UI.Rect(card != null ? card.Name : "Card", parent), x, y, W, H);
            rt.localScale = new Vector3(scale, scale, 1);
            var view = rt.gameObject.AddComponent<CardView>();
            view.Card = card;
            view.FaceUp = faceUp && card != null;
            view._group = rt.gameObject.AddComponent<CanvasGroup>();

            // Highlight (targeting) sits behind the card.
            view._highlight = UI.Rect("Highlight", rt).gameObject;
            UI.Place((RectTransform)view._highlight.transform, -22, -22, W + 44, H + 44);
            view._glow = view._highlight.AddComponent<Image>();
            view._glow.sprite = Theme.Soft;
            view._glow.raycastTarget = false;
            view._ring = Theme.RoundedPanel(rt, Theme.Gold, -4, -4, W + 8, H + 8, 13, "HighlightRing", outline: true);
            view._ring.raycastTarget = false;
            view._ring.transform.SetParent(view._highlight.transform, true);
            view._highlight.SetActive(false);

            var face = UI.Place(UI.Rect("Face", rt), 0, 0, FW, FH);
            face.localScale = new Vector3(W / FW, H / FH, 1);
            view._face = face;
            if (view.FaceUp) CardArt.PaintFront(face, card, detailed: scale >= 1.3f, tapped: tapped);
            else CardArt.PaintBack(face, tapped);
            return view;
        }

        /// <summary>Gold targeting highlight.</summary>
        public void SetHighlight(bool on) => SetGlow(on ? Theme.Gold : (Color?)null);

        /// <summary>Coloured glow + ring around the card (null removes it).</summary>
        public void SetGlow(Color? color, float strength = 0.95f)
        {
            if (_highlight == null) return;
            _highlight.SetActive(color != null);
            if (color == null) return;
            Color c = color.Value;
            _glow.color = new Color(c.r, c.g, c.b, strength);
            _ring.color = c;
        }

        /// <summary>Darkens the card to show it is used up for this turn. Call before <see cref="AddStatus"/> so tags stay bright.</summary>
        public void SetDimmed(bool on)
        {
            if (_face == null) return;
            Transform old = _face.Find("Dim");
            if (old != null) Destroy(old.gameObject);
            if (!on) return;
            var dim = Theme.RoundedPanel(_face, new Color(0.02f, 0.03f, 0.09f, 0.74f), 0, 0, FW, FH, 22, "Dim");
            dim.raycastTarget = false;
        }

        /// <summary>Small status tag stacked from the bottom-right of the art window ("ATTACKED", "NO STRIKES"...).</summary>
        public void AddStatus(string text, Color background, Color textColor)
        {
            if (_face == null || !FaceUp) return;
            float w = 24 + text.Length * 11;
            float y = 78 + 190 - 40 - _statusCount * 36;
            var tag = Theme.RoundedPanel(_face, background, 300 - 26 - w, y, w, 30, 15, "Status " + text);
            tag.raycastTarget = false;
            Theme.Label(tag.transform, text, 16, textColor, 0, 0, w, 30, TextAnchor.MiddleCenter, display: true);
            _statusCount++;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // uGUI sends the click before OnEndDrag when the pointer moved; ignore clicks that end a drag.
            if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging) return;
            Clicked?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = false;
            _forwardScroll = null;
            if (eventData.button != PointerEventData.InputButton.Left || CanDrag == null || !CanDrag(this) || DragLayer == null)
            {
                _forwardScroll = GetComponentInParent<ScrollRect>();
                if (_forwardScroll != null) _forwardScroll.OnBeginDrag(eventData);
                return;
            }
            _dragging = true;
            var flight = GetComponent<CardFlight>();
            if (flight != null) flight.Complete();
            _originalParent = transform.parent;
            _originalSibling = transform.GetSiblingIndex();
            _originalLocalPosition = transform.localPosition;
            transform.SetParent(DragLayer, true);
            transform.SetAsLastSibling();
            DragStarted?.Invoke(this);
            _group.blocksRaycasts = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(DragLayer, eventData.position, eventData.pressEventCamera, out Vector2 local);
            _grabOffset = (Vector2)transform.localPosition - local;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_forwardScroll != null) { _forwardScroll.OnDrag(eventData); return; }
            if (!_dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(DragLayer, eventData.position, eventData.pressEventCamera, out Vector2 local);
            transform.localPosition = local + _grabOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_forwardScroll != null)
            {
                _forwardScroll.OnEndDrag(eventData);
                _forwardScroll = null;
                return;
            }
            if (!_dragging) return;
            _dragging = false;
            _group.blocksRaycasts = true;
            // Snap back first; a successful play rebuilds the board anyway.
            transform.SetParent(_originalParent, false);
            transform.SetSiblingIndex(_originalSibling);
            transform.localPosition = _originalLocalPosition;
            DragEnded?.Invoke(this);
            Dropped?.Invoke(this, eventData.position);
        }
    }

    /// <summary>Paints card faces and backs (design space 300x400).</summary>
    public static class CardArt
    {
        const float FW = 300, FH = 400;

        static readonly Color GemFill = new Color32(58, 46, 150, 255);
        static readonly Color Buff = new Color32(140, 255, 160, 255);
        static readonly Color Debuff = new Color32(255, 150, 150, 255);

        public static Color Accent(Element e) => Color.Lerp(CardStyle.FrameColor(e), Color.white, 0.35f);

        public static void PaintFront(RectTransform face, Card card, bool detailed = true, bool tapped = false)
        {
            Color elem = CardStyle.FrameColor(card.Element);
            Color rim = Color.Lerp(elem, Color.white, 0.18f);
            Color body = Color.Lerp(elem, Theme.Night, 0.66f);
            Color accent = Accent(card.Element);

            // Drop shadow
            var shadow = UI.Panel(face, new Color(0, 0, 0, 0.6f), -24, -10, FW + 48, FH + 48, "Shadow");
            shadow.sprite = Theme.Soft;
            shadow.raycastTarget = false;

            // Rim (receives pointer events), rim sheen, body, body glow
            Theme.RoundedPanel(face, rim, 0, 0, FW, FH, 22, "Frame");
            // Body is a mask so the glow and sheen follow its rounded corners.
            var bodyPanel = Quiet(Theme.RoundedPanel(face, body, 7, 7, FW - 14, FH - 14, 17, "Body"));
            bodyPanel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var bodyGlow = UI.Panel(bodyPanel.transform, new Color(elem.r, elem.g, elem.b, 0.6f), 0, 0, FW - 14, 240, "BodyGlow");
            bodyGlow.sprite = Theme.Gradient(false, 0f, 1f);
            bodyGlow.raycastTarget = false;
            var sheen = UI.Panel(bodyPanel.transform, Color.white, 0, 0, FW - 14, 120, "Sheen");
            sheen.sprite = Theme.Gradient(false, 0f, 0.14f);
            sheen.raycastTarget = false;
            Quiet(Theme.RoundedPanel(face, new Color(accent.r, accent.g, accent.b, 0.5f), 7, 7, FW - 14, FH - 14, 17, "BodyLine", outline: true));

            bool coverArt = card is LegendCard || card is StageCard;
            PaintArtWindow(face, card, elem, accent, coverArt);
            PaintName(face, card);
            PaintCost(face, card.Cost);
            PaintTypePill(face, card, body, accent);

            if (card is LegendCard legend) PaintLegendFooter(face, legend, accent, detailed);
            else PaintTextBox(face, card, accent);
            TriggerIcons.Paint(face, card); // ability badges: when played / while in play / start of turn
            if (tapped) PaintTappedOverlay(face);
        }

        static void PaintArtWindow(RectTransform face, Card card, Color elem, Color accent, bool cover)
        {
            const float x = 16, y = 78, w = FW - 32, h = 190;
            var window = Theme.RoundedPanel(face, Color.Lerp(elem, Theme.Night, 0.45f), x, y, w, h, 12, "ArtWindow");
            window.raycastTarget = false;
            window.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            Texture2D art = Art.Get(card.Art);
            if (cover)
            {
                UI.Picture(window.transform, art, 0, 0, w, h, cover: true);
            }
            else
            {
                var glow = UI.Panel(window.transform, new Color(accent.r, accent.g, accent.b, 0.7f), w / 2 - 150, h / 2 - 150, 300, 300, "Glow");
                glow.sprite = Theme.Soft;
                glow.raycastTarget = false;
                UI.Picture(window.transform, art, 20, 12, w - 40, h - 24, cover: false);
            }
            // Fade the bottom of the art into the card body.
            var fade = UI.Panel(window.transform, Theme.Night, 0, h - 70, w, 70, "Fade");
            fade.sprite = Theme.Gradient(false, 0.85f, 0f);
            fade.raycastTarget = false;
            Quiet(Theme.RoundedPanel(face, new Color(accent.r, accent.g, accent.b, 0.85f), x, y, w, h, 12, "ArtLine", outline: true));

            if (card is LegendCard legend && legend.IsBurned)
            {
                // Below the ability badges when the card has them.
                float by = y + (TriggerIcons.Any(card) ? 30 : 10);
                var burn = Theme.RoundedPanel(face, new Color32(255, 120, 30, 240), FW - 112, by, 86, 30, 15, "Burned");
                burn.raycastTarget = false;
                Theme.Label(burn.transform, "BURNED", 16, Theme.Ink, 0, 0, 86, 30, TextAnchor.MiddleCenter, display: true);
            }
        }

        static void PaintName(RectTransform face, Card card)
        {
            var name = Theme.Label(face, card.Name, 30, Theme.Text, 84, 18, FW - 102, 50, TextAnchor.MiddleLeft, display: true);
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 14;
            name.resizeTextMaxSize = 30;
            Theme.AddShadow(name, 2, 0.7f);
        }

        static void PaintCost(RectTransform face, int cost)
        {
            var ring = Theme.RoundedPanel(face, Theme.Gold, 12, 12, 64, 64, 32, "CostGem");
            ring.raycastTarget = false;
            var fill = Theme.RoundedPanel(ring.transform, GemFill, 5, 5, 54, 54, 27, "Fill");
            fill.raycastTarget = false;
            fill.gameObject.AddComponent<Mask>().showMaskGraphic = true; // keep the shine inside the circle
            var shine = UI.Panel(fill.transform, Color.white, 0, 0, 54, 27, "Shine");
            shine.sprite = Theme.Gradient(false, 0f, 0.35f);
            shine.raycastTarget = false;
            var t = Theme.Label(ring.transform, cost.ToString(), 36, Color.white, 0, 0, 64, 64, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(t, 2, 0.8f);
        }

        static void PaintTypePill(RectTransform face, Card card, Color body, Color accent)
        {
            string type = card.Type.ToString().ToUpperInvariant();
            string text = card is WeaponCard w ? $"{type}  ·  {Pretty(w.Weapon)}" : $"{type}  ·  {card.Element.ToString().ToUpperInvariant()}";
            const float pw = 210, ph = 30;
            var pill = Theme.RoundedPanel(face, body, (FW - pw) / 2, 254, pw, ph, 15, "TypePill");
            pill.raycastTarget = false;
            Quiet(Theme.RoundedPanel(pill.transform, accent, 0, 0, pw, ph, 15, "Line", outline: true));
            var label = Theme.Label(pill.transform, text, 16, accent, 0, 0, pw, ph, TextAnchor.MiddleCenter, display: true);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 16;
        }

        static readonly Color Damaged = new Color32(255, 214, 120, 255);

        static Color AtkNumberColor(LegendCard l, Color normal) =>
            l.Power > l.PrintedPower ? Buff : l.Power < l.PrintedPower ? Debuff : normal;

        static Color HpNumberColor(LegendCard l, Color normal) =>
            l.CurrentHP < l.BaseHealth ? Damaged : l.BaseHealth > l.PrintedHealth ? Buff : normal;

        /// <summary>Legend footer: weapon icons, attack names (large cards) and the ATK / HP stat bar.</summary>
        static void PaintLegendFooter(RectTransform face, LegendCard legend, Color accent, bool detailed)
        {
            WeaponChipsAndAttacks(face, legend, accent, detailed, 38, 286, 50, 292, new Rect(24, 326, FW - 48, 20));
            PaintStatBar(face, legend, accent);
        }

        static void WeaponChipsAndAttacks(RectTransform face, LegendCard legend, Color accent, bool detailed,
            float chipDetailed, float chipYDetailed, float chipCompact, float chipYCompact, Rect attackBox)
        {
            float chip = detailed ? chipDetailed : chipCompact;
            float chipY = detailed ? chipYDetailed : chipYCompact;
            WeaponChip(face, legend.PrimaryWeapon, FW / 2 - chip - 6, chipY, chip, accent);
            WeaponChip(face, legend.SecondaryWeapon, FW / 2 + 6, chipY, chip, accent);

            // Attack names (only when the card is shown large enough to read them)
            if (!detailed) return;
            string attacks = legend.Attacks.Count == 0 ? "No attacks" : string.Join("  ·  ", legend.Attacks.Select(a => a.Name));
            var atkText = Theme.Label(face, attacks, 15, Theme.TextDim, attackBox.x, attackBox.y, attackBox.width, attackBox.height, TextAnchor.MiddleCenter);
            atkText.verticalOverflow = VerticalWrapMode.Truncate;
            atkText.resizeTextForBestFit = true;
            atkText.resizeTextMinSize = 9;
            atkText.resizeTextMaxSize = 15;
        }

        /// <summary>ATK on the left, HP on the right; the red half drains as the legend takes damage.</summary>
        static void PaintStatBar(RectTransform face, LegendCard legend, Color accent)
        {
            const float bx = 16, by = FH - 54, bw = FW - 32, bh = 40, half = (FW - 32) / 2;
            var bar = Theme.RoundedPanel(face, new Color(0, 0, 0, 0.55f), bx, by, bw, bh, 14, "StatBar");
            bar.raycastTarget = false;
            var clip = Theme.RoundedPanel(bar.transform, Color.white, 0, 0, bw, bh, 14, "Clip");
            clip.raycastTarget = false;
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            // ATK half: warm gradient
            var atkFill = UI.Panel(clip.transform, new Color32(226, 112, 28, 255), 0, 0, half, bh, "AtkFill");
            atkFill.sprite = Theme.Gradient(true, 0.75f, 0.15f);
            atkFill.raycastTarget = false;

            // HP half: red fill that drains with damage
            float ratio = legend.BaseHealth > 0 ? Mathf.Clamp01((float)legend.CurrentHP / legend.BaseHealth) : 0;
            var hpTrack = UI.Panel(clip.transform, new Color(0.5f, 0.05f, 0.1f, 0.35f), half, 0, half, bh, "HpTrack");
            hpTrack.raycastTarget = false;
            var hpFill = UI.Panel(clip.transform, new Color32(214, 40, 62, 255), half, 0, half * ratio, bh, "HpFill");
            hpFill.sprite = Theme.Gradient(true, 0.85f, 0.55f);
            hpFill.raycastTarget = false;
            var gloss = UI.Panel(clip.transform, Color.white, 0, 0, bw, bh / 2, "Gloss");
            gloss.sprite = Theme.Gradient(false, 0f, 0.22f);
            gloss.raycastTarget = false;
            Quiet(UI.Panel(bar.transform, new Color(1, 1, 1, 0.35f), half - 1, 6, 2, bh - 12, "Divider"));
            Quiet(Theme.RoundedPanel(bar.transform, new Color(accent.r, accent.g, accent.b, 0.8f), 0, 0, bw, bh, 14, "Line", outline: true));

            // ATK content
            Texture2D sword = Art.Weapon(Weapon.Sword);
            if (sword != null) UI.Picture(bar.transform, sword, 12, 7, 26, 26, cover: false).color = Color.white;
            var atk = Theme.Label(bar.transform, legend.Power.ToString(), 32, AtkNumberColor(legend, Color.white), 44, 0, 52, bh, TextAnchor.MiddleLeft, display: true);
            Theme.AddOutline(atk, 1.5f, 0.6f);
            Theme.Label(bar.transform, "ATK", 14, new Color(1, 1, 1, 0.75f), half - 46, 0, 40, bh, TextAnchor.MiddleRight, display: true);

            // HP content
            var heart = UI.Panel(bar.transform, Color.white, half + 12, 8, 26, 24, "Heart");
            heart.sprite = Theme.Heart;
            heart.raycastTarget = false;
            bool hurt = legend.CurrentHP < legend.BaseHealth;
            var hp = Theme.Label(bar.transform, legend.CurrentHP.ToString(), 32, HpNumberColor(legend, Color.white), half + 44, 0, 52, bh, TextAnchor.MiddleLeft, display: true);
            Theme.AddOutline(hp, 1.5f, 0.6f);
            Theme.Label(bar.transform, hurt ? "/" + legend.BaseHealth : "HP", hurt ? 18 : 14, new Color(1, 1, 1, 0.8f), bw - 56, 0, 46, bh, TextAnchor.MiddleRight, display: true);
        }

        static void WeaponChip(RectTransform face, Weapon weapon, float x, float y, float size, Color accent)
        {
            var chip = Theme.RoundedPanel(face, new Color(0, 0, 0, 0.45f), x, y, size, size, size / 2, "Weapon " + weapon);
            chip.raycastTarget = false;
            Quiet(Theme.RoundedPanel(chip.transform, new Color(accent.r, accent.g, accent.b, 0.7f), 0, 0, size, size, size / 2, "Line", outline: true));
            Texture2D icon = Art.Weapon(weapon);
            if (icon != null)
            {
                var img = UI.Picture(chip.transform, icon, 7, 7, size - 14, size - 14, cover: false);
                img.color = accent;
            }
            else
            {
                Theme.Label(chip.transform, "ANY", 13, accent, 0, 0, size, size, TextAnchor.MiddleCenter, display: true);
            }
        }

        static void PaintTextBox(RectTransform face, Card card, Color accent)
        {
            const float x = 18, y = 294, w = FW - 36, h = 92;
            var box = Theme.RoundedPanel(face, new Color(0, 0, 0, 0.38f), x, y, w, h, 12, "TextBox");
            box.raycastTarget = false;
            Quiet(Theme.RoundedPanel(box.transform, new Color(accent.r, accent.g, accent.b, 0.35f), 0, 0, w, h, 12, "Line", outline: true));

            float textX = 12;
            if (card is WeaponCard weapon)
            {
                WeaponChip((RectTransform)box.transform, weapon.Weapon, 10, (h - 58) / 2, 58, accent);
                textX = 78;
            }
            // Drop the original "N/A" placeholder lines (hidden effects) and the redundant "When Played:" prefix.
            string text = string.Join("\n", card.Description.Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.EndsWith("N/A") && line != "N/A"))
                .Replace("When Played: ", "");
            if (card is EssenceCard) text = "Each Essence card in play gives you 1 Essence at the start of your turn.";
            if (card is StageCard && string.IsNullOrWhiteSpace(text)) text = "A battlefield with no special effect.";
            var t = Theme.Label(box.transform, text, 18, Theme.Text, textX, 6, w - textX - 10, h - 12, TextAnchor.MiddleCenter);
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 9;
            t.resizeTextMaxSize = 18;
        }

        /// <summary>Tapped legends stay readable: dimmed, with a diagonal TAPPED ribbon.</summary>
        static void PaintTappedOverlay(RectTransform face)
        {
            // Everything is clipped to the card shape so the diagonal ribbon stays inside the frame.
            var clip = Theme.RoundedPanel(face, Color.white, 0, 0, FW, FH, 22, "TappedClip");
            clip.raycastTarget = false;
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Quiet(UI.Panel(clip.transform, new Color(0.02f, 0.03f, 0.1f, 0.55f), 0, 0, FW, FH, "TappedDim"));
            var band = UI.Panel(clip.transform, new Color(0.05f, 0.06f, 0.16f, 0.92f), 0, 0, FW + 80, 62, "TappedBand");
            band.raycastTarget = false;
            band.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            band.rectTransform.anchoredPosition = new Vector2(FW / 2, -FH / 2 + 20);
            band.rectTransform.localRotation = Quaternion.Euler(0, 0, 14);
            Quiet(UI.Panel(band.transform, Theme.Gold, 0, 0, FW + 80, 3, "Top"));
            Quiet(UI.Panel(band.transform, Theme.Gold, 0, 59, FW + 80, 3, "Bottom"));
            var label = Theme.Label(band.transform, "TAPPED", 36, Theme.Gold, 0, 0, FW + 80, 62, TextAnchor.MiddleCenter, display: true);
            UI.Stretch(label.rectTransform);
        }

        public static void PaintBack(RectTransform face, bool tapped)
        {
            Color rim = Theme.GoldDeep;
            Color body = new Color32(20, 24, 58, 255);

            var shadow = UI.Panel(face, new Color(0, 0, 0, 0.55f), -24, -10, FW + 48, FH + 48, "Shadow");
            shadow.sprite = Theme.Soft;
            shadow.raycastTarget = false;
            Theme.RoundedPanel(face, rim, 0, 0, FW, FH, 22, "Frame"); // receives pointer events
            Quiet(Theme.RoundedPanel(face, body, 7, 7, FW - 14, FH - 14, 17, "Body"));
            var glow = UI.Panel(face, new Color(0.45f, 0.35f, 1f, 0.55f), 0, 50, FW, FW, "Glow");
            glow.sprite = Theme.Soft;
            glow.raycastTarget = false;
            Quiet(Theme.RoundedPanel(face, new Color(1f, 0.8f, 0.35f, 0.35f), 20, 20, FW - 40, FH - 40, 12, "Inset", outline: true));
            Quiet(Theme.RoundedPanel(face, new Color(1f, 0.8f, 0.35f, 0.18f), 30, 30, FW - 60, FH - 60, 8, "Inset2", outline: true));
            var sheen = UI.Panel(face, Color.white, 7, 7, FW - 14, 180, "Sheen");
            sheen.sprite = Theme.Gradient(false, 0f, 0.12f);
            sheen.raycastTarget = false;

            UI.Picture(face, Art.CardBack, 70, 96, 160, 160, cover: false).raycastTarget = false;
            var title = Theme.Label(face, "BRAWL<color=#F5C04A>TCG</color>", 30, Theme.Text, 0, 286, FW, 40, TextAnchor.MiddleCenter, display: true);
            title.supportRichText = true;

            if (tapped)
            {
                var band = UI.Panel(face, new Color(0.05f, 0.05f, 0.1f, 0.88f), -10, FH / 2 - 30, FW + 20, 60, "Tapped");
                band.raycastTarget = false;
                band.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                band.rectTransform.anchoredPosition = new Vector2(FW / 2, -FH / 2);
                band.rectTransform.localRotation = Quaternion.Euler(0, 0, 12);
                var label = Theme.Label(band.transform, "TAPPED", 34, Theme.Gold, 0, 0, FW + 20, 60, TextAnchor.MiddleCenter, display: true);
                UI.Stretch(label.rectTransform);
            }
        }

        static string Pretty(Weapon w)
        {
            switch (w)
            {
                case Weapon.RocketLance: return "ROCKET LANCE";
                case Weapon.BattleBoots: return "BATTLE BOOTS";
                default: return w.ToString().ToUpperInvariant();
            }
        }

        static Image Quiet(Image img)
        {
            img.raycastTarget = false;
            return img;
        }
    }
}
