namespace MigoLib.ZOffset
{
    public class GetZOffsetParser : ResultParser<ZOffsetModel>
    {
        protected override void Setup(PositionalSerializer<ZOffsetModel> serializer)
        {
            serializer
                .NextDelimiter(':')
                .FixedString("ZOffsetValue")
                .Field(x => x.ZOffset);
        }
    }
}