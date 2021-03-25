using System.IO;
using System.Threading.Tasks;

namespace MigoLib.Print
{
    public class StopPrintCommand : Command
    {
        public override Task Write(BinaryWriter writer)
        {
            writer.Write("stop");
            writer.Write(';');
            
            return Task.CompletedTask;
        }
    }
}