using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib.FileUpload;
using MigoLib.GCode;
using MigoLib.State;
using MigoLib.ZOffset;

namespace MigoLib
{
    public class Migo
    {
        private bool _isConnected;
        private Socket _socket;
        private IPEndPoint _endPoint;
        
        private readonly ILogger<Migo> _logger;
        private readonly ILogger<MigoReader> _readerLogger;

        public Migo(ILoggerFactory loggerFactory, MigoEndpoint endpoint)
        {
            _logger = loggerFactory.CreateLogger<Migo>();
            _readerLogger = loggerFactory.CreateLogger<MigoReader>();

            var (ip, port) = endpoint;
            _endPoint = new IPEndPoint(ip, port);
            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public async Task<ZOffsetModel> SetZOffset(double zOffset)
        {
            await EnsureConnection();
            
            byte[] buffer = new byte[100];
            
            var chunks = CommandChain
                .On(buffer)
                .SetZOffset(zOffset)
                .GetZOffset()
                .AsChunks();

            var bytesSent = await Write(chunks)
                .ConfigureAwait(false);
            
            var reader = new MigoReader(_readerLogger, _socket);

            var result = await reader.Get(Parsers.GetZOffset)
                .ConfigureAwait(false);
            
            return result;
        }

        public async Task<ZOffsetModel> GetZOffset()
        {
            await EnsureConnection();
            
            byte[] buffer = new byte[100];

            var length = await CommandChain
                .On(buffer)
                .GetZOffset()
                .Execute()
                .ConfigureAwait(false);
    
            await Write(buffer, length)
                .ConfigureAwait(false);
            
            var reader = new MigoReader(_readerLogger, _socket);

            var result = await reader.Get(Parsers.GetZOffset)
                .ConfigureAwait(false);
            
            return result;
        }
        
        public async Task<MigoStateModel> GetState()
        {
            await EnsureConnection();
            
            var reader = new MigoReader(_readerLogger, _socket);

            var result = await reader.Get(Parsers.GetState)
                .ConfigureAwait(false);

            return result;
        }

        private async Task EnsureConnection()
        {
            if (!_socket.Connected)
            {
                await _socket.ConnectAsync(_endPoint);
            }
        }
        
        public async Task<GCodeResultModel> ExecuteGCode(string[] lines)
        {
            await EnsureConnection();
            
            byte[] buffer = new byte[100];
            
            var length = await CommandChain
                .On(buffer)
                .ExecuteGCode(lines)
                .Execute()
                .ConfigureAwait(false);

            await Write(buffer, length)
                .ConfigureAwait(false);

            var reader = new MigoReader(_readerLogger, _socket);

            var result = await reader.Get(Parsers.GetGCodeResult)
                .ConfigureAwait(false);

            return result;
        }

        private Task<int> Write(byte[] buffer, int length)
        {
            ArraySegment<byte> buf = buffer;
            return _socket.SendAsync(buf.Slice(0, length), SocketFlags.None);
        }
        
        public async Task<UploadGCodeResult> UploadGCodeFile(string fileName)
        {
            await EnsureConnection();
            
            var file = new GCodeFile(fileName);

            var command = new UploadGCodeCommand(file);
            
            await Write(command.Chunks)
                .ConfigureAwait(false);

            var reader = new MigoReader(_readerLogger, _socket);
            var result = await reader.Get(Parsers.UploadGCodeResult)
                .ConfigureAwait(false);

            return result;
        }

        private async Task<int> Write(IAsyncEnumerable<CommandChunk> chunks)
        {
            int bytesSent = 0;

            await foreach (var chunk in chunks)
            {
                var segment = chunk.AsSegment();
                
                bytesSent += await _socket.SendAsync(segment, SocketFlags.None)
                    .ConfigureAwait(false);
            }

            return bytesSent;
        }
        
        private async Task<int> Write(IEnumerable<ReadOnlyMemory<byte>> chunks)
        {
            int bytesSent = 0;
            
            foreach (var chunk in chunks)
            {
                _logger.LogDebug($"processing command chunk of {chunk.Length} bytes");
                bytesSent += await _socket.SendAsync(chunk, SocketFlags.None)
                    .ConfigureAwait(false);
            }

            return bytesSent;
        }

        public async Task<FilePercentResult> GetFilePercent()
        {
            await EnsureConnection();
            
            var reader = new MigoReader(_readerLogger, _socket);

            var result = await reader.Get(Parsers.GetFilePercent)
                .ConfigureAwait(false);

            return result;
        }
    }
}
