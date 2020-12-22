using System.IO;
using System.Threading.Tasks;

namespace MigoLib.ZOffset
{
    public class GetZOffsetCommand : Command
    {
        public override Task Write(BinaryWriter writer)
        {
            writer.Write("extruderminoffset:");
            writer.Write(';');
            
            return Task.CompletedTask;
        }
    }
}