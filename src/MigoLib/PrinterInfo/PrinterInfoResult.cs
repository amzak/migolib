using System;

namespace MigoLib.PrinterInfo
{
    public class PrinterInfoResult : ParseResult
    {
        private const string Marker = "modelprinting:";
        public int State { get; set; }

        public string StatedDescription { get; set; }
        
        public bool IsPrinting => 
            StatedDescription.Contains(Marker, StringComparison.OrdinalIgnoreCase) &&
            StatedDescription.Length > Marker.Length;
    }
}