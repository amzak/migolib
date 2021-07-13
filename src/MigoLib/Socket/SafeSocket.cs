using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib.Socket;

namespace MigoLib
{
    public class SafeSocket : IDisposable
    {
        private const int MaxConnectAttempts = 3;
        private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(5);

        private readonly ILogger<SafeSocket> _logger;
        private readonly System.Net.Sockets.Socket _socket;

        private readonly SemaphoreSlim _socketSemaphore;

        public readonly IPEndPoint EndPoint;
        private readonly CancellationTokenSource _cts;

        private int _connectAttempts;
        
        public SafeSocket(ILogger<SafeSocket> logger, IPEndPoint endPoint)
        {
            _logger = logger;
            EndPoint = endPoint;

            _cts = new CancellationTokenSource();

            _socketSemaphore = new SemaphoreSlim(1, 1);
            _socket = new System.Net.Sockets.Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _logger.LogDebug($"new socket created");
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer)
        {
            try
            {
                var received = await _socket.ReceiveAsync(buffer, SocketFlags.None)
                    .ConfigureAwait(false);

                _connectAttempts = 0;

                return received;
            }
            catch (SocketException ex)
            {
                return await TryHandleReceive(buffer, ex)
                    .ConfigureAwait(false);
            }
        }

        private async Task<int> TryHandleReceive(Memory<byte> buffer, SocketException ex)
        {
            await TryHandleOrReconnect(ex).ConfigureAwait(false);

            return await ReceiveAsync(buffer).ConfigureAwait(false);
        }

        private async Task TryHandleOrReconnect(SocketException ex)
        {
            try
            {
                await _socketSemaphore.WaitAsync().ConfigureAwait(false);
                
                if (!CanHandle(ex))
                {
                    throw new SafeSocketException(ex, "Socket exception can't be handled");
                }

                if (_connectAttempts > MaxConnectAttempts)
                {
                    throw new SafeSocketException(ex, "Connect attempts number exceeded");
                }

                if (_socket.Connected || IsSocketConnected())
                {
                    _logger.LogDebug("disconnecting socket...");
                    await Task.Factory.FromAsync(_socket.BeginDisconnect, _socket.EndDisconnect, true, null)
                        .ConfigureAwait(false);
                    _logger.LogDebug("socket disconnected");
                }

                await _socket.ConnectAsync(EndPoint).ConfigureAwait(false);
                _logger.LogDebug("socket connected");

                _connectAttempts++;
            }
            finally
            {
                _socketSemaphore.Release();
            }
        }

        private bool CanHandle(SocketException exception) => true;

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer)
        {
            try
            {
                var sent = await _socket.SendAsync(buffer, SocketFlags.None)
                    .ConfigureAwait(false);

                _connectAttempts = 0;

                return sent;
            }
            catch (SocketException exception)
            {
                return await TryHandleSend(buffer, exception);
            }
        }

        private async Task<int> TryHandleSend(ReadOnlyMemory<byte> buffer, SocketException exception)
        {
            await TryHandleOrReconnect(exception).ConfigureAwait(false);
            return await SendAsync(buffer).ConfigureAwait(false);
        }

        private bool IsSocketConnected()
        {
            var isConnected = !(_socket.Poll(1, SelectMode.SelectRead)
                                && _socket.Available == 0);

            _logger.LogDebug($"socket poll returned isConnected = {isConnected}");
            return isConnected;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _socket?.Close();
            _socket?.Dispose();
            _socketSemaphore?.Dispose();
            _cts.Dispose();
        }
    }
}