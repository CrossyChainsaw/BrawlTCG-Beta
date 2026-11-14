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

        public static void Delete(string name)
        {
            string path = PathFor(name);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>Creates two ready-to-play decks on first launch so a match can be started right away.</summary>
        public static void EnsureStarterDecks()
        {
            Directory.CreateDirectory(Folder);
            if (Directory.GetFiles(Folder, "*.txt").Length > 0) return;

            Save("Starter - Fire", Build(
                (0, 14), (2004, 2), (2005, 3), (2006, 2), (2001, 2), (2007, 2),
                (25003, 4), (24000, 4), (15000, 3), (23000, 2), (508, 2)));
            Save("Starter - Nature", Build(
                (0, 14), (1000, 3), (1003, 2), (1007, 2), (1008, 2), (1001, 2),
                (24001, 4), (13000, 3), (18000, 3), (19000, 2), (501, 2), (101, 1)));
        }

        static List<int> Build(params (int id, int count)[] entries)
        {
            var ids = new List<int>();
            foreach (var (id, count) in entries)
                ids.AddRange(Enumerable.Repeat(id, count));
            return ids;
        }
    }
}
