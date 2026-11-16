using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Networking.Transport.Utilities;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// A connection through Unity Relay, so neither player has to open a port or share their IP.
    /// Rooms are found by a 4-digit code: the host publishes a Lobby whose indexed field S1 holds the code and whose
    /// data holds the Relay join code. The guest looks the code up, joins the Relay allocation and connects.
    /// Everything runs on Unity's main thread; a hidden <see cref="RelayPump"/> updates the transport each frame.
    /// </summary>
    public sealed class RelaySession : NetSession
    {
        const string ConnectionType = "dtls";
        const string KeyRelay = "relay";
        const float HeartbeatSeconds = 15f;
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        NetworkDriver _driver;
        NetworkPipeline _pipe;
        NetworkConnection _conn;
        readonly bool _isHost;
        readonly Queue<string> _outbox = new Queue<string>();
        readonly TaskCompletionSource<bool> _connected = new TaskCompletionSource<bool>();
        RelayPump _pump;
        bool _disposed;

        string _lobbyId;
        float _nextHeartbeat;

        /// <summary>The 4-digit room code (host only).</summary>
        public string RoomCode { get; private set; }

        RelaySession(RelayServerData relay, bool isHost)
        {
            _isHost = isHost;
            var settings = new NetworkSettings(Allocator.Temp);
            settings.WithRelayParameters(ref relay);
            settings.WithReliableStageParameters(windowSize: 64);
            settings.WithFragmentationStageParameters(payloadCapacity: 16 * 1024);
            _driver = NetworkDriver.Create(settings);
            _pipe = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));

            var go = new GameObject("RelayPump") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(go);
            _pump = go.AddComponent<RelayPump>();
            _pump.Session = this;
        }

        // ───────────────────────────── Rooms ─────────────────────────────

        /// <summary>Creates a room and returns once it is open; await <see cref="WaitForOpponentAsync"/> for the guest.</summary>
        public static async Task<RelaySession> CreateRoomAsync()
        {
            await SignInAsync();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var session = new RelaySession(allocation.ToRelayServerData(ConnectionType), isHost: true);
            try
            {
                if (session._driver.Bind(NetworkEndpoint.AnyIpv4) != 0 || session._driver.Listen() != 0)
                    throw new Exception("Could not open a connection to the relay.");

                var rng = new System.Random();
                for (int attempt = 0; attempt < 10 && session._lobbyId == null; attempt++)
                {
                    string code = rng.Next(0, 10000).ToString("D4");
                    if (await FindRoomAsync(code) != null) continue; // taken, roll again
                    if (session._disposed) throw new OperationCanceledException();

                    var options = new CreateLobbyOptions
                    {
                        IsPrivate = false,
                        Data = new Dictionary<string, DataObject>
                        {
                            [KeyRelay] = new DataObject(DataObject.VisibilityOptions.Public, relayCode),
                            ["code"] = new DataObject(DataObject.VisibilityOptions.Public, code, DataObject.IndexOptions.S1),
                        },
                    };
                    Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("BrawlTCG " + code, 2, options);
                    session._lobbyId = lobby.Id;
                    session.RoomCode = code;
                    session._nextHeartbeat = Time.realtimeSinceStartup + HeartbeatSeconds;
                    if (session._disposed) session.CloseRoom(); // cancelled while the lobby was being created
                }
                if (session._disposed) throw new OperationCanceledException();
                if (session._lobbyId == null) throw new Exception("Could not find a free room code. Try again.");
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        public static async Task<RelaySession> JoinRoomAsync(string code, int timeoutMs = 15000)
        {
            await SignInAsync();
            Lobby lobby = await FindRoomAsync(code);
            if (lobby == null || lobby.Data == null || !lobby.Data.TryGetValue(KeyRelay, out DataObject relay))
                throw new Exception($"No open room with code {code}.");

            JoinAllocation allocation;
            try { allocation = await RelayService.Instance.JoinAllocationAsync(relay.Value); }
            catch (RelayServiceException) { throw new Exception($"Room {code} has closed."); }

            RelayServerData data = allocation.ToRelayServerData(ConnectionType);
            var session = new RelaySession(data, isHost: false);
            try
            {
                session._conn = session._driver.Connect(data.Endpoint);
                await session.WaitForOpponentAsync(timeoutMs);
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        static async Task<Lobby> FindRoomAsync(string code)
        {
            var query = new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new List<QueryFilter> { new QueryFilter(QueryFilter.FieldOptions.S1, code, QueryFilter.OpOptions.EQ) },
            };
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(query);
            return response.Results != null && response.Results.Count > 0 ? response.Results[0] : null;
        }

        static async Task SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                // A fresh profile per game instance, so two windows on one PC sign in as two different players.
                var options = new InitializationOptions();
                options.SetProfile("p" + Guid.NewGuid().ToString("N").Substring(0, 16));
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        /// <summary>Host: waits for the guest. Guest: waits for the connection. A negative timeout waits forever.</summary>
        public async Task WaitForOpponentAsync(int timeoutMs = -1)
        {
            Task done = _connected.Task;
            if (await Task.WhenAny(done, Task.Delay(timeoutMs)) != done)
                throw new TimeoutException("Could not reach the other player.");
            await done; // rethrows
        }

        // ───────────────────────────── Transport ─────────────────────────────

        internal void Tick()
        {
            if (_disposed) return;
            _driver.ScheduleUpdate().Complete();

            if (_isHost && !_conn.IsCreated)
            {
                NetworkConnection incoming = _driver.Accept();
                if (incoming.IsCreated)
                {
                    _conn = incoming;
                    CloseRoom(); // the room is full now
                    _connected.TrySetResult(true);
                }
            }

            if (_conn.IsCreated)
            {
                NetworkEvent.Type type;
                while ((type = _driver.PopEventForConnection(_conn, out DataStreamReader reader)) != NetworkEvent.Type.Empty)
                {
                    switch (type)
                    {
                        case NetworkEvent.Type.Connect:
                            _connected.TrySetResult(true);
                            break;
                        case NetworkEvent.Type.Data:
                            var bytes = new NativeArray<byte>(reader.Length, Allocator.Temp);
                            reader.ReadBytes(bytes);
                            Inbox.Enqueue(Utf8.GetString(bytes.ToArray()));
                            bytes.Dispose();
                            break;
                        case NetworkEvent.Type.Disconnect:
                            _conn = default;
                            Closed = true;
                            _connected.TrySetException(new IOException("Could not reach the other player."));
                            return;
                    }
                }
                Flush();
            }

            if (_lobbyId != null && Time.realtimeSinceStartup >= _nextHeartbeat)
            {
                _nextHeartbeat = Time.realtimeSinceStartup + HeartbeatSeconds;
                Heartbeat(_lobbyId);
            }
        }

        /// <summary>Sends queued lines; anything the reliable window can't take yet waits for the next frame.</summary>
        void Flush()
        {
            if (!_conn.IsCreated || _driver.GetConnectionState(_conn) != NetworkConnection.State.Connected) return;
            while (_outbox.Count > 0)
            {
                byte[] payload = Utf8.GetBytes(_outbox.Peek());
                if (_driver.BeginSend(_pipe, _conn, out DataStreamWriter writer, payload.Length) < 0) return;
                var bytes = new NativeArray<byte>(payload, Allocator.Temp);
                writer.WriteBytes(bytes);
                bytes.Dispose();
                int result = _driver.EndSend(writer);
                if (result == (int)global::Unity.Networking.Transport.Error.StatusCode.NetworkSendQueueFull) return;
                if (result < 0)
                {
                    Closed = true;
                    return;
                }
                _outbox.Dequeue();
            }
        }

        public override async Task<string> ReadLineAsync(int timeoutMs = 20000)
        {
            var clock = Stopwatch.StartNew();
            while (true)
            {
                if (Inbox.TryDequeue(out string line)) return line;
                if (Closed) throw new IOException("The connection was closed.");
                if (clock.ElapsedMilliseconds > timeoutMs)
                {
                    Dispose();
                    throw new TimeoutException("The other player did not answer in time.");
                }
                await Task.Delay(15);
            }
        }

        public override bool Send(string line)
        {
            if (Closed || _disposed) return false;
            _outbox.Enqueue(line);
            Flush();
            return true;
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Closed = true;
            _connected.TrySetCanceled();
            CloseRoom();

            if (_driver.IsCreated)
            {
                try
                {
                    // Push out anything still queued (such as a quit message), then say goodbye.
                    Flush();
                    _driver.ScheduleUpdate().Complete();
                    if (_conn.IsCreated) _driver.Disconnect(_conn);
                    _driver.ScheduleUpdate().Complete();
                }
                catch { /* ignore */ }
                _driver.Dispose();
            }
            _conn = default;

            if (_pump != null)
            {
                _pump.Session = null;
                UnityEngine.Object.Destroy(_pump.gameObject);
                _pump = null;
            }
        }

        void CloseRoom()
        {
            if (_lobbyId == null) return;
            DeleteLobby(_lobbyId);
            _lobbyId = null;
        }

        static async void Heartbeat(string lobbyId)
        {
            try { await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("Room heartbeat failed: " + e.Message); }
        }

        static async void DeleteLobby(string lobbyId)
        {
            try { await LobbyService.Instance.DeleteLobbyAsync(lobbyId); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("Could not close the room: " + e.Message); }
        }
    }

    /// <summary>Updates a <see cref="RelaySession"/> every frame and cleans it up if the game closes.</summary>
    sealed class RelayPump : MonoBehaviour
    {
        [NonSerialized] public RelaySession Session;
        void Update() => Session?.Tick();
        void OnDestroy() => Session?.Dispose();
    }
}
