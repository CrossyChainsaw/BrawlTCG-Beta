using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using BrawlTCG.Core;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace BrawlTCG.UnityApp
{
    public sealed class MatchSetup
    {
        public Game Game;
        /// <summary>Null for local hot-seat games.</summary>
        public NetSession Net;
        /// <summary>The player controlled on this machine (network games).</summary>
        public int LocalIndex;
        public bool HotSeat => Net == null;
    }

    public sealed class MenuScreen : MonoBehaviour
    {
        const string PrefName = "BrawlTCG.Name", PrefName2 = "BrawlTCG.Name2", PrefDeck = "BrawlTCG.Deck",
            PrefDeck2 = "BrawlTCG.Deck2", PrefIp = "BrawlTCG.Ip", PrefPort = "BrawlTCG.Port";

        App _app;
        InputField _name, _name2, _ip, _port;
        Selector _deck, _deck2;
        Text _status;
        Button _cancel;
        readonly List<Button> _actionButtons = new List<Button>();
        NetSession.Listener _listener;
        NetSession _pending;
        bool _busy;

        public void Build(App app)
        {
            _app = app;
            Transform root = transform;

            var bg = Art.Get("MenuBackground");
            if (bg != null) UI.Picture(root, bg, 0, 0, App.Width, App.Height, cover: true);

            UI.Label(root, "BrawlTCG", 72, Color.white, 780, 40, 1100, 100, TextAnchor.UpperLeft);

            List<string> decks = DeckStore.List();

            // EDIT DECK (as in the WinForms menu)
            UI.Button(root, "EDIT DECK", 40, 40, 700, 155, () => _app.ShowDeckBuilder(), UI.MediumPurple, 60);

            // ── Play online ──
            var online = UI.Panel(root, new Color(1, 1, 1, 0.92f), 40, 215, 700, 380, "Online").transform;
            UI.Label(online, "Play Online", 22, Color.black, 12, 8, 400, 30);
            UI.Label(online, "Name", 16, Color.black, 12, 48, 120, 34, TextAnchor.MiddleLeft);
            _name = UI.Input(online, "Username", PlayerPrefs.GetString(PrefName, ""), 130, 48, 260, 34);
            UI.Label(online, "Deck", 16, Color.black, 12, 90, 120, 34, TextAnchor.MiddleLeft);
            _deck = new Selector(online, 130, 90, 400, 34, decks, PlayerPrefs.GetString(PrefDeck, null));
            UI.Label(online, "Host IP / Port", 16, Color.black, 12, 132, 120, 34, TextAnchor.MiddleLeft);
            _ip = UI.Input(online, "127.0.0.1", PlayerPrefs.GetString(PrefIp, "127.0.0.1"), 130, 132, 260, 34);
            _port = UI.Input(online, "5000", PlayerPrefs.GetInt(PrefPort, Protocol.DefaultPort).ToString(CultureInfo.InvariantCulture), 400, 132, 130, 34);

            _actionButtons.Add(UI.Button(online, "Host Game (P2P or LAN)", 12, 182, 260, 36, HostP2P));
            UI.Label(online, "(P2P Connection: Direct connection to each others pc)", 14, Color.black, 282, 182, 410, 36, TextAnchor.MiddleLeft, FontStyle.Normal);
            _actionButtons.Add(UI.Button(online, "Join Game (P2P or LAN)", 12, 226, 260, 36, JoinP2P));
            UI.Label(online, "(Connects to the Host IP above)", 14, Color.black, 282, 226, 410, 36, TextAnchor.MiddleLeft, FontStyle.Normal);
            _actionButtons.Add(UI.Button(online, "Multiplayer (Client-Server)", 12, 270, 260, 36, JoinServer));
            UI.Label(online, "(Client-Server: Someone has to run the server)", 14, Color.black, 282, 270, 410, 36, TextAnchor.MiddleLeft, FontStyle.Normal);
            _status = UI.Label(online, "Status: Not Connected", 16, Color.black, 12, 320, 520, 44, TextAnchor.MiddleLeft, FontStyle.Bold, bestFit: true);
            _cancel = UI.Button(online, "Cancel", 560, 324, 128, 36, CancelConnect);
            _cancel.gameObject.SetActive(false);

            // ── Local hot-seat ──
            var local = UI.Panel(root, new Color(1, 1, 1, 0.92f), 40, 615, 700, 210, "Local").transform;
            UI.Label(local, "Local Game (Hot-Seat, same PC)", 22, Color.black, 12, 8, 600, 30);
            UI.Label(local, "Player 1 uses the name and deck above.", 15, Color.black, 12, 44, 600, 24, TextAnchor.MiddleLeft, FontStyle.Normal);
            UI.Label(local, "Player 2", 16, Color.black, 12, 76, 120, 34, TextAnchor.MiddleLeft);
            _name2 = UI.Input(local, "Player 2 name", PlayerPrefs.GetString(PrefName2, "Player 2"), 130, 76, 260, 34);
            UI.Label(local, "Deck", 16, Color.black, 12, 118, 120, 34, TextAnchor.MiddleLeft);
            _deck2 = new Selector(local, 130, 118, 400, 34, decks, PlayerPrefs.GetString(PrefDeck2, decks.Count > 1 ? decks[1] : null));
            _actionButtons.Add(UI.Button(local, "Start Hot-Seat Game", 12, 162, 260, 36, StartHotSeat));

            // ── Footer ──
            UI.Button(root, "Open Decks Folder", 40, 845, 230, 40, OpenDecksFolder);
            UI.Button(root, "Quit", 290, 845, 120, 40, Quit);
            UI.Label(root, "Guys don't hate this will change trust", 16, Color.black, 430, 845, 400, 40, TextAnchor.MiddleLeft, FontStyle.Normal);
        }

        void OnDestroy()
        {
            _listener?.Dispose();
            _pending?.Dispose();
        }

        void SetStatus(string s) => _status.text = s;

        void SetBusy(bool busy)
        {
            _busy = busy;
            _cancel.gameObject.SetActive(busy);
            foreach (Button b in _actionButtons) b.interactable = !busy;
        }

        void SavePrefs()
        {
            PlayerPrefs.SetString(PrefName, _name.text);
            PlayerPrefs.SetString(PrefName2, _name2.text);
            if (_deck.Value != null) PlayerPrefs.SetString(PrefDeck, _deck.Value);
            if (_deck2.Value != null) PlayerPrefs.SetString(PrefDeck2, _deck2.Value);
            PlayerPrefs.SetString(PrefIp, _ip.text);
            PlayerPrefs.SetInt(PrefPort, ParsePort());
            PlayerPrefs.Save();
        }

        int ParsePort()
        {
            return int.TryParse(_port.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int p) && p > 0 && p < 65536
                ? p : Protocol.DefaultPort;
        }

        /// <summary>Name + shuffled deck, or null (with a toast) when invalid.</summary>
        (string name, List<int> deck)? PrepareLocalPlayer(InputField nameField, Selector deckSelector)
        {
            if (string.IsNullOrWhiteSpace(nameField.text))
            {
                _app.Toast("Please enter your name.");
                return null;
            }
            if (deckSelector.Value == null)
            {
                _app.Toast("No deck selected. Build one with EDIT DECK first.");
                return null;
            }
            var errors = new List<string>();
            List<int> deck = DeckStore.Load(deckSelector.Value, errors);
            foreach (string e in errors) _app.Toast(e);
            string invalid = DeckRules.ValidateForPlay(deck);
            if (invalid != null)
            {
                _app.Toast(invalid, 5);
                return null;
            }
            return (Protocol.SanitizeName(nameField.text), DeckRules.Shuffle(deck, new Random()));
        }

        // ───────────── Hot-seat ─────────────

        void StartHotSeat()
        {
            if (_busy) return;
            var p1 = PrepareLocalPlayer(_name, _deck);
            if (p1 == null) return;
            var p2 = PrepareLocalPlayer(_name2, _deck2);
            if (p2 == null) return;
            SavePrefs();
            var game = new Game(p1.Value.name, p1.Value.deck, p2.Value.name, p2.Value.deck, new Random().Next());
            _app.StartMatch(new MatchSetup { Game = game, Net = null, LocalIndex = 0 });
        }

        // ───────────── Networking ─────────────

        async void HostP2P()
        {
            if (_busy) return;
            var me = PrepareLocalPlayer(_name, _deck);
            if (me == null) return;
            SavePrefs();
            int port = ParsePort();
            SetBusy(true);
            NetSession session = null;
            try
            {
                _listener = new NetSession.Listener(port);
                SetStatus($"Waiting for second player on port {port}...");
                session = await _listener.AcceptAsync();
                _listener = null;
                if (this == null) { session.Dispose(); return; }
                _pending = session;
                SetStatus("Someone is joining...");

                int mySeed = new Random().Next();
                session.Send(Protocol.EncodeHello(me.Value.name, mySeed, me.Value.deck));
                Protocol.Hello peer = await ReadHello(session);
                if (this == null) { session.Dispose(); return; }

                // Host always starts: host is player 0.
                var game = new Game(me.Value.name, me.Value.deck, peer.Name, peer.Deck, mySeed ^ peer.Seed);
                Launch(game, session, 0);
            }
            catch (Exception e)
            {
                session?.Dispose();
                if (this != null) SetStatus(_cancelled ? "Status: Not Connected" : $"Error hosting game: {e.Message}");
            }
            finally
            {
                _cancelled = false;
                _pending = null;
                _listener?.Dispose();
                _listener = null;
                if (this != null) SetBusy(false);
            }
        }

        async void JoinP2P()
        {
            if (_busy) return;
            var me = PrepareLocalPlayer(_name, _deck);
            if (me == null) return;
            if (string.IsNullOrWhiteSpace(_ip.text)) { _app.Toast("Invalid IP address."); return; }
            SavePrefs();
            SetBusy(true);
            NetSession session = null;
            try
            {
                SetStatus("Joining host...");
                session = await NetSession.ConnectAsync(_ip.text.Trim(), ParsePort());
                if (this == null) { session.Dispose(); return; }
                _pending = session;
                SetStatus("Connected to host!");

                int mySeed = new Random().Next();
                session.Send(Protocol.EncodeHello(me.Value.name, mySeed, me.Value.deck));
                Protocol.Hello host = await ReadHello(session);
                if (this == null) { session.Dispose(); return; }

                var game = new Game(host.Name, host.Deck, me.Value.name, me.Value.deck, host.Seed ^ mySeed);
                Launch(game, session, 1);
            }
            catch (Exception e)
            {
                session?.Dispose();
                if (this != null) SetStatus(_cancelled ? "Status: Not Connected" : $"Error joining game: {e.Message}");
            }
            finally
            {
                _cancelled = false;
                _pending = null;
                if (this != null) SetBusy(false);
            }
        }

        async void JoinServer()
        {
            if (_busy) return;
            var me = PrepareLocalPlayer(_name, _deck);
            if (me == null) return;
            if (string.IsNullOrWhiteSpace(_ip.text)) { _app.Toast("Invalid server IP."); return; }
            SavePrefs();
            SetBusy(true);
            NetSession session = null;
            try
            {
                SetStatus("Connecting to server...");
                session = await NetSession.ConnectAsync(_ip.text.Trim(), ParsePort());
                if (this == null) { session.Dispose(); return; }
                _pending = session;
                SetStatus("Connected to server! Waiting for opponent...");

                int mySeed = new Random().Next();
                session.Send(Protocol.EncodeHello(me.Value.name, mySeed, me.Value.deck));
                Protocol.Hello opponent = await ReadHello(session, timeoutMs: 10 * 60 * 1000);
                if (this == null) { session.Dispose(); return; }

                session.Send(Protocol.DistTurnsRequest);
                string dist = await session.ReadLineAsync();
                if (this == null) { session.Dispose(); return; }
                bool amIFirst = dist == Protocol.DistTurnsFirst;
                if (!amIFirst && dist != Protocol.DistTurnsSecond)
                    throw new Exception("Unexpected answer from server: " + dist);

                int seed = mySeed ^ opponent.Seed;
                Game game = amIFirst
                    ? new Game(me.Value.name, me.Value.deck, opponent.Name, opponent.Deck, seed)
                    : new Game(opponent.Name, opponent.Deck, me.Value.name, me.Value.deck, seed);
                Launch(game, session, amIFirst ? 0 : 1);
            }
            catch (Exception e)
            {
                session?.Dispose();
                if (this != null) SetStatus(_cancelled ? "Status: Not Connected" : $"Failed to connect to server: {e.Message}");
            }
            finally
            {
                _cancelled = false;
                _pending = null;
                if (this != null) SetBusy(false);
            }
        }

        static async Task<Protocol.Hello> ReadHello(NetSession session, int timeoutMs = 20000)
        {
            string line = await session.ReadLineAsync(timeoutMs);
            if (!Protocol.TryDecodeHello(line, out Protocol.Hello hello, out string error))
                throw new Exception(error);
            return hello;
        }

        void Launch(Game game, NetSession session, int localIndex)
        {
            _pending = null; // ownership moves to the match
            _app.StartMatch(new MatchSetup { Game = game, Net = session, LocalIndex = localIndex });
        }

        bool _cancelled;

        void CancelConnect()
        {
            _cancelled = true;
            _listener?.Dispose();
            _pending?.Dispose();
            SetStatus("Status: Not Connected");
        }

        void OpenDecksFolder()
        {
            System.IO.Directory.CreateDirectory(DeckStore.Folder);
            Application.OpenURL("file:///" + DeckStore.Folder.Replace('\\', '/'));
            _app.Toast("Decks folder: " + DeckStore.Folder, 6);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
