using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib.CurrentPosition;
using MigoLib.State;

namespace MigoLib.Fake
{
    public class FakeMigo
    {
        private const int MaxBuffer = 5000;

        private string _fixedReply;

        private readonly TcpListener _tcpListener;
        private readonly CancellationTokenSource _tokenSource;
        private readonly Memory<byte> _buffer;

        private readonly MemoryStream _receiveStream;
        public long ReceivedBytes => _receiveStream.Length;

        private readonly ILogger<FakeMigo> _log;

        private FakeMigoMode _mode;
        private readonly IReadOnlyCollection<string> _streamReplies;
        private long _bytesExpected;
        private int _replyCount;
        private CancellationToken _streamCancellationToken;
        
        public FakeMigo(string ip, ushort port, ILogger<FakeMigo> logger)
            : this(new MigoEndpoint(ip, port), logger)
        {
        }

        public FakeMigo(MigoEndpoint endpoint, ILogger<FakeMigo> logger)
        {
            var bytesBuff = new byte[MaxBuffer];
            _buffer = new Memory<byte>(bytesBuff);
            _receiveStream = new MemoryStream(MaxBuffer);

            _tokenSource = new CancellationTokenSource();
            _tcpListener = new TcpListener(endpoint.Ip, endpoint.Port);

            _log = logger;

            _log.LogDebug($"created fake migo on {endpoint}");
            
            _streamReplies = PopulateStreamReplies()
                .ToList();
        }

        private IEnumerable<string> PopulateStreamReplies()
        {
            yield return FakeReplies.State;
            yield return FakeReplies.Status;
            yield return FakeReplies.FilePercent;
        }

        public async Task Start(int counter = 0)
        {
            if (counter > 3)
            {
                throw new Exception("Can't start fake migo");
            }

            try
            {
                _tcpListener.Start();
            }
            catch (SocketException ex) when(ex.Message.Equals("Address already in use"))
            {
                await Task.Delay(100).ConfigureAwait(false);
                await Start(counter + 1).ConfigureAwait(false);
                return;
            }

            await StartListening().ConfigureAwait(false);
        }

        private async Task StartListening()
        {
            _log.LogInformation("started...");

            try
            {
                var cancellationToken = _tokenSource.Token;
                while (!_tokenSource.IsCancellationRequested)
                {
                    _log.LogInformation("accepting new clients...");
                    using TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                    await HandleClient(tcpClient).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "stopped after exception");
            }
            finally
            {
                _tcpListener.Stop();
            }

            _log.LogInformation("stopped.");
        }
        
        private async Task HandleClient(TcpClient tcpClient)
        {
            _log.LogInformation($"handling client {tcpClient.Client.LocalEndPoint} in {_mode.ToString()} mode");

            await using NetworkStream stream = tcpClient.GetStream();

            switch (_mode)
            {
                case FakeMigoMode.Reply | FakeMigoMode.Request:
                    await HandleRequestReply(stream)
                        .ConfigureAwait(false);
                    break;
                case FakeMigoMode.Reply:
                    await HandleImmediateReply(stream)
                        .ConfigureAwait(false);
                    break;
                case FakeMigoMode.Stream:
                    await HandleReplyStream(stream)
                        .ConfigureAwait(false);
                    break;
                case FakeMigoMode.RealStream:
                    await HandleReplyRealStream(stream)
                        .ConfigureAwait(false);
                    break;
            }

            _log.LogInformation($"handling of client {tcpClient.Client.LocalEndPoint} in {_mode.ToString()} completed.");
        }

        private async Task HandleReplyRealStream(NetworkStream stream)
        {
            try
            {
                while (!_streamCancellationToken.IsCancellationRequested || !_tokenSource.IsCancellationRequested)
                {
                    await WriteReply(stream, FakeReplies.RandomState)
                        .ConfigureAwait(false);
                }
            }
            catch (IOException ex)
            {
                _log.LogError(ex, "error");
            }
        }

        private async Task HandleReplyStream(NetworkStream stream)
        {
            foreach (var streamReply in _streamReplies)
            {
                for (int i = 0; i < _replyCount; i++)
                {
                    await WriteReply(stream, streamReply)
                        .ConfigureAwait(false);
                }
            }
            
            _log.LogDebug("stream reply completed");
        }

        private async Task HandleRequestReply(NetworkStream stream)
        {
            long receivedTotal = 0;
            _receiveStream.SetLength(0);

            if (_bytesExpected == default)
            {
                _bytesExpected = 1; // at least receive smth
            }

            _log.LogDebug($"expecting {_bytesExpected.ToString()} bytes");

            while (receivedTotal < _bytesExpected)
            {
                var received = await stream.ReadAsync(_buffer, _tokenSource.Token)
                    .ConfigureAwait(false);

                _log.LogDebug($"received {received.ToString()} bytes");

                if (received == 0)
                {
                    break;
                }

                receivedTotal += received;

                await _receiveStream.WriteAsync(_buffer.Slice(0, received), _tokenSource.Token)
                    .ConfigureAwait(false);

                _log.LogDebug($"received {received.ToString()} total {receivedTotal.ToString()} bytes");
            }
            
            await _receiveStream.FlushAsync(_tokenSource.Token)
                .ConfigureAwait(false);
            
            await WriteReply(stream, _fixedReply)
                .ConfigureAwait(false);

            _bytesExpected = 0;
        }

        private async Task WriteReply(NetworkStream stream, string reply)
        {
            var endPoint = stream.Socket.LocalEndPoint;
            var bytes = Encoding.UTF8.GetBytes(reply);

            _log.LogInformation($"sending {reply} to {endPoint}...");
            try
            {
                await stream.WriteAsync(bytes, _tokenSource.Token).ConfigureAwait(false);
                await stream.FlushAsync(_tokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            _log.LogInformation($"sent");
        }

        private Task HandleImmediateReply(NetworkStream stream) => WriteReply(stream, _fixedReply);

        public void Stop()
        {
            _tokenSource.Cancel();
        }

        public FakeMigo FixReply(string data)
        {
            _fixedReply = data;

            return this;
        }

        public FakeMigo ReplyZOffset(double zOffset)
            => FixReply($"@#ZOffsetValue:{zOffset.ToString("F2")}#@");

        public FakeMigo ReplyGCodeDone()
            => FixReply("@#gcodedone;#@");

        public FakeMigo ReplyUploadCompleted()
            => FixReply($"@#fend;#@");

        public FakeMigo ReplyMode(FakeMigoMode mode)
        {
            _mode = mode;
            return this;
        }

        public FakeMigo ReplyRealStream(CancellationToken token)
        {
            _mode = FakeMigoMode.RealStream;
            _streamCancellationToken = token;
            return this;
        }

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

        public FakeMigo ReplyFilePercent(int percent)
            => FixReply($"@#filepercent:{percent.ToString()}#@");

        public FakeMigo ExpectBytes(long size)
        {
            _bytesExpected = size;
            return this;
        }

        public FakeMigo StreamReplyCount(int count)
        {
            _replyCount = count;
            return this;
        }

        public FakeMigo ReplyPrintStarted(string fileName)
            => FixReply($"@#printstartsuccess;fn:{fileName}#@");

        public FakeMigo ReplyPrintFailed(string fileName)
            => FixReply($"@#printstartfailed;fn:{fileName}#@");

        public FakeMigo ReplyPrintStopped() => FixReply("@#stopped;#@");

        public FakeMigo ReplyPrinterInfo() 
            => FixReply("@#getprinterinfor;id:100196;state:11;modelprinting:3DBenchy.gcode;printername:100196;color:1;type:0;version:124;lock:;#@");

        public FakeMigo ReplyCurrentPosition(Position pos) 
            => FixReply($"@#curposition:{pos.X.ToString("#.##")};{pos.Y.ToString("#.##")};{pos.Z.ToString("#.##")};#@");
    }
}