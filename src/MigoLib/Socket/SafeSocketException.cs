using System;

namespace MigoLib.Socket
{
    public class SafeSocketException : Exception
    {
        public SafeSocketException(Exception exception) 
            : base(exception.Message, exception)
        {
        }

        public SafeSocketException(Exception exception, string message)
            : base(message, exception)
        {
        }
    }
}