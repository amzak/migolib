using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MigoLib
{
    public class CommandChain
    {
        private readonly byte[] _buffer;
        private readonly List<Command> _commands;

        private CommandChain(byte[] buffer)
        {
            _buffer = buffer;
            _commands = new List<Command>();
        }

        public static CommandChain Start(byte[] buffer = null)
        {
            buffer ??= new byte[100];
            return new CommandChain(buffer);
        }

        internal CommandChain Append(Command command)
        {
            _commands.Add(command);
            return this;
        }
        
        public CommandChainResult<T> GetResult<T>(IResultParser<T> parser) where T : class 
            => new(_commands, _buffer, parser);
    }

    public class CommandChainResult<T> : IDisposable where T : class
    {
        private readonly IReadOnlyCollection<Command> _commands;
        private readonly byte[] _buffer;
        private readonly IResultParser<T> _parser;
        private readonly BinaryWriter _writer;

        public CommandChainResult(IReadOnlyCollection<Command> commands, byte[] buffer, IResultParser<T> parser)
        {
            _commands = commands;
            _buffer = buffer;
            _parser = parser;
            
            var stream = new MemoryStream(buffer);
            _writer = new BinaryWriter(stream);
        }

        public Task<T> ExecuteAsync(MigoReaderWriter readerWriter) => readerWriter.Get(WriteBuffer(), _parser);

        private async Task<Memory<byte>> WriteBuffer()
        {
            foreach (var command in _commands)
            {
                await command.Write(_writer).ConfigureAwait(false);
            }
            
            _writer.Flush();
            
            var length = (int) _writer.BaseStream.Position;
            var memory = new Memory<byte>(_buffer, 0, length);
            
            return memory;
        }
        
        public Task<T> ExecuteChunksAsync(MigoReaderWriter readerWriter) => readerWriter.Get(AsChunks(), _parser);

        private async IAsyncEnumerable<ReadOnlyMemory<byte>> AsChunks()
        {
            var memory = new ReadOnlyMemory<byte>(_buffer);
            int pos = 0;
            foreach (var command in _commands)
            {
                var prevPos = _writer.BaseStream.Position;
                await command.Write(_writer).ConfigureAwait(false);
                int bytesCount = (int) (_writer.BaseStream.Position - prevPos);
                var chunk = memory.Slice(pos, bytesCount);
                pos += bytesCount;
                yield return chunk;
            }
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}