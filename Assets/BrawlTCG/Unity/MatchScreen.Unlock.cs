using System;
using System.Collections.Generic;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Bot games: beating the bot unlocks the premade deck it played (#54). The result screen then offers the reveal,
    /// which takes over the screen, and returns to the result when the player continues.
    /// </summary>
    public sealed partial class MatchScreen
    {
        /// <summary>Bot games: the premade deck the bot plays (null otherwise).</summary>
        string _botDeck;

        /// <summary>
        /// At game over in a bot game: the deck the player just unlocked (saved straight away, before any animation), or
        /// null if they lost or already owned it.
        /// </summary>
        StarterDecks.Starter UnlockBotDeck(bool iWon)
        {
            if (!VsBot || !iWon || _botDeck == null) return null;
            return Collection.Unlock(_botDeck) ? StarterDecks.Find(_botDeck) : null;
        }

        /// <summary>The result screen's line about the bot's deck, now that the match is over and it may be revealed.</summary>
        string BotDeckLine(bool iWon, StarterDecks.Starter unlocked)
        {
            if (!VsBot || _botDeck == null) return "";
            if (unlocked != null) return $"\nYou unlocked {_botDeck}!";
            // Owned or not premade at all: a deck the player chose for the bot once every bot deck was unlocked.
            if (Collection.Owns(_botDeck) || StarterDecks.Find(_botDeck) == null) return $"\nThe bot played {_botDeck}.";
            return iWon ? "" : $"\nThe bot played {_botDeck}. Beat it to unlock this deck.";
        }

        /// <summary>
        /// "NEW DECK UNLOCKED", on its own screen: the deck's name and three of its key cards flipping into view.
        /// <paramref name="onContinue"/> runs when the player closes it.
        /// </summary>
        void ShowDeckUnlocked(StarterDecks.Starter deck, Action onContinue)
        {
            var dim = ShowOverlay(0.97f); // replaces the result dialog: one screen at a time
            Transform layer = dim;
            Color tint = Fx.Tint(deck.Element);
            const float px = 330, py = 110, pw = 1260, ph = 846;

            // Soft glow in the deck's element colour, behind a solid panel so all text sits on one calm background.
            var glow = UI.Panel(layer, new Color(tint.r, tint.g, tint.b, 0.4f), 960 - 760, 540 - 560, 1520, 1120, "Glow");
            glow.sprite = Theme.Soft;
            glow.raycastTarget = false;
            var panel = Theme.GlassPanel(layer, px, py, pw, ph, 32, "UnlockPanel");
            panel.color = new Color32(14, 18, 42, 255);
            Theme.RoundedPanel(panel.transform, new Color(tint.r, tint.g, tint.b, 0.9f), 0, 0, pw, 8, 4, "Stripe").raycastTarget = false;
            Transform t = panel.transform;
            var fx = UI.Stretch(UI.Rect("Fx", layer));

            var eyebrow = Theme.Label(t, "NEW DECK UNLOCKED", 30, Theme.Gold, 0, 40, pw, 40, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(eyebrow, 2, 0.8f);
            var title = Theme.Label(t, deck.Name.ToUpperInvariant(), 80, Color.white, 20, 82, pw - 40, 100, TextAnchor.MiddleCenter, display: true);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 40;
            title.resizeTextMaxSize = 80;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            Theme.AddShadow(title, 4, 0.8f);
            var info = Theme.Label(t, $"{deck.Element.ToString().ToUpperInvariant()}  ·  {deck.Stars}-STAR DECK  ·  BEATEN ON {deck.UnlockLevel.ToString().ToUpperInvariant()}",
                20, Theme.Text, 0, 186, pw, 30, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(info, 2, 0.8f);

            // The stage it is built around (if any) and its two main legends: dark first, then lit one by one.
            List<int> keyCards = DeckLibrary.KeyCards(deck);
            const float scale = 1.3f, gap = 48, cardY = 240;
            float cw = CardView.W * scale, total = keyCards.Count * cw + (keyCards.Count - 1) * gap;
            for (int i = 0; i < keyCards.Count; i++)
            {
                float x = (pw - total) / 2 + i * (cw + gap);
                CardView view = CardView.Create(t, CardCatalogue.Create(keyCards[i]), true, x, cardY, scale);
                view.SetDimmed(true, 0.92f);
                foreach (Graphic g in view.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
                var center = new Vector2(px + x + cw / 2, py + cardY + CardView.H * scale / 2);
                After(0.5f + i * 0.35f, () =>
                {
                    if (view == null) return;
                    view.SetDimmed(false);
                    Fx.Shockwave(fx, center, tint, 320, 0, 0.45f);
                    Fx.Sparks(fx, center, tint, 14, 380);
                    Sfx.Play("card_play", 0.8f);
                });
            }

            // How it wins, on its own plate, then where to find it.
            var plate = Theme.RoundedPanel(t, new Color(1, 1, 1, 0.07f), 90, 534, pw - 180, 100, 18, "About");
            plate.raycastTarget = false;
            var about = Theme.Label(plate.transform, deck.Description, 24, Theme.Text, 30, 0, pw - 240, 100, TextAnchor.MiddleCenter);
            about.horizontalOverflow = HorizontalWrapMode.Wrap;
            Theme.Label(t, "It's yours now: play it in every mode. Find it in the Deck Builder under BOT DECKS.", 20, Theme.TextDim,
                0, 652, pw, 30, TextAnchor.MiddleCenter);

            Theme.Cta(t, "CONTINUE", (pw - 400) / 2, 726, 400, 80, onContinue, 32);

            Fx.Flash(fx, new Color(tint.r, tint.g, tint.b, 0.3f), 0.5f);
            Fx.Glow(fx, new Vector2(960, 450), new Color(1f, 0.85f, 0.45f, 0.7f), 900, 0, 0.9f);
            Fx.Sparks(fx, new Vector2(960, py + 60), Theme.Gold, 40, 640, 0.1f);
            Sfx.Play("power", 0.9f);
            After(0.5f + keyCards.Count * 0.35f + 0.1f, () => Sfx.Play("buff", 0.8f));
            panel.gameObject.AddComponent<SlideIn>().Offset = new Vector2(0, -30);
        }
    }
}
