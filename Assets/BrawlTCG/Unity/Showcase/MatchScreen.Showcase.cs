using System;
using System.Collections;
using BrawlTCG.Core;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Showcase mode: the board starts from a staged position and a script plays the moves through the normal
    /// <see cref="Submit"/> path, so every animation, pop-up and sound is the real one. Input is blocked meanwhile.
    /// </summary>
    public sealed partial class MatchScreen
    {
        ShowcaseClip _showcase;

        bool InShowcase => _showcase != null;

        /// <summary>
        /// Multiplies every gap between scripted actions (1 = relaxed, lower = snappier). The next action starts while
        /// the last one's effects are still playing, like a quick player would.
        /// </summary>
        public static float ShowcasePace = 0.45f;

        void BeginShowcase(ShowcaseClip clip)
        {
            _showcase = clip;
            _viewer = clip.Hero;
            // Swallows clicks and hovers so a stray mouse cannot change the scripted match.
            var blocker = UI.FullPanel(transform, new Color(0, 0, 0, 0), "ShowcaseInputBlocker");
            blocker.raycastTarget = true;
            Refresh();
            StartCoroutine(RunShowcase());
        }

        static IEnumerator Pause(float seconds)
        {
            if (seconds > 0) yield return new WaitForSecondsRealtime(seconds);
        }

        /// <summary>How long to let an action play out before the next one.</summary>
        static float DefaultPause(GameCommand cmd, Card played)
        {
            switch (cmd.Type)
            {
                case CommandType.EndTurn: return 2.6f;
                case CommandType.PlayEssence: return 1.0f;
                case CommandType.PlayStage: return 2.4f;
                case CommandType.PlayLegend: return 1.5f;
                case CommandType.PlayWeapon: return 1.2f;
                case CommandType.PlayBattle: return played is BattleCard b && !b.TargetRequired ? 2.0f : 1.8f;
                case CommandType.Attack: return 1.7f;
                default: return 1.2f;
            }
        }

        IEnumerator RunShowcase()
        {
            yield return Pause(0.5f);
            Fx.Banner(_top, _showcase.Title, _showcase.Subtitle, Theme.Gold);
            Sfx.Play("turn", 0.8f);
            yield return Pause(2.2f);
            TurnIntro(false);
            yield return Pause(0.9f);

            foreach (ShowStep step in _showcase.Steps)
            {
                if (this == null || _game.IsOver) break;
                switch (step.Kind)
                {
                    case ShowStepKind.Wait:
                        yield return Pause(step.Pause * ShowcasePace);
                        continue;

                    case ShowStepKind.Inspect:
                    {
                        Card card = ShowcaseResolver.FindInspected(_game, step);
                        if (card == null) { Debug.LogWarning($"[Showcase {_showcase.Id}] nothing to inspect: {step}"); continue; }
                        OpenDetail(card);
                        yield return Pause((step.Pause > 0 ? step.Pause : 2.2f) * Mathf.Max(ShowcasePace, 0.7f)); // long enough to read
                        CloseDetail();
                        yield return Pause(0.15f);
                        continue;
                    }
                }

                GameCommand cmd = ShowcaseResolver.Resolve(_game, step, out string error);
                if (cmd == null)
                {
                    Debug.LogWarning($"[Showcase {_showcase.Id}] skipped \"{step}\": {error}");
                    continue;
                }
                Card played = cmd.HandIndex >= 0 && cmd.HandIndex < _game.ActivePlayer.Hand.Count ? _game.ActivePlayer.Hand[cmd.HandIndex] : null;
                Submit(cmd);
                yield return Pause((step.Pause >= 0 ? step.Pause : DefaultPause(cmd, played)) * ShowcasePace);
            }

            // Let the K.O. and the result screen (or the final board) breathe before the clip ends.
            yield return Pause(_game.IsOver ? 5.5f : 2.5f);
            ShowcaseDirector.Finished(_showcase);
        }
    }
}
