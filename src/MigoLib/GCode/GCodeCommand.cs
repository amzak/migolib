using System.IO;
using System.Threading.Tasks;

namespace MigoLib.GCode
{
    public class GCodeCommand : Command
    {
        private readonly string[] _lines;

        public GCodeCommand(string[] lines)
        {
            _lines = lines;
        }
        
        public override Task Write(BinaryWriter writer)
        {
            writer.Write("gcode:");
            foreach (var line in _lines)
            {
                writer.Write(line);
                writer.Write(0x0A);
            }
            writer.Write(';');
            
            return Task.CompletedTask;
        }
    }
}