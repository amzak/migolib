using System;
using System.Net.Sockets;

namespace MigoLib.Socket
{
    public struct SafeSocketStatus
    {
        public bool IsConnected { get; set; }
        public bool IsDead { get; set; }
        public string Message { get; set; }

        public static SafeSocketStatus Initial = new ()
        {
            Message = "Not connected yet"
        };

        public static SafeSocketStatus Connected = new() {IsConnected = true, Message = "Connected"};

        public static SafeSocketStatus Connecting = new() {Message = "Connecting..."};

        public static SafeSocketStatus NotConnected(Exception exception = null) 
            => new ()
            {
                Message = exception?.Message ?? "Not connected"
            };

        public static SafeSocketStatus Dead(SocketException socketException) 
            => new()
            {
                IsDead = true,
                Message = socketException.Message
            };
    }
}