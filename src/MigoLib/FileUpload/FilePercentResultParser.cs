using System;
using System.Buffers;
using System.Text;

namespace MigoLib.FileUpload
{
    public class FilePercentResultParser : IResultParser<FilePercentResult>
    {
        private readonly PositionalSerializer<FilePercentResult> _positionalSerializer;

        public FilePercentResultParser()
        {
            _positionalSerializer = new PositionalSerializer<FilePercentResult>(':')
                .FixedString("filepercent");
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

        public FilePercentResult Result { get; private set; }
    }
}