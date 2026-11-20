using System;
using System.Threading.Tasks;
using BrawlTCG.Core;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// The "connection" to a computer opponent. To the match screen a bot game looks like an online game: the bot's
    /// moves arrive as command lines in the inbox. <see cref="Poll"/> (main thread) takes a search copy of the match
    /// whenever it is the bot's move and lets the <see cref="Bot"/> think on a worker thread; the real match is only
    /// ever touched by the match screen on the main thread. Moves are spaced out so the player can follow them.
    /// </summary>
    public sealed class BotSession : NetSession
    {
        /// <summary>Pause before the bot's first move of a turn, and between its moves (seconds).</summary>
        const float TurnStartDelay = 1.5f, MoveDelay = 0.9f, EndTurnDelay = 0.6f;

        readonly Game _game;
        readonly Bot _bot;
        readonly System.Random _seeds = new System.Random();
        Task<GameCommand> _thinking;
        float _readyAt;
        int _lastTurn = -1;
        bool _disposed;

        public Bot.Level Level { get; }
        public int BotIndex => _bot.PlayerIndex;

        public BotSession(Game game, Bot.Level level, int botIndex)
        {
            _game = game;
            Level = level;
            _bot = Bot.Create(level, botIndex, _seeds.Next());
        }

        public override Task<string> ReadLineAsync(int timeoutMs = 20000) => Task.FromResult<string>(null);

        /// <summary>The player's own moves: the bot reads the match itself, so there is nothing to send.</summary>
        public override bool Send(string line) => !_disposed;

        public override void Dispose() => _disposed = true;

        public override void Poll()
        {
            if (_disposed || _game.IsOver || HasPending) return;

            if (_thinking != null)
            {
                if (!_thinking.IsCompleted) return;
                GameCommand cmd = null;
                if (_thinking.IsFaulted) Debug.LogException(_thinking.Exception);
                else cmd = _thinking.Result;
                _thinking = null;

                // The bot thought on a copy: make sure the move is still legal on the real match.
                if (cmd == null || _game.Validate(cmd) != null)
                    cmd = _game.ChoosingStartingHands ? GameCommand.RedrawStartingHand(BotIndex, 0) : GameCommand.EndTurn(BotIndex);
                Inbox.Enqueue(Protocol.CommandPrefix + cmd.Serialize());
                _readyAt = Time.unscaledTime + (cmd.Type == CommandType.EndTurn ? EndTurnDelay : MoveDelay);
                return;
            }

            bool botsMove = _game.ChoosingStartingHands
                ? !_game.HasChosenStartingHand(BotIndex)
                : _game.ActiveIndex == BotIndex;
            if (!botsMove) return;
            if (!_game.ChoosingStartingHands && _game.TurnNumber != _lastTurn)
            {
                // A new bot turn: give the turn banner time to show before the first move.
                _lastTurn = _game.TurnNumber;
                _readyAt = Mathf.Max(_readyAt, Time.unscaledTime + TurnStartDelay);
            }
            if (Time.unscaledTime < _readyAt) return;

            // The bot never sees your hand: in its copy your hand is redealt from the cards it hasn't seen.
            Game snapshot = _game.CloneForSearch(_seeds.Next(), hideHandOf: 1 - BotIndex);
            _thinking = Task.Run(() => _bot.Decide(snapshot));
        }
    }
}
