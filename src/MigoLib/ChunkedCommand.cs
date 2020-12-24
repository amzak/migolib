using System;
using System.Collections.Generic;

namespace MigoLib
{
    public class ChunkedCommand
    {
        public virtual IAsyncEnumerable<ReadOnlyMemory<byte>> Chunks { get; }
    }
}