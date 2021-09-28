namespace MigoLib.GCode
{
    public class GCodeResultParser : ResultParser<GCodeResultModel>
    {
        protected override void Setup(PositionalSerializer<GCodeResultModel> serializer)
        {
            serializer.FixedString("gcodedone");
        }
    }
}