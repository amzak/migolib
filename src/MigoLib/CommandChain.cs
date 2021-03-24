using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MigoLib
{
    public class CommandChain : IDisposable
    {
        private readonly byte[] _buffer;
        private readonly BinaryWriter _writer;

        private readonly List<Command> _commands;

        private CommandChain(byte[] buffer)
        {
            _buffer = buffer;
            var stream = new MemoryStream(buffer);
            _writer = new BinaryWriter(stream);
            _commands = new List<Command>();
        }

        public static CommandChain On(byte[] buffer) 
            => new CommandChain(buffer);

        internal CommandChain Append(Command command)
        {
            _commands.Add(command);
            return this;
        }
        
        public void Dispose()
        {
            _writer?.Dispose();
        }

        public async Task<int> ExecuteAsync()
        {
            foreach (var command in _commands)
            {
                await command.Write(_writer).ConfigureAwait(false);
            }
            
            _writer.Flush();
            
            return (int) _writer.BaseStream.Position;
        }

        public async IAsyncEnumerable<ReadOnlyMemory<byte>> AsChunks()
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
    }
}