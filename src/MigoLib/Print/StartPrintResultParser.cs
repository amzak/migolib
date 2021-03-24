namespace MigoLib.Print
{
    public class StartPrintResultParser : ResultParser<StartPrintResult>
    {
        protected override void Setup(PositionalSerializer<StartPrintResult> serializer)
        {
            serializer
                .FixedString("printstartsuccess")
                .Skip(1); // skip "fn:(.*)***.gcode"
        }
    }
}