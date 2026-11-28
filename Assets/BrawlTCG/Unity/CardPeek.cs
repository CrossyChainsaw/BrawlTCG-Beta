using System.Collections.Generic;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Which other cards a card's text talks about ("Craft Fait, ...", "Set stage to Matrix"), so the detail view
    /// can show them. Effects and attacks are code, so the links are listed here by hand.
    /// </summary>
    public static class CardReferences
    {
        static readonly Dictionary<Attack, int[]> ByAttack = new Dictionary<Attack, int[]>
        {
            [AttackCatalogue.Any_GenerateAndPlayMatrix] = new[] { EffectCatalogue.MatrixId },
            [AttackCatalogue.Ada_SpawnAndPlayAtlantis] = new[] { EffectCatalogue.AtlantisId },
            [AttackCatalogue.Jiro_SpawnAndPlayNinjaSpirit] = new[] { 5003 },
            [AttackCatalogue.Yumiko_GrabOrbs] = new[] { 504 },
            [AttackCatalogue.MasterThief_GrabBomb] = new[] { 500 },
            [AttackCatalogue.PlagueKnight_GrabHealingPotion] = new[] { 501 },
            [AttackCatalogue.Kaya_CraftSnowball] = new[] { 502 },
            [AttackCatalogue.Kaya_BuildASnowman] = new[] { 7000 },
            [AttackCatalogue.Stalker_SummonOwlSpirit] = new[] { 7003 },
            [AttackCatalogue.AstroCommander_Silence] = new[] { EffectCatalogue.SilentGalaxyId },
        };

        /// <summary>Cards mentioned by a card's own text (abilities, stage or battle effects).</summary>
        static readonly Dictionary<int, int[]> ByCard = new Dictionary<int, int[]>
        {
            [2002] = new[] { EffectCatalogue.WorkshopId },   // Ulgrim: when played, the stage becomes The Workshop
            [2003] = new[] { EffectCatalogue.MustafarId },   // Molten Kor: ... Mustafar
            [5002] = new[] { EffectCatalogue.MatrixId },     // Mastermind: ... Matrix
            [518] = new[] { 4004, 3006, 23004, 21000 },      // Witch Party
            [527] = new[] { 6003 },                          // Did Shaw Win Yet?: 2x The Rock
            [7000] = new[] { EffectCatalogue.SnowballId },   // Snowman Kor: when eliminated, craft a Snowball
        };

        public static int[] For(Attack attack) => attack != null && ByAttack.TryGetValue(attack, out int[] ids) ? ids : null;
        public static int[] For(Card card) => card != null && ByCard.TryGetValue(card.Id, out int[] ids) ? ids : null;
    }

    /// <summary>A small card thumbnail; hovering it shows the full card next to the detail panel.</summary>
    public sealed class CardPeek : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        Card _card;
        RectTransform _layer;
        Vector2 _previewAt;
        GameObject _preview;

        /// <summary>
        /// Adds a thumbnail of <paramref name="cardId"/> at (x, y) inside <paramref name="parent"/>. The full-size
        /// preview appears in <paramref name="layer"/> with its top-left corner at <paramref name="previewAt"/>.
        /// </summary>
        public static CardView Create(Transform parent, int cardId, float x, float y, float scale, RectTransform layer, Vector2 previewAt) =>
            Create(parent, CardCatalogue.Create(cardId), x, y, scale, layer, previewAt);

        /// <summary>A thumbnail of this exact card, so match-only details (like the crafted hammer) are kept.</summary>
        public static CardView Create(Transform parent, Card card, float x, float y, float scale, RectTransform layer, Vector2 previewAt)
        {
            CardView thumb = CardView.Create(parent, card, true, x, y, scale);
            thumb.name = "Peek " + card.Name;
            // Only the frame takes the pointer; everything else on the mini card ignores it.
            foreach (Graphic g in thumb.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            var hit = thumb.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var peek = thumb.gameObject.AddComponent<CardPeek>();
            peek._card = card;
            peek._layer = layer;
            peek._previewAt = previewAt;
            return thumb;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            Hide();
            if (_layer == null) return;
            CardView big = CardView.Create(_layer, _card, true, _previewAt.x, _previewAt.y, 1.9f);
            foreach (Graphic g in big.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            big.GetComponent<CanvasGroup>().blocksRaycasts = false;
            big.gameObject.AddComponent<SlideIn>().Offset = new Vector2(16, 0);
            _preview = big.gameObject;
            transform.localScale *= 1.08f;
        }

        public void OnPointerExit(PointerEventData e) => Hide();

        void Hide()
        {
            if (_preview != null)
            {
                Destroy(_preview);
                _preview = null;
                transform.localScale /= 1.08f;
            }
        }

        void OnDisable() => Hide();
        void OnDestroy() { if (_preview != null) Destroy(_preview); }
    }
}
