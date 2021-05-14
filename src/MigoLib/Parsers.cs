using MigoLib.CurrentPosition;
using MigoLib.FileUpload;
using MigoLib.GCode;
using MigoLib.Print;
using MigoLib.PrinterInfo;
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

        public static IResultParser<StartPrintResult> StartPrintResult => new StartPrintResultParser();

        public static IResultParser<StopPrintResult> StopPrintResult => new StopPrintResultParser();
        
        public static IResultParser<PrinterInfoResult> GetPrinterInfo => new PrinterInfoResultParser();
        
        public static IResultParser<CurrentPositionResult> GetCurrentPosition => new CurrentPositionParser();
    }
}