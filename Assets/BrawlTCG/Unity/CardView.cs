using System;
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

    /// <summary>A 150x200 card (the WinForms CardControl): painting, clicking and dragging.</summary>
    public sealed class CardView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float W = 150, H = 200;

        public Card Card { get; private set; }
        public bool FaceUp { get; private set; }
        public int PlayerIndex;
        public BoardZone Zone;
        public int Index;

        public Func<CardView, bool> CanDrag;
        /// <summary>Called with the screen position where the card was released.</summary>
        public Action<CardView, Vector2> Dropped;
        public Action<CardView> Clicked;
        public RectTransform DragLayer;

        Image _highlight;
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
            view.FaceUp = faceUp && !tapped && card != null;
            view._group = rt.gameObject.AddComponent<CanvasGroup>();
            view.Paint(tapped);
            return view;
        }

        void Paint(bool tapped)
        {
            Transform t = transform;
            _highlight = UI.Panel(t, UI.Highlight, -6, -6, W + 12, H + 12, "Highlight");
            _highlight.raycastTarget = false;
            _highlight.enabled = false;

            UI.Panel(t, Color.black, 0, 0, W, H, "Border"); // receives the pointer events

            if (!FaceUp)
            {
                var back = UI.Panel(t, UI.CardBack, 3, 3, W - 6, H - 6, "Back");
                back.raycastTarget = false;
                UI.Picture(t, Art.CardBack, 10, 30, W - 20, H - 60, cover: false);
                if (tapped)
                {
                    var tag = UI.Panel(t, new Color(0, 0, 0, 0.7f), 10, 84, W - 20, 30, "Tapped");
                    tag.raycastTarget = false;
                    UI.Label(tag.transform, "TAPPED", 18, Color.white, 0, 0, W - 20, 30, TextAnchor.MiddleCenter);
                }
                return;
            }

            Card card = Card;
            Color textColor = CardStyle.TextColor(card.Element);
            var inner = UI.Panel(t, CardStyle.CardColor(card.Element), 3, 3, W - 6, H - 6, "Face");
            inner.raycastTarget = false;

            if (card is LegendCard legend)
            {
                UI.Picture(t, Art.Get(card.Art), 10, 31, 130, 97, cover: true);
                UI.Label(t, $"HP {legend.CurrentHP}/{legend.BaseHealth}", 16, textColor, 5, 129, 140, 22);
                UI.Label(t, $"Att {legend.Power}", 16, textColor, 5, 152, 100, 22);
                if (legend.IsBurned)
                    UI.Label(t, "BURNED", 13, new Color32(255, 140, 0, 255), 5, 177, 80, 18);
            }
            else if (card is StageCard)
            {
                UI.Picture(t, Art.Get(card.Art), 10, 31, 130, 97, cover: true);
            }
            else
            {
                UI.Picture(t, Art.Get(card.Art), 10, 30, 130, 140, cover: false);
            }
            UI.Label(t, card.Name, 16, textColor, 5, 5, 140, 22, TextAnchor.UpperLeft, FontStyle.Bold, bestFit: true);
            UI.Label(t, card.Cost.ToString(), 16, textColor, 110, 175, 35, 22, TextAnchor.UpperRight);
        }

        public void SetHighlight(bool on)
        {
            if (_highlight != null) _highlight.enabled = on;
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
            if (eventData.button != PointerEventData.InputButton.Left || CanDrag == null || !CanDrag(this) || DragLayer == null)
                return;
            _dragging = true;
            _originalParent = transform.parent;
            _originalSibling = transform.GetSiblingIndex();
            _originalLocalPosition = transform.localPosition;
            transform.SetParent(DragLayer, true);
            transform.SetAsLastSibling();
            _group.blocksRaycasts = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(DragLayer, eventData.position, eventData.pressEventCamera, out Vector2 local);
            _grabOffset = (Vector2)transform.localPosition - local;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(DragLayer, eventData.position, eventData.pressEventCamera, out Vector2 local);
            transform.localPosition = local + _grabOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            _group.blocksRaycasts = true;
            // Snap back first; a successful play rebuilds the board anyway.
            transform.SetParent(_originalParent, false);
            transform.SetSiblingIndex(_originalSibling);
            transform.localPosition = _originalLocalPosition;
            Dropped?.Invoke(this, eventData.position);
        }
    }
}
