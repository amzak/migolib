namespace MigoLib.FileUpload
{
    public class UploadGCodeResultParser : ResultParser<UploadGCodeResult>
    {
        protected override void Setup(PositionalSerializer<UploadGCodeResult> serializer)
        {
            serializer.FixedString("fend");
        }
   }
}