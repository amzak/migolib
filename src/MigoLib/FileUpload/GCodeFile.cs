using System.IO;

namespace MigoLib.FileUpload
{
    public class GCodeFile
    {
        public string FileName { get; }
        public long Size { get; }

        public GCodeFile(string fileName)
        {
            FileName = fileName;
            var fileInfo = new FileInfo(fileName);
            Size = fileInfo.Length;
        }
    }
}