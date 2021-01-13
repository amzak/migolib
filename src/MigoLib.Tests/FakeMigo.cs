using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MigoToolCli;

namespace MigoLib.Tests
{
    public class FakeMigo
    {
        private const int MaxBuffer = 5000;
        
        private string _fixedReply;

        private readonly TcpListener _tcpListener;
        private readonly CancellationTokenSource _tokenSource;
        private readonly Memory<byte> _buffer;
        private bool _immediateReply;
        private CancellationTokenSource _timeoutCancellation;

        public int BytesReceived { get; private set; }

        private readonly TimeSpan _receiveTimeout;

        public FakeMigo(string ip, ushort port)
            : this(new MigoEndpoint(ip, port))
        {
        }

        public FakeMigo(MigoEndpoint endpoint, TimeSpan? receiveTimeout = null)
        {
            var bytesBuff = new byte[MaxBuffer];
            _buffer = new Memory<byte>(bytesBuff);
            _receiveTimeout = receiveTimeout ?? TimeSpan.FromMilliseconds(500);
            _tokenSource = new CancellationTokenSource();
            _timeoutCancellation = new CancellationTokenSource(_receiveTimeout);
            _tcpListener = new TcpListener(endpoint.Ip, endpoint.Port);
        }

        public void Start()
        {
            _tcpListener.Start();

            Task.Run(StartListening);
        }

        private async Task StartListening()
        {
            Console.WriteLine("Fake Migo started...");
            try
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync()
                        .ConfigureAwait(false);
                    var clientIp = ((IPEndPoint)tcpClient.Client.RemoteEndPoint)?.Address.ToString();
                    Console.WriteLine($"accepted client from {clientIp}");
                    var stream = tcpClient.GetStream();
                    BytesReceived = 0;

                    try
                    {
                        while (!_immediateReply)
                        {
                            Console.WriteLine("Fake Migo receiving...");
                            var received = await stream.ReadAsync(_buffer, _timeoutCancellation.Token)
                                .ConfigureAwait(false);
                            BytesReceived += received;
                            Console.WriteLine($"Fake Migo: received {received.ToString()} total {BytesReceived.ToString()} bytes");
                        }

                        Console.WriteLine("Fake Migo: out of receive cycle");
                    }
                    catch (OperationCanceledException ex)
                    {
                        Console.WriteLine("Fake Migo receive timeout");
                    }
                    finally
                    {
                        _timeoutCancellation = new CancellationTokenSource(_receiveTimeout);
                        _immediateReply = false;
                    }
                    
                    Console.WriteLine($"Fake Migo sent {_fixedReply} after receiving {BytesReceived.ToString()} bytes");
                    
                    var bytes = Encoding.UTF8.GetBytes(_fixedReply);
                    await stream.WriteAsync(bytes).ConfigureAwait(false);
                    tcpClient.Close();
                    Console.WriteLine($"client from {clientIp} disconnected");
                }
            }
            finally
            {
                _tcpListener.Stop();
            }
            Console.WriteLine("Fake Migo stopped.");
        }

        public void Stop()
        {
            _tokenSource.Cancel();
        }

        public FakeMigo FixReply(string data, bool immediateReply = false)
        {
            _fixedReply = data;
            _immediateReply = immediateReply;
            
            return this;
        }

        public FakeMigo ReplyZOffset(double zOffset) 
            => FixReply($"@#ZOffsetValue:{zOffset.ToString("F2")}#@");
    }
}