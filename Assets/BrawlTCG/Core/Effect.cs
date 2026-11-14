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
            _action?.Invoke(target, source, game, playedCard);
        }
    }
}
