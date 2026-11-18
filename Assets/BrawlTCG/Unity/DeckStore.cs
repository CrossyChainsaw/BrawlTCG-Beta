using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>Decks are stored as "&lt;name&gt;.txt" (one card id per line) in persistentDataPath/Decks.</summary>
    public static class DeckStore
    {
        public static string Folder => Path.Combine(Application.persistentDataPath, "Decks");

        static string PathFor(string name) => Path.Combine(Folder, SanitizeName(name) + ".txt");

        public static string SanitizeName(string name)
        {
            name = (name ?? "").Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Length == 0 ? "Deck" : name;
        }

        public static List<string> List()
        {
            Directory.CreateDirectory(Folder);
            return Directory.GetFiles(Folder, "*.txt")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool Exists(string name) => File.Exists(PathFor(name));

        public static List<int> Load(string name, List<string> errors = null)
        {
            string path = PathFor(name);
            if (!File.Exists(path))
            {
                errors?.Add($"Deck file not found: {path}");
                return new List<int>();
            }
            return DeckFormat.Parse(File.ReadAllText(path), errors);
        }

        public static void Save(string name, IEnumerable<int> ids)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(PathFor(name), DeckFormat.Serialize(ids));
        }

        /// <summary>
        /// Renames a deck file. Returns null on success, otherwise why not. Changing only the case of the name works too
        /// (via a temporary name, as Windows treats both as the same file). The menu's remembered deck follows the rename.
        /// </summary>
        public static string Rename(string oldName, string newName)
        {
            string from = PathFor(oldName), to = PathFor(newName);
            if (!File.Exists(from)) return "That deck no longer exists.";
            if (from == to) return null;
            bool caseOnly = string.Equals(from, to, StringComparison.OrdinalIgnoreCase);
            if (!caseOnly && File.Exists(to)) return "A deck with that name already exists.";
            try
            {
                if (caseOnly)
                {
                    string temp = from + ".renaming";
                    File.Move(from, temp);
                    File.Move(temp, to);
                }
                else File.Move(from, to);
            }
            catch (Exception e)
            {
                return "Could not rename the deck: " + e.Message;
            }
            foreach (string key in new[] { "BrawlTCG.Deck", "BrawlTCG.Deck2" })
                if (PlayerPrefs.GetString(key, null) == SanitizeName(oldName)) PlayerPrefs.SetString(key, SanitizeName(newName));
            PlayerPrefs.Save();
            return null;
        }

        public static void Delete(string name)
        {
            string path = PathFor(name);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>
        /// Builds up to v1.0 Beta saved under the Unity defaults "DefaultCompany/My project". The first time a build with
        /// the real names starts, it copies those decks (and the starter ledger) over. The old folder is left untouched.
        /// </summary>
        static void MigrateFromOldFolder()
        {
            try
            {
                if (Directory.Exists(Folder)) return;
                string root = Directory.GetParent(Directory.GetParent(Application.persistentDataPath).FullName).FullName;
                string old = Path.Combine(root, "DefaultCompany", "My project", "Decks");
                if (!Directory.Exists(old) || Path.GetFullPath(old) == Path.GetFullPath(Folder)) return;

                Directory.CreateDirectory(Folder);
                foreach (string file in Directory.GetFiles(old))
                    File.Copy(file, Path.Combine(Folder, Path.GetFileName(file)), overwrite: false);
                Debug.Log($"Copied decks from {old}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not copy decks from the old folder: " + e.Message);
            }
        }

        /// <summary>
        /// Hands out the six premade decks once each and cleans up decks earlier versions handed out (see
        /// <see cref="StarterDecks.Retired"/>). A deck that was already given is never recreated, so deleting one keeps it
        /// deleted; an existing deck with the same name is never overwritten.
        /// </summary>
        public static void EnsureStarterDecks()
        {
            MigrateFromOldFolder();
            Directory.CreateDirectory(Folder);
            string ledger = Path.Combine(Folder, ".starters"); // not a .txt, so it never shows up as a deck
            var given = new HashSet<string>(File.Exists(ledger) ? File.ReadAllLines(ledger) : Array.Empty<string>());
            bool changed = false;

            // Old versions first: an untouched copy of a retired deck is removed, and if that name is still bundled
            // (the deck was re-tuned) the new version is handed out in its place below.
            var current = new HashSet<string>(StarterDecks.Premade.Select(s => s.Name));
            foreach (string removed in RemoveRetiredDecks(given))
            {
                if (current.Contains(removed)) given.Remove(removed);
                changed = true;
            }

            foreach (StarterDecks.Starter s in StarterDecks.Premade)
            {
                if (given.Contains(s.Name)) continue;
                if (!Exists(s.Name)) Save(s.Name, s.Cards);
                given.Add(s.Name);
                changed = true;
            }
            if (changed) File.WriteAllLines(ledger, given.OrderBy(n => n));
        }

        /// <summary>
        /// Deletes decks an earlier version handed out that are no longer bundled, renamed or re-tuned, but only while
        /// the file is still exactly the deck that was handed out: a deck the player edited is theirs and stays.
        /// Returns the names that were removed.
        /// </summary>
        static List<string> RemoveRetiredDecks(HashSet<string> given)
        {
            var removed = new List<string>();
            foreach (StarterDecks.Starter s in StarterDecks.Retired)
            {
                if (!given.Contains(s.Name) || !Exists(s.Name)) continue;
                List<int> saved = Load(s.Name);
                if (!saved.OrderBy(i => i).SequenceEqual(s.Cards.OrderBy(i => i))) continue;
                Delete(s.Name);
                removed.Add(s.Name);
            }
            return removed;
        }
    }
}
