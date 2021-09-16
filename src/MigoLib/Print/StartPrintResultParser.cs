namespace MigoLib.Print
{
    public class StartPrintResultParser : ResultParser<StartPrintResult>
    {
        protected override void Setup(PositionalSerializer<StartPrintResult> serializer)
        {
            serializer
                .Switch(
                    ("printstartsuccess", x => x.PrintStarted, true), 
                    ("printstartfailed", x => x.PrintStarted, false)
                )
                .Skip(1); // skip "fn:(.*)***.gcode"
        }
    }
}