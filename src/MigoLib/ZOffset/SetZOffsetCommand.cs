using System.IO;
using System.Threading.Tasks;

namespace MigoLib.ZOffset
{
    public class SetZOffsetCommand : Command
    {
        private readonly double _zOffset;

        public SetZOffsetCommand(double zOffset)
        {
            _zOffset = zOffset;
        }

        public override Task Write(BinaryWriter writer)
        {
            writer.Write("extruderminoffset:");
            writer.Write(_zOffset.ToString("F2"));
            writer.Write(';');
            
            return Task.CompletedTask;
        }
    }
}