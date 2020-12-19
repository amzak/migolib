using System;
using System.Buffers;
using System.Text;

namespace MigoLib.State
{
    /*
     @#state;0.00;0.00;0;25;0;10;1;0;0;0#@
     */
    public class GetStateParser : IResultParser<MigoStateModel>
    {
        private readonly PositionalSerializer<MigoStateModel> _positionalSerializer;

        public GetStateParser()
        {
            _positionalSerializer = PositionalSerializer.CreateFor<MigoStateModel>(';')
                .FixedString("state")
                .Field(x => x.HeadX)
                .Field(x => x.HeadY)
                .Field(x => x.BedTemp)
                .Field(x => x.NozzleTemp);
        }

        public bool TryParse(in ReadOnlySequence<byte> sequence)
        {
            int sequenceLength = (int) sequence.Length;
            Span<char> charBuf = stackalloc char[sequenceLength];
            Encoding.Default.GetChars(sequence.FirstSpan, charBuf);
            Result = _positionalSerializer.Parse(charBuf);
                
            return !_positionalSerializer.IsError;
        }
        
        public MigoStateModel Result { get; private set; }
    }
    
    
    
    
    
}