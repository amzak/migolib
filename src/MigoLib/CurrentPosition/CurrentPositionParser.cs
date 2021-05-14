namespace MigoLib.CurrentPosition
{
    /// @#curposition:50.00;50.00;0.10;#@
    public class CurrentPositionParser : ResultParser<CurrentPositionResult>
    {
        protected override void Setup(PositionalSerializer<CurrentPositionResult> serializer)
        {
            serializer
                .NextDelimiter(':')
                .FixedString("curposition")
                .NextDelimiter(';')
                .Field(item => item.X)
                .Field(item => item.Y)
                .Field(item => item.Z);
        }
    }
}