using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Deck-building showcase: a drawn mouse cursor builds a deck in the real deck builder (filters, search, preview,
    /// ADD, SAVE). The deck is kept in memory only: nothing is written to the player's deck folder.
    /// </summary>
    public sealed partial class DeckBuilderScreen
    {
        DeckClip _showcase;
        RectTransform _cursorLayer, _cursor;
        Vector2 _cursorPos = new Vector2(1180, 760);

        /// <summary>Multiplies cursor travel and waits (lower = snappier).</summary>
        public static float DeckShowcasePace = 0.8f;

        public void BuildShowcase(App app, DeckClip clip)
        {
            _showcase = clip;
            Build(app);
            var blocker = UI.FullPanel(transform, new Color(0, 0, 0, 0), "ShowcaseInputBlocker");
            blocker.raycastTarget = true; // a real mouse must not change the scripted deck
            _cursorLayer = UI.Stretch(UI.Rect("ShowcaseCursorLayer", transform));
            _cursorLayer.pivot = new Vector2(0, 1);
            _cursor = UI.Place(UI.Rect("Cursor", _cursorLayer), 0, 0, 30, 45);
            var img = _cursor.gameObject.AddComponent<Image>();
            img.sprite = CursorSprite();
            img.raycastTarget = false;
            PlaceCursor(_cursorPos);
            StartCoroutine(RunShowcase());
        }

        // ───────────── Script ─────────────

        IEnumerator RunShowcase()
        {
            yield return Wait(0.4f);
            Fx.Banner(_cursorLayer, _showcase.Title, _showcase.Subtitle, Theme.Gold);
            Sfx.Play("turn", 0.7f);
            yield return Wait(2.4f);

            // Name and create the deck, in the same pop-up a player gets (nothing is written to disk).
            yield return ClickOn(Rt(_newDeckButton));
            NamePrompt("NEW DECK", "NAME YOUR DECK", "", "CREATE", _ => null, ShowcaseCreateDeck);
            yield return Wait(0.35f);
            yield return TypeInto(_promptField, _showcase.DeckName);
            yield return ClickOn(Rt(_promptOk));
            SubmitPrompt();
            yield return Wait(0.5f);

            foreach (DeckPick pick in _showcase.Picks)
            {
                GridCell cell = _cells.First(c => c.Card.Id == pick.CardId);
                yield return Filter(pick, cell.Card);
                yield return ScrollTo(cell);
                yield return ClickOn(Rt(cell.Root));
                Select(cell.Card);
                yield return Wait(0.35f);

                yield return MoveTo(Center(Rt(_addButton)));
                for (int i = 0; i < pick.Copies; i++)
                {
                    yield return Tap(Rt(_addButton));
                    AddSelected();
                    if (i == 0 && pick.Callout != null) Callout(cell, pick.Callout);
                    yield return Wait(pick.Copies > 6 ? 0.08f : 0.14f);
                }
                yield return Wait(pick.Callout != null ? 0.5f : 0.2f);
            }

            // Save it.
            yield return ClickOn(Rt(_save));
            ShowcaseSave();
            yield return Wait(0.6f);
            yield return MoveTo(Center(_stats) + new Vector2(60, 40));
            yield return Wait(2.6f);
            ShowcaseDirector.Finished(_showcase);
        }

        void ShowcaseCreateDeck(string name)
        {
            _deckSelector.SetOptions(new[] { name }, name);
            _deckName = name;
            _deck = new List<int>();
            _dirty = false;
            RefreshDeck();
            _app.Toast($"Created {name}. Add at least {DeckRules.MinimumCardsToSave} cards, including {DeckRules.MinimumEssenceToSave} Essence.");
        }

        void ShowcaseSave()
        {
            string error = DeckRules.ValidateForSave(_deck);
            if (error != null) { _app.Toast(error); return; }
            _dirty = false;
            _app.Toast($"{_deckName} saved!");
            RefreshDeck();
        }

        /// <summary>Sets the collection filters the way the pick finds its card, clicking the chips that change.</summary>
        IEnumerator Filter(DeckPick pick, Card card)
        {
            string search = pick.Find == FindBy.Search ? pick.Search : "";
            if ((_search.text ?? "") != search)
            {
                if (search.Length == 0) { _search.text = ""; }
                else yield return TypeInto(_search, search);
            }

            Element? element = pick.Find == FindBy.Element ? card.Element : (Element?)null;
            if (_elementFilter != element)
            {
                var chip = _elementChips.First(c => c.element == element);
                yield return ClickOn(chip.bg.rectTransform);
                _elementFilter = element;
                ApplyFilters();
                yield return Wait(0.25f);
            }

            string type = pick.Find == FindBy.Type ? card.Type.ToString().ToUpperInvariant() : "ALL";
            if (_typeFilter != type)
            {
                var chip = _typeChips.First(c => c.key == type);
                yield return ClickOn(chip.bg.rectTransform);
                _typeFilter = type;
                ApplyFilters();
                yield return Wait(0.25f);
            }
        }

        // ───────────── Cursor ─────────────

        static IEnumerator Wait(float seconds)
        {
            seconds *= DeckShowcasePace;
            if (seconds > 0) yield return new WaitForSecondsRealtime(seconds);
        }

        static RectTransform Rt(Component c) => (RectTransform)c.transform;
        static RectTransform Rt(GameObject go) => (RectTransform)go.transform;

        /// <summary>Centre of a UI element in the 1920x1080 design space (y down), as the cursor layer uses.</summary>
        Vector2 Center(RectTransform rt)
        {
            Vector3 world = rt.TransformPoint(rt.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_cursorLayer, screen, null, out Vector2 local);
            return new Vector2(local.x, -local.y);
        }

        void PlaceCursor(Vector2 p)
        {
            _cursorPos = p;
            _cursor.anchoredPosition = new Vector2(p.x, -p.y);
            _cursor.SetAsLastSibling();
        }

        IEnumerator MoveTo(Vector2 target)
        {
            Vector2 from = _cursorPos;
            // A small offset so the arrow tip lands inside the element, not dead centre every time.
            float distance = Vector2.Distance(from, target);
            float duration = Mathf.Clamp(0.16f + distance / 2600f, 0.16f, 0.5f) * DeckShowcasePace;
            Vector2 bend = Vector2.Perpendicular(target - from).normalized * Mathf.Min(60, distance * 0.08f);
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / duration);
                PlaceCursor(Vector2.Lerp(from, target, k) + bend * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            PlaceCursor(target);
        }

        IEnumerator ClickOn(RectTransform rt)
        {
            yield return MoveTo(Center(rt));
            yield return Wait(0.06f);
            yield return Tap(rt);
        }

        /// <summary>A click where the cursor is: press animation, ring, sound.</summary>
        IEnumerator Tap(RectTransform target)
        {
            Sfx.Play("ui_click", 0.7f, 0.05f, 0.02f);
            Fx.Shockwave(_cursorLayer, _cursorPos, Theme.Gold, 70, 0, 0.3f);
            if (target != null && target.GetComponent<PressPop>() == null) target.gameObject.AddComponent<PressPop>();
            float d = 0.12f;
            for (float t = 0; t < d; t += Time.unscaledDeltaTime)
            {
                _cursor.localScale = Vector3.one * (1 - 0.18f * Mathf.Sin(t / d * Mathf.PI));
                yield return null;
            }
            _cursor.localScale = Vector3.one;
        }

        IEnumerator TypeInto(InputField field, string text)
        {
            yield return ClickOn(Rt(field));
            field.text = "";
            foreach (char ch in text)
            {
                field.text += ch;
                Sfx.Play("ui_hover", 0.35f, 0.15f, 0.01f);
                yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.09f) * DeckShowcasePace);
            }
            yield return Wait(0.25f);
        }

        /// <summary>Scrolls the collection smoothly until the cell sits in the middle of the view.</summary>
        IEnumerator ScrollTo(GridCell cell)
        {
            var content = _gridScroll.content;
            var viewport = _gridScroll.viewport;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float contentH = content.rect.height, viewH = viewport.rect.height;
            if (contentH <= viewH + 1) yield break;
            var rt = Rt(cell.Root);
            Vector3 local = content.InverseTransformPoint(rt.TransformPoint(rt.rect.center));
            float fromTop = content.rect.yMax - local.y;
            float target = 1 - Mathf.Clamp01((fromTop - viewH / 2) / (contentH - viewH));
            float start = _gridScroll.verticalNormalizedPosition;
            if (Mathf.Abs(target - start) < 0.01f) yield break;
            yield return MoveTo(Center(viewport) + new Vector2(260, 120)); // the hand goes to the scroll area first
            float d = Mathf.Clamp(Mathf.Abs(target - start) * 0.9f, 0.25f, 0.6f) * DeckShowcasePace;
            for (float t = 0; t < d; t += Time.unscaledDeltaTime)
            {
                _gridScroll.verticalNormalizedPosition = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t / d));
                yield return null;
            }
            _gridScroll.verticalNormalizedPosition = target;
            _gridScroll.velocity = Vector2.zero;
        }

        void Callout(GridCell cell, string text)
        {
            Vector2 at = Center(Rt(cell.Root));
            Color tint = Fx.Tint(cell.Card.Element);
            Fx.Glow(_cursorLayer, at, new Color(tint.r, tint.g, tint.b, 0.7f), 300, 0, 0.6f);
            Fx.Sparks(_cursorLayer, at, tint, 18, 420);
            Fx.Float(_cursorLayer, text, at + new Vector2(0, -60), Theme.Gold, 30, 0.05f, 60);
            Sfx.Play("buff", 0.6f);
        }

        static Sprite _cursorSprite;

        /// <summary>A classic white arrow pointer with a dark outline, drawn once at runtime.</summary>
        static Sprite CursorSprite()
        {
            if (_cursorSprite != null) return _cursorSprite;
            const int S = 4, W = 20 * S, H = 30 * S;
            Vector2[] poly =
            {
                new Vector2(1, 1), new Vector2(1, 23), new Vector2(6, 18.5f), new Vector2(9.5f, 27),
                new Vector2(13, 25.5f), new Vector2(9.6f, 17.4f), new Vector2(16, 17.4f),
            };
            for (int i = 0; i < poly.Length; i++) poly[i] *= S;

            bool Inside(float x, float y)
            {
                bool inside = false;
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    if ((poly[i].y > y) != (poly[j].y > y) &&
                        x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                        inside = !inside;
                }
                return inside;
            }

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            const int border = 6;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Color32 c = new Color32(0, 0, 0, 0);
                if (Inside(x + 0.5f, y + 0.5f))
                {
                    bool core = true;
                    for (int a = 0; a < 8 && core; a++)
                    {
                        float ang = a * Mathf.PI / 4;
                        core = Inside(x + 0.5f + Mathf.Cos(ang) * border, y + 0.5f + Mathf.Sin(ang) * border);
                    }
                    c = core ? new Color32(255, 255, 255, 255) : new Color32(14, 16, 30, 255);
                }
                px[(H - 1 - y) * W + x] = c; // texture rows run bottom-up
            }
            tex.SetPixels32(px);
            tex.Apply();
            _cursorSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0, 1), 100);
            return _cursorSprite;
        }
    }

    /// <summary>A quick squash on a clicked element, then removes itself.</summary>
    sealed class PressPop : MonoBehaviour
    {
        float _t;
        Vector3 _scale;

        void Start() => _scale = transform.localScale;

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            const float d = 0.16f;
            transform.localScale = _scale * (1 - 0.06f * Mathf.Sin(Mathf.Clamp01(_t / d) * Mathf.PI));
            if (_t >= d)
            {
                transform.localScale = _scale;
                Destroy(this);
            }
        }
    }
}
