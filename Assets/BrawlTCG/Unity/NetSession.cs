using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// A line based connection to the other player. During the handshake lines are read with <see cref="ReadLineAsync"/>;
    /// afterwards <see cref="StartReceiving"/> is called and lines are polled on the main thread with <see cref="TryReceive"/>,
    /// so game state is only ever touched from Unity's main thread.
    /// Two transports exist: direct TCP (LAN / IP, <see cref="ConnectAsync"/> and <see cref="Listener"/>) and
    /// Unity Relay (<see cref="RelaySession"/>, used by room codes).
    /// </summary>
    public abstract class NetSession : IDisposable
    {
        protected readonly ConcurrentQueue<string> Inbox = new ConcurrentQueue<string>();
        protected volatile bool Closed;

        public bool IsClosed => Closed;
        public bool TryReceive(out string line) => Inbox.TryDequeue(out line);
        public bool HasPending => !Inbox.IsEmpty;

        public abstract Task<string> ReadLineAsync(int timeoutMs = 20000);
        public virtual void StartReceiving() { }
        public abstract bool Send(string line);
        public abstract void Dispose();

        // ───────────────────────────── Direct TCP ─────────────────────────────

        public static async Task<NetSession> ConnectAsync(string host, int port, int timeoutMs = 10000)
        {
            var client = new TcpClient();
            Task connect = client.ConnectAsync(host, port);
            if (await Task.WhenAny(connect, Task.Delay(timeoutMs)) != connect)
            {
                client.Close();
                throw new TimeoutException($"Could not reach {host}:{port}.");
            }
            await connect; // rethrows connection errors
            return new TcpSession(client);
        }

        public sealed class Listener : IDisposable
        {
            readonly TcpListener _listener;
            bool _stopped;

            public Listener(int port)
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
            }

            public async Task<NetSession> AcceptAsync()
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Dispose();
                return new TcpSession(client);
            }

            public void Dispose()
            {
                if (_stopped) return;
                _stopped = true;
                try { _listener.Stop(); } catch { /* ignore */ }
            }
        }

        /// <summary>Reads on a background thread once <see cref="StartReceiving"/> is called.</summary>
        sealed class TcpSession : NetSession
        {
            readonly TcpClient _client;
            readonly StreamReader _reader;
            readonly StreamWriter _writer;
            readonly object _writeLock = new object();
            Thread _thread;

            public TcpSession(TcpClient client)
            {
                _client = client;
                _client.NoDelay = true;
                NetworkStream stream = client.GetStream();
                var utf8 = new UTF8Encoding(false);
                _reader = new StreamReader(stream, utf8);
                _writer = new StreamWriter(stream, utf8) { AutoFlush = true, NewLine = "\n" };
            }

            public override async Task<string> ReadLineAsync(int timeoutMs = 20000)
            {
                Task<string> read = _reader.ReadLineAsync();
                if (await Task.WhenAny(read, Task.Delay(timeoutMs)) != read)
                {
                    Dispose();
                    throw new TimeoutException("The other player did not answer in time.");
                }
                string line = await read;
                if (line == null) throw new IOException("The connection was closed.");
                return line;
            }

            public override void StartReceiving()
            {
                if (_thread != null) return;
                _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "BrawlTCG-Net" };
                _thread.Start();
            }

            void ReceiveLoop()
            {
                try
                {
                    while (!Closed)
                    {
                        string line = _reader.ReadLine();
                        if (line == null) break;
                        Inbox.Enqueue(line);
                    }
                }
                catch
                {
                    // connection lost
                }
                Closed = true;
            }

            public override bool Send(string line)
            {
                if (Closed) return false;
                try
                {
                    lock (_writeLock)
                        _writer.WriteLine(line);
                    return true;
                }
                catch
                {
                    Closed = true;
                    return false;
                }
            }

            public override void Dispose()
            {
                Closed = true;
                try { _client.Close(); } catch { /* ignore */ }
            }
        }
    }
}
