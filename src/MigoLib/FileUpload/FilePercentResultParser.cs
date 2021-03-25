namespace MigoLib.FileUpload
{
    public class FilePercentResultParser : ResultParser<FilePercentResult>
    {
        protected override void Setup(PositionalSerializer<FilePercentResult> serializer)
        {
            serializer
                .Delimiter(':')
                .FixedString("filepercent")
                .Field(x => x.Percent);
        }
    }
}