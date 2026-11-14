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
    /// A line based TCP connection. During the handshake lines are read with <see cref="ReadLineAsync"/>;
    /// afterwards <see cref="StartReceiving"/> moves reading to a background thread and lines are polled on the
    /// main thread with <see cref="TryReceive"/>, so game state is only ever touched from Unity's main thread.
    /// </summary>
    public sealed class NetSession : IDisposable
    {
        readonly TcpClient _client;
        readonly StreamReader _reader;
        readonly StreamWriter _writer;
        readonly object _writeLock = new object();
        readonly ConcurrentQueue<string> _inbox = new ConcurrentQueue<string>();
        Thread _thread;
        volatile bool _closed;

        public bool IsClosed => _closed;
        public string RemoteEndPoint { get; }

        NetSession(TcpClient client)
        {
            _client = client;
            _client.NoDelay = true;
            NetworkStream stream = client.GetStream();
            var utf8 = new UTF8Encoding(false);
            _reader = new StreamReader(stream, utf8);
            _writer = new StreamWriter(stream, utf8) { AutoFlush = true, NewLine = "\n" };
            RemoteEndPoint = client.Client?.RemoteEndPoint?.ToString() ?? "?";
        }

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
            return new NetSession(client);
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
                return new NetSession(client);
            }

            public void Dispose()
            {
                if (_stopped) return;
                _stopped = true;
                try { _listener.Stop(); } catch { /* ignore */ }
            }
        }

        public async Task<string> ReadLineAsync(int timeoutMs = 20000)
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

        public void StartReceiving()
        {
            if (_thread != null) return;
            _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "BrawlTCG-Net" };
            _thread.Start();
        }

        void ReceiveLoop()
        {
            try
            {
                while (!_closed)
                {
                    string line = _reader.ReadLine();
                    if (line == null) break;
                    _inbox.Enqueue(line);
                }
            }
            catch
            {
                // connection lost
            }
            _closed = true;
        }

        public bool TryReceive(out string line) => _inbox.TryDequeue(out line);

        public bool HasPending => !_inbox.IsEmpty;

        public bool Send(string line)
        {
            if (_closed) return false;
            try
            {
                lock (_writeLock)
                    _writer.WriteLine(line);
                return true;
            }
            catch
            {
                _closed = true;
                return false;
            }
        }

        public void Dispose()
        {
            _closed = true;
            try { _client.Close(); } catch { /* ignore */ }
        }
    }
}
