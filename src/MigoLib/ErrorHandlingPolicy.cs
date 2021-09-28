using System;

namespace MigoLib
{
    public class ErrorHandlingPolicy
    {
        public int ReconnectAttempts { get; set; }
        public TimeSpan ReconnectInterval { get; set; }
        public TimeSpan SocketTimeout { get; set; }
    }
}