using System.Collections.Generic;

namespace MigoLib
{
    public class ChunkedCommand
    {
        public virtual IAsyncEnumerable<CommandChunk> Chunks { get; }
    }
}