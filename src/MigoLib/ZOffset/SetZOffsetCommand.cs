using System.IO;

namespace MigoLib.ZOffset
{
    public static class SetZOffsetExtension
    {
        public static CommandChain SetZOffset(this CommandChain self, double zOffset)
        {
            var command = new SetZOffsetCommand(zOffset);
            self.Append(command);
            return self;
        }
    }
    
    public class SetZOffsetCommand : Command
    {
        private readonly double _zOffset;

        public SetZOffsetCommand(double zOffset)
        {
            _zOffset = zOffset;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write("extruderminoffset:");
            writer.Write(_zOffset.ToString("F2"));
            writer.Write(';');
        }
    }
}