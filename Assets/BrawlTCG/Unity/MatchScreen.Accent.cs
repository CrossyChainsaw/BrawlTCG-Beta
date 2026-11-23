using System.Collections.Generic;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Stage accent (#60): while a stage is in play, the decorative parts of the board (lane and panel outlines, the
    /// centre strip, the stage slot, the vignette) take on the stage's element colour, fading in with the stage backdrop.
    /// Gold stays on everything that means "you can act" (your turn, usable attacks, targets, drop zones, costs).
    /// </summary>
    public sealed partial class MatchScreen
    {
        /// <summary>
        /// A graphic that follows the accent: its own colour without a stage, the accent at <c>Alpha</c> with one
        /// (a negative <c>Alpha</c> keeps its own). <c>Mix</c> below 1 only tints its own colour towards the accent.
        /// </summary>
        struct Accented
        {
            public Graphic Graphic;
            public Color Neutral;
            public float Alpha, Mix;
        }

        // Board graphics are rebuilt on every refresh; the vignette lives as long as the screen.
        readonly List<Accented> _accentBoard = new List<Accented>();
        readonly List<Accented> _accentBackdrop = new List<Accented>();
        Color _accent = Theme.Gold, _accentTarget = Theme.Gold;
        float _accentK, _accentTargetK; // 0 = neutral look, 1 = full stage accent

        /// <summary>
        /// Personal setting (OPTIONS, saved on this PC): the stage in play changes the background and the board's colours.
        /// Off keeps the battle backdrop and the normal colours whatever stage is in play.
        /// </summary>
        public static bool StageVisuals
        {
            get => PlayerPrefs.GetInt("BrawlTCG.StageVisuals", 1) == 1;
            set { PlayerPrefs.SetInt("BrawlTCG.StageVisuals", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Makes <paramref name="g"/> follow the stage accent. Its current colour is the look without a stage.</summary>
        T Accent<T>(T g, float alpha, bool backdrop = false, float mix = 1) where T : Graphic
        {
            var a = new Accented { Graphic = g, Neutral = g.color, Alpha = alpha, Mix = mix };
            (backdrop ? _accentBackdrop : _accentBoard).Add(a);
            Paint(a);
            return g;
        }

        /// <summary>Called on every refresh, with the stage backdrop: picks the accent of the stage in play.</summary>
        void UpdateAccentTarget()
        {
            StageCard stage = StageVisuals ? _game.ActiveStage : null;
            _accentTargetK = stage != null ? 1 : 0;
            if (stage != null) _accentTarget = CardStyle.FrameColor(stage.Element);
            if (_accentK <= 0) _accent = _accentTarget; // from neutral: no blend through the previous stage's colour
        }

        void UpdateAccent()
        {
            if (_accentK == _accentTargetK && _accent == _accentTarget) return;
            float step = Time.unscaledDeltaTime / StageFade;
            _accentK = Mathf.MoveTowards(_accentK, _accentTargetK, step);
            _accent = Color.Lerp(_accent, _accentTarget, Mathf.Clamp01(step * 4));
            if (Mathf.Abs(_accent.r - _accentTarget.r) + Mathf.Abs(_accent.g - _accentTarget.g) + Mathf.Abs(_accent.b - _accentTarget.b) < 0.004f)
                _accent = _accentTarget;
            PaintAll(_accentBoard);
            PaintAll(_accentBackdrop);
        }

        void PaintAll(List<Accented> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Graphic == null) { list.RemoveAt(i); continue; }
                Paint(list[i]);
            }
        }

        void Paint(Accented a)
        {
            float k = Theme.EaseOut(_accentK);
            Color tinted = Color.Lerp(a.Neutral, _accent, a.Mix);
            tinted.a = a.Alpha < 0 ? a.Neutral.a : a.Alpha;
            a.Graphic.color = Color.Lerp(a.Neutral, tinted, k);
        }
    }
}
