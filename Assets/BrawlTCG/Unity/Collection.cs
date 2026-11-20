using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BrawlTCG.Core;
using UnityEngine;
using Random = System.Random;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Which premade decks (<see cref="StarterDecks.Premade"/>) this player owns. Premade decks are never deck files the
    /// player can edit:
    /// <list type="bullet">
    /// <item><b>Starter decks:</b> <see cref="StarterDecks.StarterDeckCount"/> decks from <see cref="StarterDecks.StarterPool"/>,
    /// picked from a hash of this PC's hardware ID. Reinstalling the game, or deleting its save data, picks the same ones again.</item>
    /// <item><b>Bot decks:</b> every other premade deck, unlocked by beating the bot while it plays that deck.</item>
    /// </list>
    /// Saved in persistentDataPath/collection.dat with a checksum, so editing the file by hand loses the unlocks
    /// instead of granting decks.
    /// </summary>
    public static class Collection
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "collection.dat");
        const string Salt = "BrawlTCG/collection/v1/7f3a9c";
        const string PrefFallbackSeed = "BrawlTCG.CollectionSeed";

        static List<string> _starters;
        static HashSet<string> _unlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _starters = null;
            _unlocked = null;
        }

        /// <summary>Call once at startup: removes the old editable premade copies, then loads (or picks) the collection.</summary>
        public static void Initialize()
        {
            DeckStore.RemoveHandedOutDecks();
            Load();
        }

        // ───────────── Queries ─────────────

        /// <summary>The player's starter decks, strongest first.</summary>
        public static List<StarterDecks.Starter> Starters
        {
            get { Load(); return StarterDecks.Premade.Where(s => _starters.Contains(s.Name)).ToList(); }
        }

        /// <summary>Every premade deck that is not one of the player's starters (locked or not), easiest unlock first.</summary>
        public static List<StarterDecks.Starter> BotDecks
        {
            get
            {
                Load();
                return StarterDecks.Premade.Where(s => !_starters.Contains(s.Name))
                    .OrderBy(s => s.UnlockLevel).ThenBy(s => s.Rank).ToList();
            }
        }

        /// <summary>The bot decks the player has unlocked, strongest first.</summary>
        public static List<StarterDecks.Starter> UnlockedBotDecks => BotDecks.Where(s => IsUnlocked(s.Name)).OrderBy(s => s.Rank).ToList();

        public static bool IsStarter(string name) { Load(); return name != null && _starters.Contains(name); }
        public static bool IsUnlocked(string name) { Load(); return name != null && _unlocked.Contains(name); }
        public static bool Owns(string name) => IsStarter(name) || IsUnlocked(name);

        /// <summary>
        /// The deck the bot plays at <paramref name="level"/>: one the player doesn't own yet, or, once they own the whole
        /// pool, any deck of it.
        /// </summary>
        public static StarterDecks.Starter PickBotDeck(Bot.Level level, Random rng)
        {
            List<StarterDecks.Starter> pool = StarterDecks.ForLevel(level);
            List<StarterDecks.Starter> notOwned = pool.Where(s => !Owns(s.Name)).ToList();
            List<StarterDecks.Starter> from = notOwned.Count > 0 ? notOwned : pool;
            return from[rng.Next(from.Count)];
        }

        /// <summary>How many decks of the bot's pool at <paramref name="level"/> the player doesn't own yet, and the pool size.</summary>
        public static (int locked, int total) Progress(Bot.Level level)
        {
            List<StarterDecks.Starter> pool = StarterDecks.ForLevel(level);
            return (pool.Count(s => !Owns(s.Name)), pool.Count);
        }

        // ───────────── Changes ─────────────

        /// <summary>Unlocks a bot deck. Returns true only the first time (false for starters, owned and unknown decks).</summary>
        public static bool Unlock(string name)
        {
            Load();
            if (StarterDecks.Find(name) == null || Owns(name)) return false;
            _unlocked.Add(name);
            Save();
            return true;
        }

        // ───────────── Storage ─────────────

        static void Load()
        {
            if (_starters != null) return;
            _starters = new List<string>();
            _unlocked = new HashSet<string>();
            bool changed = false;
            try
            {
                if (File.Exists(FilePath))
                {
                    string[] lines = File.ReadAllLines(FilePath);
                    string body = string.Join("\n", lines.Where(l => !l.StartsWith("sig:")));
                    string sig = lines.FirstOrDefault(l => l.StartsWith("sig:"))?.Substring(4);
                    if (sig == Sign(body))
                    {
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("starter:")) _starters.Add(line.Substring(8));
                            else if (line.StartsWith("unlocked:")) _unlocked.Add(line.Substring(9));
                        }
                    }
                    else
                    {
                        Debug.LogWarning("collection.dat was changed outside the game; its unlocked decks are not counted.");
                        changed = true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not read the deck collection: " + e.Message);
            }

            // Decks that no longer ship (renamed or removed in an update) are dropped; missing starters are picked again.
            var pool = StarterDecks.StarterPool.Select(s => s.Name).ToList();
            int before = _starters.Count + _unlocked.Count;
            _starters = _starters.Where(pool.Contains).Distinct().Take(StarterDecks.StarterDeckCount).ToList();
            _unlocked.RemoveWhere(n => StarterDecks.Find(n) == null || _starters.Contains(n));
            if (_starters.Count + _unlocked.Count != before) changed = true;
            if (_starters.Count < StarterDecks.StarterDeckCount)
            {
                foreach (string pick in PickStarters(pool))
                {
                    if (_starters.Count >= StarterDecks.StarterDeckCount) break;
                    if (_starters.Contains(pick)) continue;
                    _starters.Add(pick);
                    _unlocked.Remove(pick);
                }
                changed = true;
            }
            if (changed) Save();
        }

        static void Save()
        {
            try
            {
                var lines = new List<string> { "# BrawlTCG deck collection. Edits make the game ignore the unlocked decks." };
                lines.AddRange(_starters.Select(n => "starter:" + n));
                lines.AddRange(_unlocked.OrderBy(n => n).Select(n => "unlocked:" + n));
                string body = string.Join("\n", lines);
                lines.Add("sig:" + Sign(body));
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, lines);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not save the deck collection: " + e.Message);
            }
        }

        /// <summary>The pool in this PC's own fixed order: the first entries are its starter decks.</summary>
        static List<string> PickStarters(List<string> pool)
        {
            var rng = new Random(SeedFromText(DeviceKey()));
            var order = pool.OrderBy(n => n, StringComparer.Ordinal).ToList(); // independent of rank changes
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order;
        }

        /// <summary>
        /// A stable key for this PC. Unity's device ID is a hash of the hardware, so it survives reinstalls. Where a
        /// platform has none, a random key is kept in PlayerPrefs (the registry on Windows, which also outlives the game folder).
        /// </summary>
        static string DeviceKey()
        {
            string id = SystemInfo.deviceUniqueIdentifier;
            if (!string.IsNullOrEmpty(id) && id != SystemInfo.unsupportedIdentifier) return id;
            string saved = PlayerPrefs.GetString(PrefFallbackSeed, "");
            if (saved.Length == 0)
            {
                saved = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PrefFallbackSeed, saved);
                PlayerPrefs.Save();
            }
            return saved;
        }

        static int SeedFromText(string text)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Salt + "|starters|" + text));
                return BitConverter.ToInt32(hash, 0) & 0x7fffffff;
            }
        }

        static string Sign(string body)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Salt + "|" + body.Replace("\r", "")));
                return BitConverter.ToString(hash, 0, 16).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// Every deck the player can play: their starter decks, unlocked bot decks and their own decks (files). Premade
    /// names are reserved, so a name always means one deck.
    /// </summary>
    public static class DeckLibrary
    {
        /// <summary>Names for deck pickers: starter decks, then unlocked bot decks, then the player's own decks.</summary>
        public static List<string> Playable()
        {
            var names = Collection.Starters.Select(s => s.Name).ToList();
            names.AddRange(Collection.UnlockedBotDecks.Select(s => s.Name));
            names.AddRange(DeckStore.List().Where(n => !names.Contains(n)));
            return names;
        }

        /// <summary>The premade deck with this name if the player owns it, else null.</summary>
        public static StarterDecks.Starter OwnedPremade(string name)
        {
            StarterDecks.Starter s = StarterDecks.Find(name);
            return s != null && Collection.Owns(name) ? s : null;
        }

        public static List<int> Load(string name, List<string> errors = null)
        {
            StarterDecks.Starter premade = OwnedPremade(name);
            return premade != null ? premade.Cards.ToList() : DeckStore.Load(name, errors);
        }

        /// <summary>"Starter deck", "Bot deck" or null for the player's own decks.</summary>
        public static string KindOf(string name) =>
            Collection.IsStarter(name) ? "Starter deck" : Collection.IsUnlocked(name) ? "Bot deck" : null;

        /// <summary>Whether a deck name belongs to a premade deck (and so can't be used for the player's own decks).</summary>
        public static bool IsReserved(string name) =>
            StarterDecks.Premade.Any(s => string.Equals(s.Name, DeckStore.SanitizeName(name), StringComparison.OrdinalIgnoreCase));

        /// <summary>A premade deck's face: its most-used stage card (if any) and its most-used legends, three cards in all.</summary>
        public static List<int> KeyCards(StarterDecks.Starter deck)
        {
            var counts = deck.Cards.Where(id => id != 0).GroupBy(id => id)
                .Select(g => (id: g.Key, n: g.Count(), card: CardCatalogue.Get(g.Key)))
                .OrderByDescending(x => x.n).ThenByDescending(x => x.card.Cost).ToList();
            var picks = new List<int>();
            var stage = counts.FirstOrDefault(x => x.card.Type == CardType.Stage);
            if (stage.card != null) picks.Add(stage.id);
            picks.AddRange(counts.Where(x => x.card.Type == CardType.Legend).Select(x => x.id).Take(3 - picks.Count));
            if (picks.Count < 3) picks.AddRange(counts.Select(x => x.id).Where(id => !picks.Contains(id)).Take(3 - picks.Count));
            return picks;
        }
    }
}
