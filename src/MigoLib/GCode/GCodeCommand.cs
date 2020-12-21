using System.IO;

namespace MigoLib.GCode
{
    public class GCodeCommand : Command
    {
        private readonly string[] _lines;

        public GCodeCommand(string[] lines)
        {
            _lines = lines;
        }
        
        public override void Write(BinaryWriter writer)
        {
            writer.Write("gcode:");
            foreach (var line in _lines)
            {
                writer.Write(line);
                writer.Write(0x0A);
            }
            writer.Write(';');
        }
    }
}