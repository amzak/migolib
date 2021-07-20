using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MigoLib.Socket
{
    public class SafeSocket : IDisposable
    {
        private const int MaxConnectAttempts = 3;

        public readonly IPEndPoint EndPoint;

        private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _socketTimeout = TimeSpan.FromSeconds(10);

        private readonly ILogger<SafeSocket> _logger;
        private readonly System.Net.Sockets.Socket _socket;

        private readonly SemaphoreSlim _socketSemaphore;

        private int _connectAttempts;

        private readonly ConcurrentDictionary<int, DateTime> _timeoutsMap;
        private readonly Task _timeoutsMonitor;

        private readonly CancellationTokenSource _lifetimeCts;

        public SafeSocket(ILogger<SafeSocket> logger, IPEndPoint endPoint)
        {
            _logger = logger;
            EndPoint = endPoint;

            _lifetimeCts = new CancellationTokenSource();
            _timeoutsMap = new ConcurrentDictionary<int, DateTime>();

            _socketSemaphore = new SemaphoreSlim(1, 1);
            _socket = new System.Net.Sockets.Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _timeoutsMonitor = Task.Run(TimeoutsMonitor);

            _logger.LogDebug($"new socket created");
            
            RaiseOnConnectionStatusChange(SafeSocketStatus.Initial);
        }

        private async Task TimeoutsMonitor()
        {
            while (!_lifetimeCts.IsCancellationRequested)
            {
                var now = DateTime.Now;
                foreach (var timeout in _timeoutsMap)
                {
                    if (now - timeout.Value <= _socketTimeout)
                    {
                        continue;
                    }

                    _socket.Disconnect(true);
                    _timeoutsMap.TryRemove(timeout);
                    _logger.LogWarning("socket forcefully closed due to operation timeout");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), _lifetimeCts.Token)
                    .ConfigureAwait(false);
            }
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer)
        {
            var hash = buffer.GetHashCode();

            try
            {
                _logger.LogTrace("started receiving...");

                int received;
                try
                {
                    _timeoutsMap[hash] = DateTime.Now;
                    received = await _socket.ReceiveAsync(buffer, SocketFlags.None)
                        .ConfigureAwait(false);
                }
                finally
                {
                    _timeoutsMap.TryRemove(hash, out _);
                }
                
                if (received == 0)
                {
                    _logger.LogTrace("received 0 bytes, will try to reconnect");
                    return await TryHandleReceive(buffer)
                        .ConfigureAwait(false);
                }
                
                _connectAttempts = 1;

                RaiseOnConnectionStatusChange(SafeSocketStatus.Connected);

                return received;
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                return await TryHandleReceive(buffer, ex)
                    .ConfigureAwait(false);
            }
        }

        private async Task<int> TryHandleReceive(Memory<byte> buffer, Exception ex = null)
        {
            await TryHandleOrReconnect(ex).ConfigureAwait(false);
            
            return await ReceiveAsync(buffer).ConfigureAwait(false);
        }

        private async Task TryHandleOrReconnect(Exception ex)
        {
            try
            {
                RaiseOnConnectionStatusChange(SafeSocketStatus.NotConnected(ex));
                
                await _socketSemaphore.WaitAsync().ConfigureAwait(false);

                _connectAttempts++;
                _logger.LogTrace($"attempt {_connectAttempts} of {MaxConnectAttempts}");
                
                if (ex != null)
                {
                    await TryHandleException(ex).ConfigureAwait(false);
                }

                RaiseOnConnectionStatusChange(SafeSocketStatus.Connecting);

                _logger.LogDebug("connecting socket...");
                await _socket.ConnectAsync(EndPoint).ConfigureAwait(false);
                _logger.LogDebug("socket connected");
            }
            catch (SocketException socketException)
            {
                if (socketException.ErrorCode == 113 || // No route to host
                    socketException.ErrorCode == 111)   // Connection refused
                {
                    RaiseOnConnectionStatusChange(SafeSocketStatus.Dead(socketException));
                    throw new SafeSocketException(socketException);
                }
            }
            finally
            {
                _socketSemaphore.Release();
            }
        }

        private async Task TryHandleException(Exception ex)
        {
            _logger.LogTrace(
                $"handling exception {ex.GetType()} {(ex as SocketException)?.ErrorCode} {ex.Message}");

            if (!CanHandle(ex))
            {
                throw new SafeSocketException(ex, "Socket exception can't be handled");
            }

            if (_connectAttempts > MaxConnectAttempts)
            {
                throw new SafeSocketException(ex, "Connect attempts number exceeded");
            }

            if (IsSocketForcefullyClosed(ex))
            {
                _logger.LogDebug("socket was forcefully disconnected");
            }
            else if (_socket.Connected || IsSocketConnected())
            {
                _logger.LogDebug("disconnecting socket...");
                await Task.Factory.FromAsync(_socket.BeginDisconnect, _socket.EndDisconnect, true, null)
                    .ConfigureAwait(false);
                _logger.LogDebug("socket disconnected");

                await Task.Delay(_reconnectInterval)
                    .ConfigureAwait(false);
            }
        }

        private bool IsSocketForcefullyClosed(Exception exception)
        {
            if (exception is SocketException socketException)
            {
                return socketException.ErrorCode == 125;
            }

            return false;
        }

        private bool CanHandle(Exception exception)
        {
            bool result = false;
            if (exception is SocketException socketException)
            {
                result = socketException.ErrorCode == 107 ||
                         socketException.ErrorCode == 125; // operation cancelled
            }

            if (exception is OperationCanceledException)
            {
                result = true;
            }

            _logger.LogTrace($"can handle exception {result}");
            return result;
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer)
        {
            try
            {
                var sent = await _socket.SendAsync(buffer, SocketFlags.None)
                    .ConfigureAwait(false);

                _connectAttempts = 1;

                return sent;
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                return await TryHandleSend(buffer, ex);
            }
        }

        private async Task<int> TryHandleSend(ReadOnlyMemory<byte> buffer, Exception exception)
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
            _lifetimeCts.Cancel();
            _socket?.Close();
            _socket?.Dispose();
            _socketSemaphore?.Dispose();
            _lifetimeCts.Dispose();
        }

        public event EventHandler<SafeSocketStatus> OnConnectionStatusChange;
            
        public void RaiseOnConnectionStatusChange(SafeSocketStatus status)
        {
            OnConnectionStatusChange?.Invoke(this, status);
        }
    }
}