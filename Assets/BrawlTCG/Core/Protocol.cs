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
        /// <summary>
        /// The game version (Application.version, "major.minor.patch"), set by the Unity layer at startup. It is sent in
        /// HELLO; players can only play each other when major and minor match, so a patch must never change the rules.
        /// Clients up to v1.1.0 sent a protocol tag ("BTCG-UNITY-3") in its place instead of a version number.
        /// </summary>
        public static string GameVersion = "0.0.0";
        public const string VersionMismatchPrefix = "Version mismatch: ";
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
            /// <summary>Legend card id shown as the avatar, or -1 for the name's initial.</summary>
            public int Avatar = -1;
        }

        /// <summary>
        /// The name field also carries profile extras after the escaped name, as "&amp;key=value" pairs (the escaped name
        /// never contains a raw '&amp;'). Keeping HELLO at five fields lets pre-v1.2.0 clients still reach their version check.
        /// Unknown keys are ignored, so later patches can add extras without breaking compatibility.
        /// </summary>
        public static string EncodeHello(string name, int seed, IEnumerable<int> deck, int avatar = -1)
        {
            string profile = Uri.EscapeDataString(name ?? "");
            if (ValidAvatar(avatar) >= 0) profile += "&a=" + avatar.ToString(CultureInfo.InvariantCulture);
            return string.Join(":", HelloPrefix, GameVersion, profile,
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
            string[] profile = parts[2].Split('&');
            string peerName = SanitizeName(Uri.UnescapeDataString(profile[0]));
            int avatar = -1;
            foreach (string extra in profile.Skip(1))
                if (extra.StartsWith("a=", StringComparison.Ordinal) &&
                    int.TryParse(extra.Substring(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int a))
                    avatar = ValidAvatar(a);
            error = VersionMismatch(GameVersion, parts[1], peerName);
            if (error != null) return false;
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

            hello = new Hello { Name = peerName, Seed = seed, Deck = deck, Avatar = avatar };
            return true;
        }

        /// <summary>
        /// Null when the two versions can play each other (same major.minor; the patch number may differ), otherwise a
        /// short message for this player saying who is outdated.
        /// </summary>
        public static string VersionMismatch(string mine, string theirs, string peerName)
        {
            bool mineOk = TryParseVersion(mine, out int myMajor, out int myMinor);
            bool theirsOk = TryParseVersion(theirs, out int theirMajor, out int theirMinor);
            if (mineOk && theirsOk && myMajor == theirMajor && myMinor == theirMinor) return null;

            // An unparsable peer version is a pre-v1.2.0 client, which sent a protocol tag instead.
            bool peerOlder = !theirsOk || (mineOk && (theirMajor, theirMinor).CompareTo((myMajor, myMinor)) < 0);
            string theirLabel = theirsOk ? "v" + theirs : "an older version";
            return peerOlder
                ? $"{VersionMismatchPrefix}you are on v{mine}, {peerName} is on {theirLabel} (outdated). {peerName} needs to update."
                : $"{VersionMismatchPrefix}you are on v{mine} (outdated), {peerName} is on v{theirs}. Update your game.";
        }

        static bool TryParseVersion(string version, out int major, out int minor)
        {
            major = minor = 0;
            string[] p = (version ?? "").Split('.');
            return p.Length >= 2
                && int.TryParse(p[0], NumberStyles.None, CultureInfo.InvariantCulture, out major)
                && int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor);
        }

        /// <summary>The id when it is a legend (a valid avatar), otherwise -1.</summary>
        public static int ValidAvatar(int id) => id >= 0 && CardCatalogue.Exists(id) && CardCatalogue.Get(id) is LegendCard ? id : -1;

        public static string SanitizeName(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length > 24) name = name.Substring(0, 24);
            return name.Length == 0 ? "Player" : name;
        }
    }
}
