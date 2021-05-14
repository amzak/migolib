using System.Buffers;

namespace MigoLib
{
    public class PositionParser<T>
    {
        public readonly char Delimiter;
        public readonly ReadOnlySpanAction<char, T> ParserAction;

        public PositionParser(ReadOnlySpanAction<char,T> parserAction, char delimiter)
        {
            ParserAction = parserAction;
            Delimiter = delimiter;
        }
    }
}