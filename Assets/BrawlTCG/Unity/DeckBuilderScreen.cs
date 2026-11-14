using System;
using System.Collections.Generic;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.UnityApp
{
    /// <summary>The WinForms FRM_DeckBuilder: filter the catalogue, add/remove copies, save as a deck file.</summary>
    public sealed class DeckBuilderScreen : MonoBehaviour
    {
        static readonly Color RowColor = new Color32(250, 250, 250, 255);
        static readonly Color RowSelected = new Color32(180, 210, 255, 255);

        App _app;
        Selector _type, _element, _weapon, _deckSelector;
        ScrollList _available, _deckList;
        InputField _newDeckName;
        Button _save, _delete;
        Text _stats, _saveHint, _attacks, _effects;
        RectTransform _preview;

        List<int> _deck = new List<int>();
        string _deckName;
        bool _dirty;
        bool _confirmDelete;
        Card _selected;
        bool _selectedFromDeck;

        public void Build(App app)
        {
            _app = app;
            Transform root = transform;

            UI.Label(root, "Deck Builder", 34, Color.black, 20, 8, 600, 44);
            UI.Button(root, "Back to Menu", 1700, 12, 200, 40, BackToMenu);

            // Filters
            _type = new Selector(root, 20, 60, 220, 34, new[] { "All Types", "Legend", "Stage", "Weapon", "Essence", "Battle" });
            _element = new Selector(root, 250, 60, 220, 34, new[] { "All Elements" }.Concat(Enum.GetNames(typeof(Element))));
            _weapon = new Selector(root, 480, 60, 220, 34, new[] { "All Weapons" }.Concat(Enum.GetNames(typeof(Weapon)).Where(w => w != "Any")));
            _type.Changed += _ => PopulateAvailable();
            _element.Changed += _ => PopulateAvailable();
            _weapon.Changed += _ => PopulateAvailable();

            // Catalogue
            UI.Label(root, "Card Name", 15, Color.black, 26, 100, 230, 22);
            UI.Label(root, "Cost", 15, Color.black, 256, 100, 60, 22);
            UI.Label(root, "Element", 15, Color.black, 316, 100, 90, 22);
            UI.Label(root, "Description", 15, Color.black, 406, 100, 400, 22);
            _available = new ScrollList(root, 20, 124, 940, 640, new Color32(220, 220, 220, 255));
            UI.Button(root, "Add >", 20, 776, 200, 44, AddSelected, UI.ButtonGreen, 18);
            UI.Label(root, "Tip: click a card to preview it, then Add. Max 4 copies (Essence unlimited).", 15, Color.black, 236, 776, 720, 44, TextAnchor.MiddleLeft, FontStyle.Normal);

            // Deck
            UI.Label(root, "Deck", 15, Color.black, 980, 60, 60, 34, TextAnchor.MiddleLeft);
            _deckSelector = new Selector(root, 1040, 60, 360, 34, DeckStore.List());
            _deckSelector.Changed += name => SwitchDeck(name);
            _newDeckName = UI.Input(root, "New deck name", "", 980, 100, 260, 34, 16);
            UI.Button(root, "New Deck", 1250, 100, 150, 34, CreateDeck);
            UI.Label(root, "Card Name", 15, Color.black, 986, 140, 300, 22);
            UI.Label(root, "Copies", 15, Color.black, 1286, 140, 100, 22);
            _deckList = new ScrollList(root, 980, 164, 420, 600, new Color32(220, 220, 220, 255));
            UI.Button(root, "< Remove", 980, 776, 130, 44, RemoveSelected, new Color32(240, 170, 170, 255), 18);
            _save = UI.Button(root, "Save Deck", 1120, 776, 140, 44, SaveDeck, UI.ButtonGray, 18);
            _delete = UI.Button(root, "Delete Deck", 1270, 776, 130, 44, DeleteDeck, UI.ButtonGray, 16);
            _stats = UI.Label(root, "", 16, Color.black, 980, 828, 420, 26);
            _saveHint = UI.Label(root, "", 14, new Color32(160, 0, 0, 255), 980, 854, 420, 44, TextAnchor.UpperLeft, FontStyle.Normal);

            // Preview
            _preview = UI.Rect("Preview", root);
            UI.Place(_preview, 1420, 60, 480, 420);
            _attacks = UI.Label(root, "", 15, Color.black, 1420, 490, 480, 200, TextAnchor.UpperLeft, FontStyle.Normal, bestFit: true);
            _effects = UI.Label(root, "", 15, Color.black, 1420, 700, 480, 190, TextAnchor.UpperLeft, FontStyle.Normal, bestFit: true);

            PopulateAvailable();
            SwitchDeck(_deckSelector.Value);
        }

        // ───────────── Catalogue ─────────────

        void PopulateAvailable()
        {
            _available.Clear();
            string type = _type.Value;
            string element = _element.Value;
            string weapon = _weapon.Value;
            foreach (Card card in CardCatalogue.All)
            {
                if (type != "All Types" && card.Type.ToString() != type) continue;
                if (element != "All Elements" && card.Element.ToString() != element) continue;
                if (weapon != "All Weapons" && !HasWeapon(card, weapon)) continue;
                Card c = card;
                _available.AddRow(30, c == _selected && !_selectedFromDeck ? RowSelected : RowColor, () => Select(c, false),
                    (c.Name, 230), (c.Cost.ToString(), 60), (c.Element.ToString(), 90), (c.Description.Replace("\n", "  "), 540));
            }
        }

        static bool HasWeapon(Card card, string weapon)
        {
            if (card is LegendCard legend)
                return legend.PrimaryWeapon.ToString() == weapon || legend.SecondaryWeapon.ToString() == weapon;
            if (card is WeaponCard w)
                return w.Weapon.ToString() == weapon;
            return false;
        }

        void Select(Card card, bool fromDeck)
        {
            _selected = card;
            _selectedFromDeck = fromDeck;
            ShowPreview(card);
            PopulateAvailable();
            PopulateDeck();
        }

        void ShowPreview(Card card)
        {
            UI.Clear(_preview);
            _attacks.text = "";
            _effects.text = "";
            if (card == null) return;
            CardView.Create(_preview, card.Clone(), true, 0, 0, 2f);

            if (card is LegendCard legend)
            {
                var lines = legend.Attacks.Select(a =>
                {
                    string dmg = a.OneHitKO ? "KO" : $"{Attack.CalculateDamage(legend, a)} Damage";
                    return $"{a.WeaponDescription} - {dmg} - {a.Name}";
                }).ToList();
                _attacks.text = $"HP {legend.BaseHealth}   Att {legend.Power}   Weapons: {legend.PrimaryWeapon}, {legend.SecondaryWeapon}\n"
                    + (lines.Count > 0 ? "Attacks:\n" + string.Join("\n", lines) : "No attacks");
            }
            else
            {
                _attacks.text = card.Description;
            }

            // Stage / Essence descriptions already are their effect text.
            if (card is LegendCard || card is BattleCard)
                _effects.text = card.EffectDescription;
        }

        // ───────────── Deck ─────────────

        void SwitchDeck(string name)
        {
            _deckName = name;
            _confirmDelete = false;
            var errors = new List<string>();
            _deck = name != null ? DeckStore.Load(name, errors) : new List<int>();
            foreach (string e in errors) _app.Toast(e);
            _dirty = false;
            if (_selectedFromDeck) _selected = null;
            PopulateDeck();
        }

        void PopulateDeck()
        {
            _deckList.Clear();
            // Grouped by card, in the order the cards were first added (as the WinForms deck list).
            foreach (int id in _deck.Distinct())
            {
                Card card = CardCatalogue.Get(id);
                int copies = _deck.Count(x => x == id);
                _deckList.AddRow(30, card == _selected && _selectedFromDeck ? RowSelected : RowColor, () => Select(card, true),
                    (card.Name, 300), (copies.ToString(), 100));
            }

            int essence = _deck.Count(id => id == 0);
            _stats.text = _deckName == null
                ? "No deck selected - create one with New Deck."
                : $"{_deckName}{(_dirty ? " *" : "")}:  {_deck.Count} cards ({essence} Essence)";

            string saveError = _deckName == null ? "Create a deck first." : DeckRules.ValidateForSave(_deck);
            _save.interactable = saveError == null;
            _delete.interactable = _deckName != null;
            UI.ButtonText(_delete).text = _confirmDelete ? "Confirm?" : "Delete Deck";
            string playHint = _deck.Count < DeckRules.MinimumCardsToPlay
                ? $"Note: a deck needs {DeckRules.MinimumCardsToPlay} cards to play a match."
                : "";
            _saveHint.text = saveError != null ? saveError + (playHint.Length > 0 ? "\n" + playHint : "") : playHint;
        }

        void AddSelected()
        {
            if (_selected == null) { _app.Toast("Select a card first."); return; }
            if (_deckName == null) { _app.Toast("Create a deck first (New Deck)."); return; }
            string error = DeckRules.CanAddCard(_deck, _selected.Id);
            if (error != null) { _app.Toast(error); return; }
            _deck.Add(_selected.Id);
            _dirty = true;
            _confirmDelete = false;
            PopulateDeck();
        }

        void RemoveSelected()
        {
            if (_selected == null || !_deck.Contains(_selected.Id)) { _app.Toast("Select a card in your deck first."); return; }
            int index = _deck.LastIndexOf(_selected.Id);
            _deck.RemoveAt(index);
            if (!_deck.Contains(_selected.Id) && _selectedFromDeck) _selected = null;
            _dirty = true;
            _confirmDelete = false;
            PopulateDeck();
        }

        void SaveDeck()
        {
            if (_deckName == null) return;
            string error = DeckRules.ValidateForSave(_deck);
            if (error != null) { _app.Toast(error); return; }
            try
            {
                DeckStore.Save(_deckName, _deck);
                _dirty = false;
                _app.Toast($"{_deckName} saved successfully!");
            }
            catch (Exception e)
            {
                _app.Toast($"Error saving deck: {e.Message}");
            }
            PopulateDeck();
        }

        void CreateDeck()
        {
            string name = DeckStore.SanitizeName(_newDeckName.text);
            if (string.IsNullOrWhiteSpace(_newDeckName.text)) { _app.Toast("Enter a name for the new deck."); return; }
            if (DeckStore.Exists(name)) { _app.Toast("A deck with that name already exists."); return; }
            DeckStore.Save(name, Enumerable.Empty<int>());
            _newDeckName.text = "";
            _deckSelector.SetOptions(DeckStore.List(), name);
            SwitchDeck(name);
        }

        void DeleteDeck()
        {
            if (_deckName == null) return;
            if (!_confirmDelete)
            {
                _confirmDelete = true;
                PopulateDeck();
                return;
            }
            DeckStore.Delete(_deckName);
            _app.Toast($"{_deckName} deleted.");
            _deckSelector.SetOptions(DeckStore.List());
            SwitchDeck(_deckSelector.Value);
        }

        void BackToMenu()
        {
            if (_dirty) _app.Toast("Unsaved changes were discarded.");
            _app.ShowMenu();
        }
    }
}
