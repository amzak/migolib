using System;
using System.Buffers;
using System.Text;

namespace MigoLib.ZOffset
{
    public class GetZOffsetParser : IResultParser<ZOffsetModel>
    {
        private readonly PositionalSerializer<ZOffsetModel> _positionalSerializer;

        public GetZOffsetParser()
        {
            _positionalSerializer = new PositionalSerializer<ZOffsetModel>(':')
                .FixedString("ZOffsetValue")
                .Field(x => x.ZOffset);
        }
        
        public bool TryParse(in ReadOnlySequence<byte> sequence)
        {
            int sequenceLength = (int) sequence.Length;
            Span<char> charBuf = stackalloc char[sequenceLength];
            Encoding.Default.GetChars(sequence.FirstSpan, charBuf);

            string str = charBuf.ToString();
            Console.WriteLine($"processing: {str}");
            
            Result = _positionalSerializer.Parse(charBuf);
                
            return !_positionalSerializer.IsError;
        }

        public ZOffsetModel Result { get; private set; }
    }
}