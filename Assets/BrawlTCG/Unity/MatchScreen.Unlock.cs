using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Shown once, right after the deck that completes the collection: every bot deck of every difficulty is beaten,
        /// so VS BOT now lets the player choose the bot's deck. A fan of legends from the bot's decks, fireworks, and the
        /// reward. <paramref name="onContinue"/> runs when the player closes it.
        /// </summary>
        void ShowAllBotDecksBeaten(Action onContinue)
        {
            var dim = ShowOverlay(0.97f);
            Transform layer = dim;
            Color gold = Theme.Gold;
            const float px = 250, py = 70, pw = 1420, ph = 940;

            var glow = UI.Panel(layer, new Color(gold.r, gold.g, gold.b, 0.45f), 960 - 900, 540 - 640, 1800, 1280, "Glow");
            glow.sprite = Theme.Soft;
            glow.raycastTarget = false;
            var panel = Theme.GlassPanel(layer, px, py, pw, ph, 36, "ChampionPanel");
            panel.color = new Color32(14, 16, 38, 255);
            Theme.RoundedPanel(panel.transform, new Color(gold.r, gold.g, gold.b, 0.95f), 0, 0, pw, 10, 5, "Stripe").raycastTarget = false;
            Theme.RoundedPanel(panel.transform, new Color(gold.r, gold.g, gold.b, 0.55f), 0, 0, pw, ph, 36, "Line", outline: true).raycastTarget = false;
            Transform t = panel.transform;
            var fx = UI.Stretch(UI.Rect("Fx", layer));

            int total = ((Bot.Level[])Enum.GetValues(typeof(Bot.Level))).Sum(l => StarterDecks.ForLevel(l).Count);
            var eyebrow = Theme.Label(t, "CHAMPION", 30, gold, 0, 36, pw, 40, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(eyebrow, 2, 0.8f);
            var title = Theme.Label(t, "ALL BOT DECKS BEATEN", 88, Color.white, 20, 78, pw - 40, 110, TextAnchor.MiddleCenter, display: true);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 40;
            title.resizeTextMaxSize = 88;
            Theme.AddShadow(title, 4, 0.8f);
            Theme.AddOutline(title, 2, 0.5f);
            var info = Theme.Label(t, $"YOU BEAT THE BOT WITH EVERY ONE OF ITS {total} DECKS  ·  EASY, NORMAL AND HARD",
                20, Theme.Text, 0, 192, pw, 30, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(info, 2, 0.8f);

            // A fan of legends, one from each of the strongest bot decks, lit one by one from the middle out.
            List<int> legends = StarterDecks.Premade.Where(s => !s.IsStarterPool).OrderBy(s => s.Rank)
                .Select(s => DeckLibrary.KeyCards(s).FirstOrDefault(id => CardCatalogue.Get(id).Type == CardType.Legend))
                .Where(id => id != 0).Distinct().Take(7).ToList();
            const float scale = 0.95f, cardTop = 250;
            float cw = CardView.W * scale, ch = CardView.H * scale;
            int n = legends.Count;
            for (int i = 0; i < n; i++)
            {
                float k = n == 1 ? 0 : (i - (n - 1) / 2f) / ((n - 1) / 2f); // -1 ... 1
                float x = pw / 2 - cw / 2 + k * 470;
                float y = cardTop + k * k * 46;                                // an arc: the edges sit lower
                CardView view = CardView.Create(t, CardCatalogue.Create(legends[i]), true, x, y, scale);
                view.transform.localRotation = Quaternion.Euler(0, 0, -k * 14);
                view.SetDimmed(true, 0.92f);
                foreach (Graphic g in view.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
                var center = new Vector2(px + x + cw / 2, py + y + ch / 2);
                float delay = 0.45f + Mathf.Abs(k) * 0.9f;
                After(delay, () =>
                {
                    if (view == null) return;
                    view.SetDimmed(false);
                    Fx.Shockwave(fx, center, gold, 300, 0, 0.45f);
                    Fx.Sparks(fx, center, gold, 12, 360);
                    Sfx.Play("card_play", 0.7f);
                });
            }

            // The reward, on its own plate.
            var plate = Theme.RoundedPanel(t, new Color(gold.r, gold.g, gold.b, 0.12f), 140, 640, pw - 280, 130, 20, "Reward");
            plate.raycastTarget = false;
            Theme.RoundedPanel(plate.transform, new Color(gold.r, gold.g, gold.b, 0.6f), 0, 0, pw - 280, 130, 20, "Line", outline: true).raycastTarget = false;
            Theme.Label(plate.transform, "NEW  ·  CHOOSE THE BOT'S DECK", 30, gold, 0, 16, pw - 280, 40, TextAnchor.MiddleCenter, display: true);
            var about = Theme.Label(plate.transform, "In VS BOT you now pick the deck the bot plays: any deck you own, your own builds, or a random bot deck.",
                22, Theme.Text, 30, 62, pw - 340, 56, TextAnchor.MiddleCenter);
            about.horizontalOverflow = HorizontalWrapMode.Wrap;

            Theme.Cta(t, "CONTINUE", (pw - 400) / 2, 810, 400, 84, onContinue, 32);

            // Fireworks: bursts all over the screen in gold and the element colours, for a few seconds.
            Fx.Flash(fx, new Color(gold.r, gold.g, gold.b, 0.35f), 0.6f);
            Fx.Glow(fx, new Vector2(960, 300), new Color(1f, 0.85f, 0.45f, 0.8f), 1100, 0, 1.1f);
            Fx.Sparks(fx, new Vector2(960, py + 130), gold, 60, 760, 0.05f);
            Sfx.Play("power", 1f);
            Color[] colors = { gold, Color.white, Fx.Tint(Element.Fire), Fx.Tint(Element.Arctic), Fx.Tint(Element.Nature), Fx.Tint(Element.Cosmic), Fx.Tint(Element.Shadow) };
            var rng = new System.Random();
            for (int i = 0; i < 14; i++)
            {
                var at = new Vector2(160 + (float)rng.NextDouble() * 1600, 90 + (float)rng.NextDouble() * 520);
                Color c = colors[rng.Next(colors.Length)];
                bool loud = rng.Next(3) == 0;
                After(0.3f + i * 0.28f, () =>
                {
                    if (fx == null) return;
                    Fx.Shockwave(fx, at, c, 260, 0, 0.5f);
                    Fx.Sparks(fx, at, c, 26, 520, 0, 420);
                    if (loud) Sfx.Play("buff", 0.5f);
                });
            }
            After(1.6f, () => Sfx.Play("ko", 0.6f));
            panel.gameObject.AddComponent<SlideIn>().Offset = new Vector2(0, -30);
        }
    }
}
