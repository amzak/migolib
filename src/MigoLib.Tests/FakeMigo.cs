using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib.State;

namespace MigoLib.Tests
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

            _streamReplies = PopulateStreamReplies()
                .ToList();
        }

        private IEnumerable<string> PopulateStreamReplies()
        {
            yield return FakeReplies.State;
            yield return FakeReplies.Status;
        }

        public void Start()
        {
            _tcpListener.Start();

            Task.Run(StartListening);
        }

        private async Task StartListening()
        {
            _log.LogInformation("started...");

            try
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    _log.LogInformation("accepting new clients...");
                    using (var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false))
                    {
                        var clientIp = ((IPEndPoint) tcpClient.Client.RemoteEndPoint)?.Address.ToString();
                        _log.LogInformation($"accepted client from {clientIp}");

                        await HandleClient(tcpClient)
                            .ConfigureAwait(false);
                    
                        _log.LogInformation($"client from {clientIp} disconnected");
                    }
                }
            }
            finally
            {
                _tcpListener.Stop();
            }

            _log.LogInformation("stopped.");
        }

        private async Task HandleClient(TcpClient tcpClient)
        {
            var stream = tcpClient.GetStream();

            _log.LogInformation($"handling client {tcpClient.Client.LocalEndPoint} in {_mode.ToString()} mode");

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
        }

        private async Task HandleReplyRealStream(NetworkStream stream)
        {
            while (!_streamCancellationToken.IsCancellationRequested || !_tokenSource.IsCancellationRequested)
            {
                await WriteReply(stream, FakeReplies.RandomState)
                    .ConfigureAwait(false);

                await Task.Delay(1000, _streamCancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task HandleReplyStream(NetworkStream stream)
        {
            var replyCount = _replyCount == 0 ? 1 : _replyCount;

            foreach (var streamReply in _streamReplies)
            {
                for (int i = 0; i < replyCount; i++)
                {
                    await WriteReply(stream, streamReply)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task HandleRequestReply(NetworkStream stream)
        {
            long receivedTotal = 0;
            _receiveStream.SetLength(0);

            if (_bytesExpected == default)
            {
                _bytesExpected = 1; // at least receive smth
            }

            while (receivedTotal < _bytesExpected)
            {
                var received = await stream.ReadAsync(_buffer /*, _timeoutCancellation.Token*/)
                    .ConfigureAwait(false);

                if (received == 0)
                {
                    break;
                }

                receivedTotal += received;

                _receiveStream.Write(_buffer.Slice(0, received).Span);
                _receiveStream.Flush();

                _log.LogDebug($"received {received.ToString()} total {receivedTotal.ToString()} bytes");
            }

            await WriteReply(stream, _fixedReply)
                .ConfigureAwait(false);

            _bytesExpected = 0;
        }

        private async Task WriteReply(NetworkStream stream, string reply)
        {
            var bytes = Encoding.UTF8.GetBytes(reply);
            await stream.WriteAsync(bytes).ConfigureAwait(false);
            _log.LogInformation($"sent {reply}");
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

        public void ReplyFilePercent(int percent)
            => FixReply($"@#filepercent:{percent.ToString()}#@");

        public FakeMigo ExpectBytes(long size)
        {
            _bytesExpected = size;
            return this;
        }

        public void ReplyCount(int count)
        {
            _replyCount = count;
        }

        public void ReplyPrintStarted(string fileName)
            => FixReply($"@#printstartsuccess;fn:{fileName}#@");
    }
}