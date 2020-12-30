using System;
using System.IO;
using System.Threading.Tasks;

namespace MigoLib.ZOffset
{
    public class SetZOffsetCommand : Command
    {
        private readonly double _zOffset;
        private readonly ReadOnlyMemory<char> _request;

        public SetZOffsetCommand(double zOffset)
        {
            _zOffset = zOffset;
            _request = "extruderminoffset:".AsMemory();
        }

        public override Task Write(BinaryWriter writer)
        {
            Span<char> chars = stackalloc char[100];
            
            writer.Write(_request.Span);
            _zOffset.TryFormat(chars, out int charsWritten, "F2");
            writer.Write(chars.Slice(0, charsWritten));
            writer.Write(';');
            
            return Task.CompletedTask;
        }
    }
}