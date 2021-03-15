using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MigoLib
{
    public class MigoReaderWriter : IDisposable
    {
        const int BufferSize = 100;

        private readonly byte[] _startMarker = {(byte) '@', (byte) '#'};
        private readonly byte[] _endMarker = {(byte) '#', (byte) '@'};

        private readonly IPEndPoint _endPoint;
        private readonly ILogger<MigoReaderWriter> _logger;
        private Socket _socket;
        private readonly Pipe _pipe;
        private Memory<byte> _buffer;
        
        private readonly ConcurrentBag<StreamedReply> _streams;
        private readonly object _requestsRepliesLock;
        private readonly List<RequestReply> _requestsReplies;

        private Task _socketReadingTask;

        public MigoReaderWriter(IPEndPoint endPoint, ILogger<MigoReaderWriter> logger)
        {
            _endPoint = endPoint;
            _logger = logger;

            _pipe = new Pipe();
            _streams = new ConcurrentBag<StreamedReply>();
            _requestsReplies = new List<RequestReply>();
            _requestsRepliesLock = new object(); 

            Task.Run(Start);
        }

        private async Task Start()
        {
            _logger.LogDebug("started...");
            
            await ReadPipeAsync()
                .ConfigureAwait(false);

            _logger.LogDebug("completed");
        }

        private async Task EnsureConnection()
        {
            if (_socket != default && _socket.Connected)
            {
                var isConnected = !(_socket.Poll(1, SelectMode.SelectRead)
                                    && _socket.Available == 0);

                if (!isConnected)
                {
                    await Connect().ConfigureAwait(false);
                    _logger.LogDebug("socket connection established");
                }
            }
            else
            {
                await Connect().ConfigureAwait(false);
                _logger.LogDebug("socket connection established");
            }

            _logger.LogDebug("socket connection ok");
        }
        
        private Task Connect()
        {
            if (_socket != default)
            {
                _socket.Disconnect(true);
            }
            else
            {
                _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            return _socket.ConnectAsync(_endPoint);
        }

        private async Task ReadSocketAsync()
        {
            _logger.LogDebug("started socket reader...");

            FlushResult result = default;
            try
            {
                while (true)
                {
                    _buffer = _pipe.Writer.GetMemory(BufferSize);
                    var arraySegment = GetArray(_buffer);

                    var bytesRead = await _socket.ReceiveAsync(arraySegment, SocketFlags.None)
                        .ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        _logger.LogDebug("socket returned zero bytes");
                        break;
                    }

                    _pipe.Writer.Advance(bytesRead);
                    
                    _logger.LogDebug("socket reader received {bytesRead} bytes", bytesRead.ToString());

                    await _pipe.Writer.FlushAsync()
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException _)
            {
                // nop
            }
            
            _logger.LogDebug("socket reader completed");
        }

        private async Task ReadPipeAsync()
        {
            _logger.LogDebug("pipe reader started...");
            var reader = _pipe.Reader;
            ReadResult result;

            long consumedTotal = 0;
            
            do
            {
                _logger.LogDebug("reading from pipe...");
                result = await reader.ReadAsync()
                    .ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;

                _logger.LogDebug($"reader result {result.IsCompleted} {result.IsCanceled} {buffer.Length}");

                var consumed = ProcessBuffer(ref buffer);
                consumedTotal += consumed;
                
                reader.AdvanceTo(buffer.GetPosition(consumed), buffer.End);
                //reader.AdvanceTo(buffer.End, buffer.End);

                _logger.LogDebug($"consumed {consumed} consumedTotal {consumedTotal}");
            } 
            while (!result.IsCompleted);

            await reader.CompleteAsync().ConfigureAwait(false);
            
            _logger.LogDebug("pipe reader completed");
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
                
                ParseStream(resultBuffer);
                ParseForRequests(resultBuffer);

                consumed += resultBuffer.Length + _startMarker.Length + _endMarker.Length;
            } while (true);

            _logger.LogDebug($"processed buffer of size {incomingBuffer.Length} offset {consumed}");
            return consumed;
        }

        private void ParseStream(ReadOnlySequence<byte> resultBuffer)
        {
            _logger.LogDebug($"looking for stream response (streams: {_streams.Count})");

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

            _logger.LogDebug($"looking for request response (requests: {requestsReplies.Count})");
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

        public async Task<T> Get<T>(IResultParser<T> parser)
            where T: class
        {
            var requestReply = RequestReply(parser);

            await EnsureConnection().ConfigureAwait(false);
            EnsureSocketReading();
            
            var result = await requestReply.ConfigureAwait(false);

            return (T) result;
        }

        private void EnsureSocketReading()
        {
            if (_socketReadingTask != null && (!_socketReadingTask.IsCompleted && !_socketReadingTask.IsCanceled))
            {
                _logger.LogDebug("socket reading in progress");
                return;
            }
            
            _socketReadingTask = ReadSocketAsync();
            Task.Run(() => _socketReadingTask);
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
            where T: class
        {
            var streamedReply = new StreamedReply(parser, token);
            _streams.Add(streamedReply);

            await EnsureConnection().ConfigureAwait(false);
            EnsureSocketReading();

            await foreach (var o in streamedReply)
            {
                yield return (T) o;
            }
        }

        public async Task<int> Write(IEnumerable<ReadOnlyMemory<byte>> chunks)
        {
            await EnsureConnection()
                .ConfigureAwait(false);

            int bytesSent = 0;
            
            foreach (var chunk in chunks)
            {
                _logger.LogDebug($"processing command chunk of {chunk.Length} bytes");
                bytesSent += await _socket.SendAsync(chunk, SocketFlags.None)
                    .ConfigureAwait(false);
            }

            return bytesSent;
        }

        public async Task<int> Write(byte[] buffer, int length)
        {
            await EnsureConnection()
                .ConfigureAwait(false);
            
            ArraySegment<byte> buf = buffer;
            
            return await _socket
                .SendAsync(buf.Slice(0, length), SocketFlags.None)
                .ConfigureAwait(false);
        }

        public async Task<int> Write(IAsyncEnumerable<CommandChunk> chunks)
        {
            await EnsureConnection()
                .ConfigureAwait(false);

            int bytesSent = 0;

            await foreach (var chunk in chunks)
            {
                var segment = chunk.AsSegment();
                
                bytesSent += await _socket.SendAsync(segment, SocketFlags.None)
                    .ConfigureAwait(false);
            }

            return bytesSent;
        }
        
        public void Dispose()
        {
            _pipe.Writer.Complete();
            _pipe.Reader.Complete();
        }
    }
}