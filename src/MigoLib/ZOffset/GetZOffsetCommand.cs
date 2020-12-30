using System;
using System.IO;
using System.Threading.Tasks;

namespace MigoLib.ZOffset
{
    public class GetZOffsetCommand : Command
    {
        private readonly ReadOnlyMemory<char> _request;

        public GetZOffsetCommand()
        {
            _request = "GetZOffsetValue;".AsMemory();
        }
        
        public override Task Write(BinaryWriter writer)
        {
            writer.Write(_request.Span);
            return Task.CompletedTask;
        }
    }
}