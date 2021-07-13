using System;
using System.Net.Sockets;

namespace MigoLib.Socket
{
    public class SafeSocketException : Exception
    {
        private readonly SocketException _socketException;

        public SafeSocketException(SocketException socketException)
        {
            _socketException = socketException;
        }

        public SafeSocketException(SocketException socketException, string message)
        {
            throw new NotImplementedException();
        }
    }
}