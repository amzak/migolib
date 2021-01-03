using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MigoLib.FileUpload;
using MigoLib.State;
using MigoLib.ZOffset;

namespace MigoLib
{
    public class Migo
    {
        private readonly IPAddress _ip;
        private readonly ushort _port;

        private bool _isConnected;
        private Socket _socket;
        private IPEndPoint _endPoint;

        public Migo(string ip, ushort port)
        {
            _ip = IPAddress.Parse(ip);
            _port = port;
            _endPoint = new IPEndPoint(_ip, port);
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
            
            var reader = new MigoReader(_socket);

            var result = await reader.Get(Parsers.GetZOffset)
                .ConfigureAwait(false);
            
            return result;
        }

        public async Task<MigoStateModel> GetState()
        {
            await EnsureConnection();
            
            var reader = new MigoReader(_socket);

            var result = await reader.Get(Parsers.GetState)
                .ConfigureAwait(false);

            return result;
        }

        private async Task EnsureConnection()
        {
            if (!_socket.Connected)
            {
                await _socket.ConnectAsync(_endPoint);
                Console.WriteLine(_socket.SendBufferSize);
            }
        }
        
        public async Task<bool> ExecuteGCode(string[] lines)
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

            var reader = new MigoReader(_socket);

            var result = await reader.Get(Parsers.GetGCodeResult)
                .ConfigureAwait(false);

            return result.Success;
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

            var reader = new MigoReader(_socket);
            var result = await reader.Get(Parsers.UploadGCodeResult)
                .ConfigureAwait(false);

            return result;
        }

        private async Task<int> Write(IAsyncEnumerable<ReadOnlyMemory<byte>> commandChunks)
        {
            int bytesSent = 0;

            await foreach (var chunk in commandChunks)
            {
                bytesSent += await _socket.SendAsync(chunk, SocketFlags.None)
                    .ConfigureAwait(false);
            }

            return bytesSent;
        }
        
        private async Task<int> Write(IEnumerable<ReadOnlyMemory<byte>> chunks)
        {
            int bytesSent = 0;
            
            foreach (var chunk in chunks)
            {
                Console.WriteLine($"processing command chunk of {chunk.Length} bytes");
                bytesSent += await _socket.SendAsync(chunk, SocketFlags.None)
                    .ConfigureAwait(false);
            }

            return bytesSent;
        }
    }
}
