using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib.Socket;
using Nito.AsyncEx;

namespace MigoLib
{
    public class MigoReaderWriter : IDisposable
    {
        const int BufferSize = 100;

        private readonly byte[] _startMarker = {(byte) '@', (byte) '#'};
        private readonly byte[] _endMarker = {(byte) '#', (byte) '@'};

        private readonly IPEndPoint _endPoint;
        private readonly ILogger<MigoReaderWriter> _logger;
        private readonly SafeSocket _socket;
        private readonly Pipe _pipe;
        private Memory<byte> _buffer;

        private readonly ConcurrentBag<StreamedReply> _streams;
        private readonly object _requestsRepliesLock;
        private readonly List<RequestReply> _requestsReplies;

        private readonly CancellationTokenSource _lifetimeCts;

        private readonly AsyncManualResetEvent _startListening;

        public MigoReaderWriter(IPEndPoint endPoint, ILoggerFactory loggerFactory, ErrorHandlingPolicy errorPolicy)
        {
            _endPoint = endPoint;
            _logger = loggerFactory.CreateLogger<MigoReaderWriter>();

            _pipe = new Pipe();
            _streams = new ConcurrentBag<StreamedReply>();
            _requestsReplies = new List<RequestReply>();
            _requestsRepliesLock = new object();

            _lifetimeCts = new CancellationTokenSource();
            
            var socketLogger = loggerFactory.CreateLogger<SafeSocket>();

            _socket = new SafeSocket(socketLogger, endPoint, errorPolicy);

            _startListening = new AsyncManualResetEvent();

            Task.Run(Start);
        }

        private async Task Start()
        {
            _logger.LogDebug($"waiting for signal to start listening on... {_endPoint}");

            await _startListening.WaitAsync().ConfigureAwait(false);

            _logger.LogDebug($"started... {_endPoint}");

            var readPipeTask = ReadPipeAsync();
            
            var startSocketReaderTask = StartSocketReader();

            await Task.WhenAll(readPipeTask, startSocketReaderTask).ConfigureAwait(false);

            _logger.LogDebug("completed");
        }

        private async Task StartSocketReader()
        {
            _logger.LogDebug($"started socket reader... {_socket.EndPoint}");

            try
            {
                while (true)
                {
                    _buffer = _pipe.Writer.GetMemory(BufferSize);

                    _logger.LogDebug($"reading from socket...{_socket.EndPoint}");
                
                    int bytesRead = await _socket.ReceiveAsync(_buffer)
                        .ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        _logger.LogDebug("socket returned zero bytes");
                        break;
                    }

                    _logger.LogDebug("socket reader received {bytesRead} bytes", bytesRead.ToString());

                    _pipe.Writer.Advance(bytesRead);

                    await _pipe.Writer.FlushAsync()
                        .ConfigureAwait(false);
                }
            }
            catch (SafeSocketException socketException)
            {
                _logger.LogError(socketException, "No connection");
            }

            _logger.LogWarning($"socket reader completed {_socket.EndPoint}");
        }

        private async Task ReadPipeAsync()
        {
            _logger.LogDebug("pipe reader started...");

            try
            {
                var reader = _pipe.Reader;
                ReadResult result;

                do
                {
                    _logger.LogDebug($"reading from pipe...");

                    result = await reader.ReadAsync(_lifetimeCts.Token)
                        .ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    _logger.LogDebug($"reader result {result.IsCompleted} {result.IsCanceled} {buffer.Length}");

                    var consumed = ProcessBuffer(ref buffer);

                    reader.AdvanceTo(buffer.GetPosition(consumed), buffer.End);

                    _logger.LogDebug($"consumed {consumed} bytes");
                } while (!result.IsCompleted);

                await reader.CompleteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "pipe reader aborted");
            }
            finally
            {
                _logger.LogDebug("pipe reader completed");
            }
        }

        private long ProcessBuffer(ref ReadOnlySequence<byte> incomingBuffer)
        {
            var sequenceReader = new SequenceReader<byte>(incomingBuffer);

            long consumed = 0;
            do
            {
                if (!sequenceReader.TryReadTo(out ReadOnlySpan<byte> _, _startMarker) ||
                    !sequenceReader.TryReadTo(out ReadOnlySequence<byte> resultBuffer, _endMarker))
                {
                    break;
                }

                lock (_requestsRepliesLock)
                {
                    var requestsCount = _requestsReplies.Count;
                    var streamsCount = _streams.Count;
                    if (requestsCount > 0 || streamsCount > 0)
                    {
                        _logger.LogDebug($"requests {requestsCount} streams {streamsCount}");
                    }
                }
                
                ParseStream(resultBuffer);
                ParseForRequests(resultBuffer);

                consumed += resultBuffer.Length + _startMarker.Length + _endMarker.Length;
                _logger.LogTrace($"consumed {consumed} bytes from buffer of {incomingBuffer.Length}");
            } while (true);

            _logger.LogDebug($"processed buffer of size {incomingBuffer.Length} offset {consumed}");
            return consumed;
        }

        private void ParseStream(ReadOnlySequence<byte> resultBuffer)
        {
            if (_streams.Count == 0)
            {
                return;
            }
            
            var streamsToContinue = new List<StreamedReply>(_streams.Count);

            while (_streams.TryTake(out var stream))
            {
                _logger.LogDebug($"parsing started, buffer {stream.BufferSize}");
                stream.Parse(resultBuffer);
                _logger.LogDebug("parsing completed");

                if (!stream.IsCompleted)
                {
                    streamsToContinue.Add(stream);
                }
            }

            foreach (var stream in streamsToContinue)
            {
                _streams.Add(stream);
            }
        }

        private void ParseForRequests(ReadOnlySequence<byte> resultBuffer)
        {
            List<RequestReply> requestsReplies;

            lock (_requestsRepliesLock)
            {
                requestsReplies = _requestsReplies.ToList();
            }

            foreach (var requestsReply in requestsReplies)
            {
                if (!requestsReply.Parser.TryParse(resultBuffer))
                {
                    continue;
                }

                requestsReply.Complete();

                lock (_requestsRepliesLock)
                {
                    _requestsReplies.Remove(requestsReply);
                }
            }
        }

        private static ArraySegment<byte> GetArray(Memory<byte> memory)
        {
            var readonlyMemory = (ReadOnlyMemory<byte>) memory;
            if (!MemoryMarshal.TryGetArray(readonlyMemory, out var result))
            {
                throw new InvalidOperationException("Buffer backed by array was expected");
            }

            return result;
        }

        public async Task<T> Get<T>(Task<Memory<byte>> bufferWriter, IResultParser<T> parser)
            where T : class
        {
            var requestReply = RequestReply(parser);

            var buffer = await bufferWriter.ConfigureAwait(false);
            await Write(buffer).ConfigureAwait(false);
            
            _startListening.Set();

            var result = await requestReply.ConfigureAwait(false);

            return (T) result;
        }

        public async Task<T> Get<T>(IAsyncEnumerable<ReadOnlyMemory<byte>> chunks, IResultParser<T> parser)
            where T : class
        {
            var requestReply = RequestReply(parser);

            await Write(chunks).ConfigureAwait(false);

            _startListening.Set();

            var result = await requestReply.ConfigureAwait(false);

            return (T) result;
        }

        public async Task<T> Get<T>(IAsyncEnumerable<CommandChunk> chunks, IResultParser<T> parser)
            where T : class
        {
            var requestReply = RequestReply(parser);

            await Write(chunks).ConfigureAwait(false);

            _startListening.Set();

            var result = await requestReply.ConfigureAwait(false);

            return (T) result;
        }
        
        public async Task<T> Get<T>(IResultParser<T> parser)
            where T : class
        {
            var requestReply = RequestReply(parser);

            _startListening.Set();

            var result = await requestReply.ConfigureAwait(false);

            return (T) result;
        }
        
        private Task<object> RequestReply(IResultParser parser)
        {
            var completionSource = new TaskCompletionSource<object>();
            var requestReply = new RequestReply(parser, completionSource);

            lock (_requestsRepliesLock)
            {
                _requestsReplies.Add(requestReply);
            }

            return completionSource.Task;
        }

        public async IAsyncEnumerable<T> GetStream<T>(IResultParser<T> parser, CancellationToken token)
            where T : class
        {
            _logger.LogTrace($"starting stream of {typeof(T).Name}");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCts.Token,
                token);

            var streamedReply = new StreamedReply(parser, cts.Token);
            _streams.Add(streamedReply);
            
            _startListening.Set();

            await foreach (var o in streamedReply)
            {
                _logger.LogTrace($"stream of {typeof(T).Name} new item");
                yield return (T) o;
            }

            _logger.LogDebug($"stream of {typeof(T).Name} completed");
        }

        public async IAsyncEnumerable<SafeSocketStatus> GetConnectionStatusStream([EnumeratorCancellation] CancellationToken token)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCts.Token,
                token);

            var connectionStatusStream = new EventsStream<SafeSocketStatus>(cts.Token);

            void Handler(object _, SafeSocketStatus status)
            {
                connectionStatusStream.SetNextEvent(status);
            }

            _socket.OnConnectionStatusChange += Handler;

            try
            {
                _startListening.Set();

                await foreach (var status in connectionStatusStream)
                {
                    yield return status;
                }
            }
            finally
            {
                _socket.OnConnectionStatusChange -= Handler;
            }
        }

        public async Task<int> Write(IEnumerable<ReadOnlyMemory<byte>> chunks)
        {
            int bytesSent = 0;

            foreach (var chunk in chunks)
            {
                _logger.LogDebug($"processing command chunk of {chunk.Length} bytes");
                bytesSent += await _socket.SendAsync(chunk)
                    .ConfigureAwait(false);
            }

            return bytesSent;
        }

        private async ValueTask<int> Write(Memory<byte> buffer)
        {
            var sent = await _socket.SendAsync(buffer).ConfigureAwait(false);

            return sent;
        }

        private async Task<int> Write(IAsyncEnumerable<CommandChunk> chunks)
        {
            int bytesSent = 0;

            await foreach (var chunk in chunks)
            {
                var segment = chunk.AsSegment();

                bytesSent += await _socket.SendAsync(segment)
                    .ConfigureAwait(false);
            }

            return bytesSent;
        }

        private async Task<int> Write(IAsyncEnumerable<ReadOnlyMemory<byte>> chunks)
        {
            int bytesSent = 0;

            await foreach (var chunk in chunks)
            {
                bytesSent += await _socket.SendAsync(chunk)
                    .ConfigureAwait(false);
            }

            return bytesSent;
        }

        public void Dispose()
        {
            _lifetimeCts.Cancel();

            _pipe.Writer.Complete();
            _pipe.Reader.Complete();

            _socket?.Dispose();
            _logger.LogDebug("socket closed");

            _lifetimeCts.Dispose();
        }
    }
}