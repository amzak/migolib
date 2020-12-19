using System.Buffers;

namespace MigoLib
{
    public interface IResultParser<T>
    {
        bool TryParse(in ReadOnlySequence<byte> sequence);
        
        T Result { get; }
    }
}