using System;

namespace MigoLib.FileUpload
{
    public class UploadGCodeResult
    {
        public bool Success { get; set; }
        public TimeSpan CompletedIn { get; set; }
    }
}