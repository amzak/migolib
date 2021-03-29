using System;
using System.IO;
using System.Threading.Tasks;

namespace MigoLib.Print
{
    public class StartPrintCommand : Command
    {
        private readonly string _fileName;

        public StartPrintCommand(string fileName)
        {
            _fileName = fileName;
        }
        
        public override Task Write(BinaryWriter writer)
        {
            writer.Write("startprint;fn:".AsSpan());
            writer.Write(_fileName.AsSpan());
            writer.Write(';');
            
            return Task.CompletedTask;
        }
    }
}