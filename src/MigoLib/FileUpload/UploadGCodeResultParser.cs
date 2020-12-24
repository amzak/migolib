using System;
using System.Buffers;
using System.Text;

namespace MigoLib.FileUpload
{
    public class UploadGCodeResultParser : IResultParser<UploadGCodeResult>
    {
        private readonly PositionalSerializer<UploadGCodeResult> _positionalSerializer;

        public UploadGCodeResultParser()
        {
            _positionalSerializer = new PositionalSerializer<UploadGCodeResult>(';')
                .FixedString("fend");
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

        public UploadGCodeResult Result { get; private set; }
    }
}