using System;
using System.IO;
using System.Threading.Tasks;

namespace MigoLib.Print
{
    public class StopPrintCommand : Command
    {
        public override Task Write(BinaryWriter writer)
        {
            writer.Write("stop".AsSpan());
            writer.Write(';');
            
            return Task.CompletedTask;
        }
    }
}