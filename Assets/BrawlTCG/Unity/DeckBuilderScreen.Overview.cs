using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>The Deck Builder's ALL DECKS button: the shared <see cref="DeckOverview"/> grid, locked decks included.</summary>
    public sealed partial class DeckBuilderScreen
    {
        RectTransform _overviewLayer;

        void OpenOverview()
        {
            if (_showcase != null) return;
            DeckOverview.Open(_overviewLayer, playableOnly: false, _deckName, (name, kind) =>
                ConfirmUnsaved("SWITCH", () => SetCategory((DeckCategory)(int)kind, name)));
            _modal.SetAsLastSibling(); // the unsaved-changes dialog still goes on top
        }
    }
}
