using System.IO;

namespace MigoLib.ZOffset
{
    public class GetZOffsetCommand : Command
    {
        public override void Write(BinaryWriter writer)
        {
            writer.Write("extruderminoffset:");
            writer.Write(';');
        }
    }
}