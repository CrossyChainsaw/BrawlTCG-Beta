namespace BrawlTCG.Core
{
    /// <param name="target">A Player, a LegendCard, a List of LegendCard / Card, or null depending on the trigger.</param>
    /// <param name="source">The card that owns the effect.</param>
    /// <param name="game">The running game.</param>
    /// <param name="playedCard">The card whose play triggered the effect (for while-in-play triggers).</param>
    public delegate void EffectAction(object target, Card source, Game game, Card playedCard);

    public sealed class Effect
    {
        readonly EffectAction _action;

        public string Description { get; }

        public Effect(string description, EffectAction action)
        {
            Description = description;
            _action = action;
        }

        public void Invoke(object target, Card source, Game game, Card playedCard)
        {
            if (_action == null) return;
            EffectContext.Push(source?.Name);
            try { _action(target, source, game, playedCard); }
            finally { EffectContext.Pop(); }
        }
    }

    /// <summary>
    /// Which card (or attack) is currently changing the game, so a legend can remember where its buffs, debuffs,
    /// taps and burns came from. Bookkeeping only: it never changes what an effect does.
    /// </summary>
    internal static class EffectContext
    {
        [System.ThreadStatic] static System.Collections.Generic.List<string> _stack;

        internal static string Current => _stack != null && _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        internal static void Push(string source) => (_stack ??= new System.Collections.Generic.List<string>()).Add(source ?? "Unknown");

        internal static void Pop() => _stack.RemoveAt(_stack.Count - 1);
    }
}
