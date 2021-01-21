using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MigoLib
{
    public class MigoReader
    {
        const int BufferSize = 100;

        private readonly byte[] _startMarker = {(byte) '@', (byte) '#'};
        private readonly byte[] _endMarker = {(byte) '#', (byte) '@'};

        private readonly Socket _socket;
        private readonly Pipe _pipe;
        private Memory<byte> _buffer;

        private CancellationTokenSource _cancellationSource;

        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(20);

        public MigoReader(Socket socket)
        {
            _socket = socket;
            _pipe = new Pipe();
        }

        private async Task ReadSocketAsync()
        {
            try
            {
                while (!_cancellationSource.IsCancellationRequested)
                {
                    var token = _cancellationSource.Token;
                    _buffer = _pipe.Writer.GetMemory(BufferSize);
                    var arraySegment = GetArray(_buffer);

                    var bytesRead = await _socket.ReceiveAsync(arraySegment, SocketFlags.None, token)
                        .ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    _pipe.Writer.Advance(bytesRead);

                    FlushResult result = await _pipe.Writer.FlushAsync().ConfigureAwait(false);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException _)
            {
                // nop
            }

            await _pipe.Writer.CompleteAsync()
                .ConfigureAwait(false);
        }

        private async Task<T> ReadPipeAsync<T>(IResultParser<T> resultParser)
        {
            var reader = _pipe.Reader;
            ReadResult result;
            do
            {
                result = await reader.ReadAsync().ConfigureAwait(false);

                var position = ProcessBuffer(result, resultParser);

                reader.AdvanceTo(position, position);
            } while (!result.IsCompleted && !_cancellationSource.IsCancellationRequested);

            await reader.CompleteAsync().ConfigureAwait(false);

            return resultParser.Result;
        }

        private SequencePosition ProcessBuffer<T>(in ReadResult incoming, IResultParser<T> resultParser)
        {
            ReadOnlySequence<byte> incomingBuffer = incoming.Buffer;
            var sequenceReader = new SequenceReader<byte>(incomingBuffer);

            long offset = 0;
            do
            {
                if (!sequenceReader.TryReadTo(out ReadOnlySpan<byte> _, _startMarker) ||
                    !sequenceReader.TryReadTo(out ReadOnlySequence<byte> resultBuffer, _endMarker))
                {
                    break;
                }

                if (resultParser.TryParse(resultBuffer))
                {
                    _cancellationSource.Cancel();
                }

                offset += resultBuffer.Length + _startMarker.Length + _endMarker.Length;
            } while (!_cancellationSource.IsCancellationRequested);

            return incomingBuffer.GetPosition(offset);
        }

        private void ParseBuffer(in ReadOnlySequence<byte> sequence)
        {
        }

        private static ArraySegment<byte> GetArray(Memory<byte> memory)
        {
            return GetArray((ReadOnlyMemory<byte>) memory);
        }

        private static ArraySegment<byte> GetArray(ReadOnlyMemory<byte> memory)
        {
            if (!MemoryMarshal.TryGetArray(memory, out var result))
            {
                throw new InvalidOperationException("Buffer backed by array was expected");
            }

            return result;
        }

        public async Task<T> Get<T>(IResultParser<T> parser)
        {
            _cancellationSource = new CancellationTokenSource(_timeout);

            var readSocketTask = ReadSocketAsync();
            var readPipeTask = ReadPipeAsync(parser);

            await Task.WhenAll(readSocketTask, readPipeTask)
                .ConfigureAwait(false);

            return readPipeTask.Result;
        }
    }
}