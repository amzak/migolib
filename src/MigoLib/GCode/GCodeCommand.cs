using System;
using System.IO;
using System.Threading.Tasks;

namespace MigoLib.GCode
{
    public class GCodeCommand : Command
    {
        private readonly string[] _lines;
        private readonly ReadOnlyMemory<char> _request;

        public GCodeCommand(string[] lines)
        {
            _lines = lines;
            _request = "gcode:".AsMemory();
        }
        
        public override Task Write(BinaryWriter writer)
        {
            writer.Write(_request.Span);
            foreach (var line in _lines)
            {
                writer.Write(line.AsSpan());
                writer.Write(0x0A);
            }
            writer.Write(';');
            
            return Task.CompletedTask;
        }
    }
}