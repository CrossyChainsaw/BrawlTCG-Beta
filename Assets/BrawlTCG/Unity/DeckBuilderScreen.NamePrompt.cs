using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// The "name your deck" pop-up used by NEW DECK and RENAME: centred over a darkened screen, a name field, CANCEL and
    /// a confirm button. Enter confirms, Esc or a click outside cancels. Problems with the name are shown in the pop-up.
    /// </summary>
    public sealed partial class DeckBuilderScreen
    {
        const int MaxDeckNameLength = 32;

        InputField _promptField;
        Button _promptOk;
        Text _promptError;
        Func<string, string> _promptCheck;
        Action<string> _promptAccept;

        bool PromptOpen => _promptField != null;

        /// <param name="check">Gets the typed name; returns the problem to show, or null when it can be used.</param>
        /// <param name="accept">Runs with the typed name after the pop-up has closed.</param>
        void NamePrompt(string eyebrow, string title, string initial, string confirm, Func<string, string> check, Action<string> accept)
        {
            UI.Clear(_modal);
            _modal.SetAsLastSibling();
            _promptCheck = check;
            _promptAccept = accept;

            var dim = UI.FullPanel(_modal, new Color(Theme.Night.r, Theme.Night.g, Theme.Night.b, 0.72f), "Dim");
            UI.Clicks(dim.gameObject, e => ClosePrompt()); // clicking outside cancels

            const float w = 620, h = 350;
            var box = Theme.GlassPanel(dim.rectTransform, 960 - w / 2, 540 - h / 2, w, h, 28, "NamePrompt");
            box.color = new Color32(14, 18, 42, 255);
            UI.Clicks(box.gameObject, e => { });
            box.gameObject.AddComponent<SlideIn>().Offset = new Vector2(0, -24);
            Transform t = box.transform;

            Theme.Label(t, eyebrow, 16, Theme.Gold, 0, 28, w, 22, TextAnchor.MiddleCenter, display: true);
            var heading = Theme.Label(t, title, 38, Theme.Text, 0, 52, w, 54, TextAnchor.MiddleCenter, display: true);
            Theme.AddShadow(heading, 3, 0.6f);

            _promptField = Theme.Field(t, null, "Deck name", initial ?? "", 40, 124, w - 80, MaxDeckNameLength);
            _promptError = Theme.Label(t, "", 16, Theme.Danger, 40, 184, w - 80, 26, TextAnchor.MiddleCenter);

            float bw = (w - 80 - 16) / 2;
            Theme.Ghost(t, "CANCEL", 40, 236, bw, 66, ClosePrompt, 22);
            _promptOk = Theme.Cta(t, confirm, 40 + bw + 16, 236, bw, 66, SubmitPrompt, 26);
            Theme.Label(t, "Enter to confirm  ·  Esc to cancel", 14, Theme.TextFaint, 0, 314, w, 22, TextAnchor.MiddleCenter);

            // Ready to type straight away, with any current name selected so typing replaces it.
            _promptField.ActivateInputField();
            _promptField.Select();
        }

        void SubmitPrompt()
        {
            if (!PromptOpen) return;
            string typed = _promptField.text;
            string error = _promptCheck?.Invoke(typed);
            if (error != null)
            {
                _promptError.text = error;
                _promptField.ActivateInputField();
                return;
            }
            Action<string> accept = _promptAccept;
            ClosePrompt(); // first: accepting may open the unsaved-changes dialog on the same layer
            accept?.Invoke(typed);
        }

        void ClosePrompt()
        {
            _promptField = null;
            _promptOk = null;
            _promptError = null;
            _promptCheck = null;
            _promptAccept = null;
            UI.Clear(_modal);
        }

        void Update()
        {
            if (!PromptOpen || _showcase != null) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) SubmitPrompt();
            else if (kb.escapeKey.wasPressedThisFrame) ClosePrompt();
        }

        /// <summary>Why a name can't be used for a new deck (or a rename to it), or null when it's fine.</summary>
        string CheckNewName(string typed, string current = null)
        {
            if (string.IsNullOrWhiteSpace(typed)) return "Enter a name for the deck.";
            string name = DeckStore.SanitizeName(typed);
            if (current != null && name == current) return "The deck already has that name.";
            bool caseOnly = current != null && string.Equals(name, current, StringComparison.OrdinalIgnoreCase);
            if (!caseOnly && DeckStore.Exists(name)) return "A deck with that name already exists.";
            return null;
        }
    }
}
