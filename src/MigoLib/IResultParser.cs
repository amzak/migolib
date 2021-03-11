using System.Buffers;

namespace MigoLib
{

    public interface IResultParser
    {
        bool TryParse(in ReadOnlySequence<byte> sequence);
        
        object ResultObject { get; }
    }

    public interface IResultParser<T> : IResultParser where T: class
    {
        T Result { get; }

        object IResultParser.ResultObject => Result;
    }
}