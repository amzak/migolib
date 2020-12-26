using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MigoLib.Tests
{
    public class FakeMigo
    {
        private string _fixedReply;
        private long _bytesExpected;

        private readonly TcpListener _tcpListener;
        private readonly CancellationTokenSource _tokenSource;
        private Memory<byte> _buffer;

        public FakeMigo(string ip, ushort port)
        {
            _tokenSource = new CancellationTokenSource();
            _tcpListener = new TcpListener(IPAddress.Parse(ip), port);
        }

        public void Start()
        {
            _tcpListener.Start();

            Task.Run(StartListening);
        }

        private async Task StartListening()
        {
            try
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                    var clientIp = ((IPEndPoint)tcpClient.Client.RemoteEndPoint)?.Address.ToString();
                    Console.WriteLine($"accepted client from {clientIp}");
                    var stream = tcpClient.GetStream();
                    var bytesRead = 0;
                    while (bytesRead < _bytesExpected)
                    {
                        bytesRead += await stream.ReadAsync(_buffer, _tokenSource.Token).ConfigureAwait(false);
                    }
                    
                    var bytes = Encoding.UTF8.GetBytes(_fixedReply);
                    await stream.WriteAsync(bytes);
                    tcpClient.Close();
                    Console.WriteLine($"client from {clientIp} disconnected");
                }
            }
            finally
            {
                _tcpListener.Stop();
            }
        }

        public void Stop()
        {
            _tokenSource.Cancel();
        }

        public FakeMigo FixReply(string data)
        {
            _fixedReply = data;

            return this;
        }

        public FakeMigo ExpectBytes(long expected)
        {
            _bytesExpected = expected;
            var bytesBuf = new byte[_bytesExpected];
            _buffer = new Memory<byte>(bytesBuf);

            return this;
        }
    }
}