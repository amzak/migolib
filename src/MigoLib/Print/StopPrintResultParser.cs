namespace MigoLib.Print
{
    public class StopPrintResultParser : ResultParser<StopPrintResult>
    {
        protected override void Setup(PositionalSerializer<StopPrintResult> serializer)
        {
            serializer.FixedString("stopped");
        }
    }
}