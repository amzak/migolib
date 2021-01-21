using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MigoLib.State;
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

        private readonly MemoryStream _receiveStream;
        public long ReceivedBytes => _receiveStream.Length;
        
        private readonly TimeSpan _receiveTimeout;

        public FakeMigo(string ip, ushort port)
            : this(new MigoEndpoint(ip, port))
        {
        }

        public FakeMigo(MigoEndpoint endpoint, TimeSpan? receiveTimeout = null)
        {
            var bytesBuff = new byte[MaxBuffer];
            _buffer = new Memory<byte>(bytesBuff);
            _receiveStream = new MemoryStream(MaxBuffer);
            
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
                    _receiveStream.SetLength(0);
                    
                    var stream = tcpClient.GetStream();
                    
                    try
                    {
                        while (!_immediateReply)
                        {
                            Console.WriteLine("Fake Migo receiving...");
                            var received = await stream.ReadAsync(_buffer, _timeoutCancellation.Token)
                                .ConfigureAwait(false);

                            if (received == 0)
                            {
                                break;
                            }

                            _receiveStream.Write(_buffer.Slice(0, received).Span);
                            
                            Console.WriteLine($"Fake Migo: received {received.ToString()} total {ReceivedBytes.ToString()} bytes");
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
                        _receiveStream.Flush();
                    }
                    
                    Console.WriteLine($"Fake Migo sent {_fixedReply} after receiving {ReceivedBytes.ToString()} bytes");
                    
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

        public void ReplyState()
        {
            var migoStateModel = new MigoStateModel();
            var reply = $"@#state;{migoStateModel.HeadX.ToString("F2")};" +
                        $"{migoStateModel.HeadX.ToString("F2")};" +
                        $"{migoStateModel.BedTemp.ToString()};" +
                        $"{migoStateModel.NozzleTemp.ToString()};0;10;1;0;0;0#@";

            FixReply(reply);
        }

        public async Task<byte[]> GetMD5(int skip = 0)
        {
            using var md5 = MD5.Create();
            _receiveStream.Position = skip;
            var hash = await md5.ComputeHashAsync(_receiveStream);
            return hash;
        }
    }
}