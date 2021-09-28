using System;

namespace MigoLib
{
    public class ErrorHandlingPolicy
    {
        public static readonly ErrorHandlingPolicy Default = new()
        {
            ReconnectAttempts = 3,
            ReconnectInterval = TimeSpan.FromSeconds(5),
            SocketTimeout = TimeSpan.FromSeconds(10),
            ReconnectOnDisconnect = false
        };
        
        public int ReconnectAttempts { get; set; }
        public TimeSpan ReconnectInterval { get; set; }
        public TimeSpan SocketTimeout { get; set; }
        
        public bool ReconnectOnDisconnect { get; set; }
    }
}