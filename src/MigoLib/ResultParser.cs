using System;
using System.Buffers;
using System.Text;

namespace MigoLib
{
    public abstract class ResultParser<T> : IResultParser<T> where T: ParseResult, new()
    {
        private readonly PositionalSerializer<T> _serializer;

        protected ResultParser()
        {
            _serializer = new PositionalSerializer<T>(';');
            Setup(_serializer);
        }

        protected abstract void Setup(PositionalSerializer<T> serializer);

        public bool TryParse(in ReadOnlySequence<byte> sequence)
        {
            int sequenceLength = (int) sequence.Length;
            Span<char> charBuf = stackalloc char[sequenceLength];
            Encoding.Default.GetChars(sequence.FirstSpan, charBuf);
            Result = _serializer.Parse(charBuf);
            Result.Success = !_serializer.IsError;
            
            return Result.Success;
        }

        public T Result { get; protected set; }
    }
}