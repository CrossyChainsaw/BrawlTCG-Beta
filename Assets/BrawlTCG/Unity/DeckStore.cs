using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrawlTCG.Core;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>The player's own decks ("My decks"): "&lt;name&gt;.txt" files (one card id per line) in persistentDataPath/Decks.</summary>
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
        /// Premade decks are no longer deck files (see <see cref="Collection"/>). Earlier versions handed them out as
        /// editable files and listed every name they gave in the ".starters" ledger: those files are deleted once, edited or
        /// not, together with the ledger. Then any remaining file whose name belongs to a premade deck gets "(mine)"
        /// added, so every deck name means one deck.
        /// </summary>
        public static void RemoveHandedOutDecks()
        {
            MigrateFromOldFolder();
            Directory.CreateDirectory(Folder);
            string ledger = Path.Combine(Folder, ".starters"); // not a .txt, so it never showed up as a deck
            try
            {
                if (File.Exists(ledger))
                {
                    foreach (string name in File.ReadAllLines(ledger))
                        if (name.Trim().Length > 0) Delete(name.Trim());
                    File.Delete(ledger);
                }
                foreach (string name in List())
                {
                    if (!DeckLibrary.IsReserved(name)) continue;
                    string free = name + " (mine)";
                    for (int i = 2; Exists(free) || DeckLibrary.IsReserved(free); i++) free = $"{name} (mine {i})";
                    Rename(name, free);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not clean up the old premade decks: " + e.Message);
            }
        }
    }
}
