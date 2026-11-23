using System;
using System.Collections;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Opening animation: the two players slide in from either side, "VS" slams down between them, and the one who
    /// goes first is announced. When a coin flip decided that, the coin is tossed with an avatar on each face. Click to skip. Fully opaque, so in hot seat no hand shows through.
    /// </summary>
    public sealed partial class MatchScreen
    {
        const float IntroHold = 2.6f, IntroFade = 0.35f;

        static readonly Color LeftColor = new Color32(245, 192, 74, 255);   // gold, like the active player's panel
        static readonly Color RightColor = new Color32(140, 170, 255, 255); // blue, like the opponent's turn banner

        void PlayVersusIntro(Action done)
        {
            var dim = ShowOverlay(1f);
            // You on the left (online); player 1 on the left in hot seat.
            int leftIndex = _hotSeat ? (_seatsSwapped ? 1 : 0) : _localIndex;
            Player left = _game.Players[leftIndex], right = _game.Players[1 - leftIndex];

            var group = dim.gameObject.AddComponent<CanvasGroup>();
            RectTransform leftSide = VersusSide(dim, left, true);
            RectTransform rightSide = VersusSide(dim, right, false);

            // "VS" in the middle.
            var vs = UI.Place(UI.Rect("Vs", dim), 760, 380, 400, 260);
            vs.pivot = new Vector2(0.5f, 0.5f);
            vs.anchoredPosition = new Vector2(960, -510);
            var vsLabel = Theme.Label(vs, "VS", 190, Theme.Gold, 0, 0, 400, 260, TextAnchor.MiddleCenter, display: true);
            UI.Stretch(vsLabel.rectTransform);
            Theme.AddShadow(vsLabel, 6, 0.7f);
            vs.localScale = Vector3.zero;
            RectTransform coin = _coinFlip ? Coin(dim, left, right) : null;

            string starter = _game.ActivePlayer.Name.ToUpperInvariant();
            bool meFirst = !_hotSeat && _game.ActiveIndex == _localIndex;
            var first = Theme.Label(dim, meFirst ? "YOU GO FIRST" : $"{starter} GOES FIRST", 30, Theme.Text, 0, 790, 1920, 44, TextAnchor.MiddleCenter, display: true);
            first.raycastTarget = false;
            var firstGroup = first.gameObject.AddComponent<CanvasGroup>();
            firstGroup.alpha = 0;
            var skip = Theme.Label(dim, "click to skip", 15, Theme.TextFaint, 0, 1030, 1920, 24, TextAnchor.MiddleCenter);
            skip.raycastTarget = false;

            bool finished = false;
            void Finish()
            {
                if (finished || this == null) return;
                finished = true;
                CloseOverlay();
                done();
            }
            UI.Clicks(dim.gameObject, _ => Finish());
            bool leftWins = _game.ActiveIndex == leftIndex;
            StartCoroutine(VersusRoutine(group, leftSide, rightSide, vs, coin, leftWins, firstGroup, () => finished, Finish));
        }

        /// <summary>A gold-rimmed coin with the left player's avatar on face "A" and the right player's on face "B".</summary>
        RectTransform Coin(RectTransform dim, Player left, Player right)
        {
            const float size = 220;
            var coin = UI.Place(UI.Rect("Coin", dim), 960 - size / 2, 510 - size / 2, size, size);
            coin.pivot = new Vector2(0.5f, 0.5f);
            coin.anchoredPosition = new Vector2(960, -510);
            CoinFace(coin, "A", left, LeftColor, size);
            CoinFace(coin, "B", right, RightColor, size).gameObject.SetActive(false);
            coin.gameObject.SetActive(false);
            return coin;
        }

        RectTransform CoinFace(RectTransform coin, string name, Player p, Color c, float size)
        {
            var rim = Theme.Disc(coin, Theme.Gold, 0, 0, size, name);
            rim.raycastTarget = false;
            float inner = size - 28;
            var face = Theme.Disc(rim.transform, c, 14, 14, inner, "Avatar");
            face.raycastTarget = false;
            Avatars.Paint(face, inner, _avatars[p.Index], p.Name, 96, Theme.Ink);
            return rim.rectTransform;
        }

        /// <summary>One player's half: a tinted band, a big avatar (portrait or initial), the name and their role.</summary>
        RectTransform VersusSide(RectTransform dim, Player p, bool isLeft)
        {
            Color c = isLeft ? LeftColor : RightColor;
            var side = UI.Place(UI.Rect(isLeft ? "Left" : "Right", dim), 0, 0, 1920, 1080);

            var band = UI.Panel(side, new Color(c.r, c.g, c.b, 0.22f), isLeft ? 0 : 960, 300, 960, 420, "Band");
            band.sprite = Theme.Gradient(true, isLeft ? 0f : 1f, isLeft ? 1f : 0f);
            band.raycastTarget = false;
            var edge = UI.Panel(side, new Color(c.r, c.g, c.b, 0.8f), isLeft ? 0 : 960, 300, 960, 3, "Edge");
            edge.sprite = band.sprite;
            edge.raycastTarget = false;
            var edge2 = UI.Panel(side, new Color(c.r, c.g, c.b, 0.8f), isLeft ? 0 : 960, 717, 960, 3, "Edge");
            edge2.sprite = band.sprite;
            edge2.raycastTarget = false;

            float cx = isLeft ? 480 : 1440;
            var glow = UI.Panel(side, new Color(c.r, c.g, c.b, 0.45f), cx - 170, 330, 340, 340, "Glow");
            glow.sprite = Theme.Soft;
            glow.raycastTarget = false;
            var avatar = Theme.Disc(side, c, cx - 90, 380, 180, "Avatar");
            avatar.raycastTarget = false;
            Avatars.Paint(avatar, 180, _avatars[p.Index], p.Name, 96, Theme.Ink);

            var name = Theme.Label(side, p.Name.ToUpperInvariant(), 56, Theme.Text, cx - 420, 580, 840, 72, TextAnchor.MiddleCenter, display: true);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 24;
            name.resizeTextMaxSize = 56;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.raycastTarget = false;
            Theme.AddShadow(name, 3, 0.6f);
            string role = _hotSeat ? $"PLAYER {p.Index + 1}" : p.Index == _localIndex ? "YOU" : "OPPONENT";
            Theme.Label(side, role, 20, c, cx - 300, 656, 600, 28, TextAnchor.MiddleCenter, display: true).raycastTarget = false;

            side.anchoredPosition = new Vector2(isLeft ? -1100 : 1100, 0);
            return side;
        }

        IEnumerator VersusRoutine(CanvasGroup all, RectTransform left, RectTransform right, RectTransform vs, RectTransform coin,
            bool leftWins, CanvasGroup first, Func<bool> skipped, Action finish)
        {
            Sfx.Play("ui_click", 0.6f);
            // Sides slide in.
            const float slide = 0.5f;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / slide)
            {
                if (skipped()) yield break;
                float e = Theme.EaseOut(t);
                left.anchoredPosition = new Vector2(-1100 * (1 - e), 0);
                right.anchoredPosition = new Vector2(1100 * (1 - e), 0);
                yield return null;
            }
            left.anchoredPosition = right.anchoredPosition = Vector2.zero;

            // VS slams down.
            Sfx.Play("turn", 0.9f);
            Fx.Shockwave(vs.parent, new Vector2(960, 510), Theme.Gold, 900, 0.08f, 0.6f);
            Fx.Sparks(vs.parent, new Vector2(960, 510), Theme.Gold, 40, 900, 0.08f);
            const float slam = 0.28f;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / slam)
            {
                if (skipped()) yield break;
                float s = Mathf.Lerp(2.4f, 1f, Theme.EaseOut(t));
                vs.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            vs.localScale = Vector3.one;
            Shake(0.4f, 0);

            if (coin != null)
            {
                IEnumerator toss = CoinToss(vs, coin, leftWins, skipped);
                while (toss.MoveNext()) yield return toss.Current;
                if (skipped()) yield break;
            }

            // Who goes first.
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.3f)
            {
                if (skipped()) yield break;
                first.alpha = t;
                yield return null;
            }
            first.alpha = 1;

            for (float t = 0; t < IntroHold - slide - slam - 0.3f; t += Time.unscaledDeltaTime)
            {
                if (skipped()) yield break;
                yield return null;
            }

            // Fade out.
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / IntroFade)
            {
                if (skipped()) yield break;
                all.alpha = 1 - t;
                yield return null;
            }
            finish();
        }

        /// <summary>
        /// The VS shrinks away and the coin spins in place, slowing down, and stops on the
        /// face of the player who goes first (A = left, B = right). That player's side lights up in their colour.
        /// </summary>
        IEnumerator CoinToss(RectTransform vs, RectTransform coin, bool leftWins, Func<bool> skipped)
        {
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.2f)
            {
                if (skipped()) yield break;
                float s = 1 - Theme.EaseOut(t);
                vs.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            vs.localScale = Vector3.zero;

            GameObject faceA = coin.Find("A").gameObject, faceB = coin.Find("B").gameObject;
            coin.gameObject.SetActive(true);
            Sfx.Play("card_draw", 0.8f);

            // An even number of half turns ends on face A, an odd number on face B.
            int halfTurns = leftWins ? 12 : 13;
            const float duration = 1.9f;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / duration)
            {
                if (skipped()) yield break;
                float spin = (1 - (1 - t) * (1 - t) * (1 - t)) * halfTurns * Mathf.PI; // fast at first, slowing down
                bool a = Mathf.FloorToInt((spin + Mathf.PI / 2) / Mathf.PI) % 2 == 0;
                faceA.SetActive(a);
                faceB.SetActive(!a);
                coin.localScale = new Vector3(Mathf.Max(0.04f, Mathf.Abs(Mathf.Cos(spin))), 1, 1);
                yield return null;
            }
            faceA.SetActive(leftWins);
            faceB.SetActive(!leftWins);
            coin.localScale = Vector3.one;

            // Landed: the winner's side lights up.
            Color c = leftWins ? LeftColor : RightColor;
            var winner = new Vector2(leftWins ? 480 : 1440, 470);
            Sfx.Play("turn", 0.9f);
            Fx.Shockwave(coin.parent, new Vector2(960, 510), c, 600, 0, 0.5f);
            Fx.Glow(coin.parent, winner, c, 460, 0.05f, 0.9f);
            Fx.Sparks(coin.parent, winner, c, 36, 700, 0.05f);
            Shake(0.3f, 0);
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                if (skipped()) yield break;
                yield return null;
            }
        }
    }
}
