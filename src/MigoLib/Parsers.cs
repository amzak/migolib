using MigoLib.FileUpload;
using MigoLib.GCode;
using MigoLib.State;
using MigoLib.ZOffset;

namespace MigoLib
{
    public static class Parsers
    {
        public static IResultParser<ZOffsetModel> GetZOffset => new GetZOffsetParser();
        
        public static IResultParser<MigoStateModel> GetState => new GetStateParser();
        
        public static IResultParser<GCodeResultModel> GetGCodeResult => new GCodeResultParser();
        
        public static IResultParser<UploadGCodeResult> UploadGCodeResult => new UploadGCodeResultParser();
        
        public static IResultParser<FilePercentResult> GetFilePercent => new FilePercentResultParser();
    }
}