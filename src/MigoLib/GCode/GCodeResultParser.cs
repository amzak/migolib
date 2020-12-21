using System;
using System.Buffers;
using System.Text;

namespace MigoLib.GCode
{
    public class GCodeResultParser : IResultParser<GCodeResultModel>
    {
        private readonly PositionalSerializer<GCodeResultModel> _positionalSerializer;

        public GCodeResultParser()
        {
            _positionalSerializer = new PositionalSerializer<GCodeResultModel>(';')
                .FixedString("gcodedone");
        }
        
        public bool TryParse(in ReadOnlySequence<byte> sequence)
        {
            int sequenceLength = (int) sequence.Length;
            Span<char> charBuf = stackalloc char[sequenceLength];
            Encoding.Default.GetChars(sequence.FirstSpan, charBuf);
            Result = _positionalSerializer.Parse(charBuf);
            Result.Success = !_positionalSerializer.IsError;
            
            return Result.Success;
        }

        public GCodeResultModel Result { get; private set; }
    }
}