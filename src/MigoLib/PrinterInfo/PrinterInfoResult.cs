using System;

namespace MigoLib.PrinterInfo
{
    public class PrinterInfoResult : ParseResult
    {
        public int State { get; set; }

        public string StatedDescription { get; set; }
        
        public bool IsPrinting => StatedDescription.Contains("modelprinting", StringComparison.OrdinalIgnoreCase);
    }
}