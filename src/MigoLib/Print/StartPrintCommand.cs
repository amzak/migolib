using System.IO;
using System.Threading.Tasks;

namespace MigoLib.Print
{
    public class StartPrintCommand : Command
    {
        private string _fileName;

        public StartPrintCommand(string fileName)
        {
            _fileName = fileName;
        }
        
        public override Task Write(BinaryWriter writer)
        {
            writer.Write("startprint;fn:");
            writer.Write(_fileName);
            writer.Write(';');
            
            return Task.CompletedTask;
        }
    }
}