using System.IO;
using System.Threading.Tasks;

namespace MigoLib.PrinterInfo
{
    public class GetPrinterInfoCommand : Command
    {
        public override Task Write(BinaryWriter writer)
        {
            writer.Write("getprinterinfor;");
            return Task.CompletedTask;
        }
    }
}