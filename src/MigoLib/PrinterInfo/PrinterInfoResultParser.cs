namespace MigoLib.PrinterInfo
{
    public class PrinterInfoResultParser : ResultParser<PrinterInfoResult>
    {
        protected override void Setup(PositionalSerializer<PrinterInfoResult> serializer)
        {
            // @#getprinterinfor;id:100196;state:11;modelprinting:3DBenchy.gcode;printername:100196;color:1;type:0;version:124;lock:;#@
            serializer
                .FixedString("getprinterinfor")
                .Skip(1)
                .Field(item => item.State)
                .Field(item => item.StatedDescription);
        }
    }
}