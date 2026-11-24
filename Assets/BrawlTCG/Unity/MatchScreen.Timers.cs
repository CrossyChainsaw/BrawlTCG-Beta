using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Time limits: 30 seconds to choose a starting hand, 60 seconds per turn. Each client only enforces its own
    /// player's limit (by sending the normal command when time runs out), so lockstep is unaffected. The opponent's
    /// remaining time is shown as an estimate. Hot seat pauses the clock while the options menu is open.
    /// </summary>
    public sealed partial class MatchScreen
    {
        public const float StartingHandSeconds = 30, TurnSeconds = 60;

        // Starting hand
        float _handDeadline = -1;      // while the viewer is choosing, else -1
        float _handShownAt = -1;       // when the starting hands were first shown (online: estimate the opponent's clock)
        bool _handOverlay;             // our hand is shown while we wait for the opponent's choice
        Text _handTimerLabel, _opponentHandLabel;

        // Turn
        int _timedTurn = -1;
        float _turnDeadline;
        int _lastTick = -1;
        Text _turnTimerLabel;
        RectTransform _turnTimerBar;
        float _turnTimerBarWidth;

        static readonly Color TimerRed = new Color32(255, 110, 110, 255);

        void UpdateTimers()
        {
            if (_ended || InShowcase || _game == null || !_game.IsStarted || _game.IsOver) return;
            float now = Time.unscaledTime;
            bool paused = _hotSeat && OptionsOpen;

            // ── Starting hand ──
            if (_handDeadline >= 0)
            {
                if (paused) _handDeadline += Time.unscaledDeltaTime;
                float left = _handDeadline - now;
                if (_handTimerLabel != null)
                {
                    int s = Mathf.Max(0, Mathf.CeilToInt(left));
                    _handTimerLabel.text = $"{s}s left to choose";
                    _handTimerLabel.color = s <= 10 ? TimerRed : Theme.TextDim;
                }
                if (left <= 0)
                {
                    _app.Toast("Time's up! Your starting hand is locked in.");
                    ConfirmStartingHand();
                    return;
                }
            }
            if (_opponentHandLabel != null) _opponentHandLabel.text = OpponentHandStatus();

            // ── Turn ──
            if (_game.ChoosingStartingHands || _handoffPending || _handOverlay) return;
            if (_timedTurn != _game.TurnNumber)
            {
                _timedTurn = _game.TurnNumber;
                _turnDeadline = now + TurnSeconds;
                _lastTick = -1;
            }
            if (paused) _turnDeadline += Time.unscaledDeltaTime;
            float turnLeft = Mathf.Max(0, _turnDeadline - now);
            int secs = Mathf.CeilToInt(turnLeft);
            ShowTurnTime(turnLeft);

            if (!CanAct) return;
            if (secs <= 5 && secs > 0 && secs != _lastTick)
            {
                _lastTick = secs;
                Sfx.Play("ui_click", 0.5f, 0, 0, 1.4f); // last five seconds tick
            }
            if (turnLeft <= 0) TurnTimeUp();
        }

        void ShowTurnTime(float left)
        {
            int secs = Mathf.CeilToInt(left);
            bool low = secs <= 10;
            if (_turnTimerLabel != null)
            {
                _turnTimerLabel.text = $"{secs / 60}:{secs % 60:00}";
                _turnTimerLabel.color = low ? TimerRed : Theme.Text;
            }
            if (_turnTimerBar != null)
            {
                _turnTimerBar.sizeDelta = new Vector2(_turnTimerBarWidth * Mathf.Clamp01(left / TurnSeconds), _turnTimerBar.sizeDelta.y);
                _turnTimerBar.GetComponent<Image>().color = low ? TimerRed : Theme.Gold;
            }
        }

        void TurnTimeUp()
        {
            CancelTargeting();
            if (_burn != null) CloseOverlay(); // was choosing weapons to burn
            UI.Clear(_drag); // a card being dragged when time ran out
            _app.Toast("Time's up!");
            Submit(GameCommand.EndTurn(_game.ActiveIndex));
        }

        /// <summary>The countdown and its bar in the centre strip (called from <see cref="DrawStrip"/>).</summary>
        void DrawTurnTimer(Transform strip)
        {
            _turnTimerLabel = null;
            _turnTimerBar = null;
            if (InShowcase || _game.IsOver || _handoffPending) return;
            _turnTimerLabel = Theme.Label(strip, "", 34, Theme.Text, Strip.width - 236 - 128, 16, 112, 60, TextAnchor.MiddleRight, display: true);
            _turnTimerLabel.raycastTarget = false;

            _turnTimerBarWidth = Strip.width - 40;
            Quiet(Theme.RoundedPanel(strip, new Color(1, 1, 1, 0.08f), 20, Strip.height - 9, _turnTimerBarWidth, 4, 2, "TimerTrack"));
            var bar = UI.Panel(strip, Theme.Gold, 20, Strip.height - 9, _turnTimerBarWidth, 4, "TimerBar");
            bar.raycastTarget = false;
            _turnTimerBar = bar.rectTransform;

            bool started = _timedTurn == _game.TurnNumber;
            ShowTurnTime(started ? Mathf.Max(0, _turnDeadline - Time.unscaledTime) : TurnSeconds);
        }

        // ───────────── Starting hand ─────────────

        /// <summary>Starts the viewer's starting-hand clock (once per choice; re-drawing the overlay keeps it running).</summary>
        void StartHandClock()
        {
            if (_handShownAt < 0) _handShownAt = Time.unscaledTime;
            if (_handDeadline < 0) _handDeadline = Time.unscaledTime + StartingHandSeconds;
        }

        /// <summary>Online: whether the opponent is still choosing, with an estimate of the time they have left.</summary>
        string OpponentHandStatus()
        {
            Player opponent = _game.Players[1 - _viewer];
            if (_game.HasChosenStartingHand(opponent.Index)) return $"<color=#78E6A0>{opponent.Name} is ready</color>";
            int left = Mathf.CeilToInt(_handShownAt + StartingHandSeconds - Time.unscaledTime);
            string time = left > 0 ? $"  ·  up to {left}s left" : "  ·  any moment now";
            return $"<color=#F5C04A>{opponent.Name} is still choosing their starting hand</color><color=#A6B0D6>{time}</color>";
        }

        /// <summary>A rich-text status line about the opponent's choice, kept up to date by <see cref="UpdateTimers"/>.</summary>
        void OpponentHandLine(Transform parent, float y)
        {
            if (_hotSeat) return;
            _opponentHandLabel = Theme.Label(parent, OpponentHandStatus(), 22, Theme.Text, 0, y, 1920, 34, TextAnchor.MiddleCenter);
            _opponentHandLabel.supportRichText = true;
            _opponentHandLabel.raycastTarget = false;
        }
    }
}
