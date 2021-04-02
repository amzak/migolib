namespace MigoLib.Print
{
    public class StartPrintResultParser : ResultParser<StartPrintResult>
    {
        protected override void Setup(PositionalSerializer<StartPrintResult> serializer)
        {
            serializer
                .Switch(
                    ("printstartsuccess", x => x.Success, true), 
                    ("printstartfailed", x => x.Success, false)
                )
                .Skip(1); // skip "fn:(.*)***.gcode"
        }
    }
}