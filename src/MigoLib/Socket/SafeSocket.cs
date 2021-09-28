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
        public readonly IPEndPoint EndPoint;
        private readonly ErrorHandlingPolicy _errorPolicy;

        private readonly ILogger<SafeSocket> _logger;
        private readonly System.Net.Sockets.Socket _socket;

        private readonly SemaphoreSlim _socketSemaphore;

        private int _connectAttempts;

        private readonly ConcurrentDictionary<int, DateTime> _timeoutsMap;
        private readonly Task _timeoutsMonitor;

        private readonly CancellationTokenSource _lifetimeCts;

        public SafeSocket(ILogger<SafeSocket> logger, IPEndPoint endPoint, ErrorHandlingPolicy errorPolicy)
        {
            _logger = logger;
            EndPoint = endPoint;
            _errorPolicy = errorPolicy;

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
                    if (now - timeout.Value <= _errorPolicy.SocketTimeout)
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
                    if (_errorPolicy.ReconnectOnDisconnect)
                    {
                        _logger.LogTrace("received 0 bytes, will try to reconnect");
                        return await TryHandleReceive(buffer)
                            .ConfigureAwait(false);
                    }

                    throw new SafeSocketException("Disconnected by other side.");
                }
                
                _connectAttempts = 0;

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
            await TryHandleOrReconnect(ex, SocketOp.Receive).ConfigureAwait(false);
            
            return await ReceiveAsync(buffer).ConfigureAwait(false);
        }

        private async Task TryHandleOrReconnect(Exception ex, SocketOp op)
        {
            var handlingZeroBytes = ex == null;
            
            _logger.LogTrace(
                handlingZeroBytes
                    ? $"handling zero bytes on {op.ToString()}"
                    : $"handling exception on {op.ToString()} {ex.GetType()} {(ex as SocketException)?.ErrorCode} {ex.Message}");

            try
            {
                RaiseOnConnectionStatusChange(SafeSocketStatus.NotConnected(ex));
                
                await _socketSemaphore.WaitAsync().ConfigureAwait(false);

                if ((_socket.Connected || IsSocketConnected()) && !handlingZeroBytes)
                {
                    _logger.LogTrace($"reconnect aborted, socket is connected now");
                    return;
                }

                _connectAttempts++;
                _logger.LogTrace($"attempt {_connectAttempts} of {_errorPolicy.ReconnectAttempts} {op}");
                
                if (_connectAttempts > _errorPolicy.ReconnectAttempts)
                {
                    throw new SafeSocketException(ex, "Connect attempts number exceeded");
                }

                if (ex != null)
                {
                    await TryHandleException(ex, op).ConfigureAwait(false);
                }
                else
                {
                    await ReconnectWithDelay().ConfigureAwait(false);
                }

                RaiseOnConnectionStatusChange(SafeSocketStatus.Connecting);

                _logger.LogTrace("connecting socket...");

                await _socket.ConnectAsync(EndPoint).ConfigureAwait(false);

                _logger.LogTrace("socket connected");
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

        private async Task ReconnectWithDelay()
        {
            _logger.LogDebug("disconnecting socket...");
            await Task.Factory.FromAsync(_socket.BeginDisconnect, _socket.EndDisconnect, true, null)
                .ConfigureAwait(false);
            _logger.LogDebug("socket disconnected");

            if (_connectAttempts > 1) // first time retry immediately
            {
                await Task.Delay(_errorPolicy.ReconnectInterval)
                    .ConfigureAwait(false);
            }
        }

        private async Task TryHandleException(Exception ex, SocketOp op)
        {
            if (!CanHandle(ex))
            {
                throw new SafeSocketException(ex, "Socket exception can't be handled");
            }

            if (IsSocketForcefullyClosed(ex))
            {
                _logger.LogDebug("socket was forcefully disconnected");
            }
            else if (_socket.Connected || IsSocketConnected())
            {
                await ReconnectWithDelay().ConfigureAwait(false);
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
                result = socketException.ErrorCode == 107 ||    // Transport endpoint is not connected
                         socketException.ErrorCode == 125 ||    // operation cancelled
                         socketException.ErrorCode == 32;       // broken pipe (OK on send if no connection established) 
            }

            if (exception is OperationCanceledException)
            {
                result = true;
            }

            return result;
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer)
        {
            try
            {
                _logger.LogDebug($"sending {buffer.Length} bytes...");

                var sent = await _socket.SendAsync(buffer, SocketFlags.None)
                    .ConfigureAwait(false);
                
                _logger.LogDebug($"sent {sent} bytes");

                _connectAttempts = 0;

                return sent;
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                return await TryHandleSend(buffer, ex);
            }
        }

        private async Task<int> TryHandleSend(ReadOnlyMemory<byte> buffer, Exception exception)
        {
            await TryHandleOrReconnect(exception, SocketOp.Send).ConfigureAwait(false);
            return await SendAsync(buffer).ConfigureAwait(false);
        }

        private bool IsSocketConnected()
        {
            var socketPollRead = _socket.Poll(1, SelectMode.SelectRead);

            var socketPollWrite = _socket.Poll(1, SelectMode.SelectWrite);
            
            var socketPollError = _socket.Poll(1, SelectMode.SelectError);

            var bytesAvailable = _socket.Available;

            _logger.LogDebug($"socket poll socketPollWrite = {socketPollWrite}, socketPollRead = {socketPollRead}, socketPollError = {socketPollError}, bytesAvailable = {bytesAvailable}");

            return !(_socket.Poll(1, SelectMode.SelectRead)
                && _socket.Available == 0);
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