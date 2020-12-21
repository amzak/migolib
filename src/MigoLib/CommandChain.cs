using System;
using System.Collections.Generic;
using System.IO;

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

        public CommandChainResult<T> AsResult<T>(IResultParser<T> parser) 
            => new(parser);
        
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

        public void Execute()
        {
            foreach (var command in _commands)
            {
                command.Write(_writer);
            }
            
            _writer.Flush();
        }
    }
}