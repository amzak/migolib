using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MigoLib
{
    public class CommandChain : IDisposable
    {
        private readonly BinaryWriter _writer;

        private readonly List<Command> _commands;

        private CommandChain(byte[] buffer)
        {
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

        public async Task<CommandChain> Execute()
        {
            foreach (var command in _commands)
            {
                await command.Write(_writer).ConfigureAwait(false);
            }
            
            _writer.Flush();

            return this;
        }

        public async Task<CommandChain> ExecuteInChunks(ChunkedCommand command)
        {
            await foreach (var chunk in command.Chunks)
            {
                Console.WriteLine($"writing chunk of {chunk.Length.ToString()} bytes...");
                _writer.Write(chunk.Span);
                _writer.Flush();
            }
            
            return this;
        }
    }
}