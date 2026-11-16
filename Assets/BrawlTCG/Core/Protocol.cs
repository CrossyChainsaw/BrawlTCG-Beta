using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BrawlTCG.Core
{
    /// <summary>
    /// Line based TCP protocol (lockstep).
    ///
    /// Handshake: each side sends one HELLO line containing its name, a random seed and its shuffled deck.
    ///   P2P: the host is player 0 (goes first).
    ///   Relay server (BrawlTCG-Server): the server forwards the HELLO lines, then answers DIST_TURNS_REQUEST
    ///   with DIST_TURNS:true for the player that goes first.
    /// Both clients seed the shared game RNG with seedA ^ seedB, so every random effect resolves identically
    /// on both sides without extra messages.
    ///
    /// In game: CMD:&lt;serialized GameCommand&gt; lines, and QUIT when a player leaves.
    /// </summary>
    public static class Protocol
    {
        public const string Version = "BTCG-UNITY-2"; // 2: 50 starting health, DiscardCard command
        public const int DefaultPort = 5000;
        public const string HelloPrefix = "HELLO";
        public const string CommandPrefix = "CMD:";
        public const string Quit = "QUIT";
        public const string DistTurnsRequest = "DIST_TURNS_REQUEST";
        public const string DistTurnsFirst = "DIST_TURNS:true";
        public const string DistTurnsSecond = "DIST_TURNS:false";

        public sealed class Hello
        {
            public string Name;
            public int Seed;
            public List<int> Deck;
        }

        public static string EncodeHello(string name, int seed, IEnumerable<int> deck)
        {
            return string.Join(":", HelloPrefix, Version, Uri.EscapeDataString(name ?? ""),
                seed.ToString(CultureInfo.InvariantCulture),
                string.Join(",", deck.Select(i => i.ToString(CultureInfo.InvariantCulture))));
        }

        public static bool TryDecodeHello(string line, out Hello hello, out string error)
        {
            hello = null;
            error = null;
            if (string.IsNullOrEmpty(line)) { error = "No data received from peer."; return false; }
            string[] parts = line.Split(':');
            if (parts.Length != 5 || parts[0] != HelloPrefix) { error = "Invalid data received from peer."; return false; }
            if (parts[1] != Version) { error = $"Peer runs an incompatible version ({parts[1]})."; return false; }
            if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed)) { error = "Invalid seed from peer."; return false; }

            var deck = new List<int>();
            foreach (string s in parts[4].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || !CardCatalogue.Exists(id))
                {
                    error = $"Peer deck contains an unknown card ({s}).";
                    return false;
                }
                deck.Add(id);
            }
            string deckError = DeckRules.ValidateForPlay(deck);
            if (deckError != null) { error = "Peer deck is invalid: " + deckError; return false; }

            hello = new Hello { Name = SanitizeName(Uri.UnescapeDataString(parts[2])), Seed = seed, Deck = deck };
            return true;
        }

        public static string SanitizeName(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length > 24) name = name.Substring(0, 24);
            return name.Length == 0 ? "Player" : name;
        }
    }
}
