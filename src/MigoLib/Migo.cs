using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MigoLib.FileUpload;
using MigoLib.State;

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

        public async Task<double> SetZOffset(double zOffset)
        {
            await EnsureConnection();
            
            byte[] buffer = new byte[100];
            
            await CommandChain
                .On(buffer)
                .SetZOffset(zOffset)
                .GetZOffset()
                .Execute()
                .ConfigureAwait(false);

            await Write(buffer)
                .ConfigureAwait(false);

            var reader = new MigoReader(_socket);

            var result = await reader.Get(Parsers.GetZOffset)
                .ConfigureAwait(false);

            return result.ZOffset;
        }

        public async Task<MigoStateModel> GetState()
        {
            await EnsureConnection();
            
            var reader = new MigoReader(_socket);

            var result = await reader.Get(Parsers.GetState)
                .ConfigureAwait(false);

            return result;
        }

        private Task<int> Write(byte[] buffer)
        {
            ArraySegment<byte> buf = buffer;
            return _socket.SendAsync(buf, SocketFlags.None);
        }

        private async Task EnsureConnection()
        {
            if (!_socket.Connected)
            {
                await _socket.ConnectAsync(_endPoint);
            }
        }
        
        public async Task<bool> ExecuteGCode(string[] lines)
        {
            await EnsureConnection();
            
            byte[] buffer = new byte[100];
            
            await CommandChain
                .On(buffer)
                .ExecuteGCode(lines)
                .Execute()
                .ConfigureAwait(false);

            await Write(buffer)
                .ConfigureAwait(false);

            var reader = new MigoReader(_socket);

            var result = await reader.Get(Parsers.GetGCodeResult)
                .ConfigureAwait(false);

            return result.Success;
        }

        public async Task<UploadGCodeResult> UploadGCodeFile(string fileName)
        {
            await EnsureConnection();
            
            byte[] buffer = new byte[UploadGCodeCommand.ChunkSize];

            var file = new GCodeFile(fileName);
            
            await CommandChain
                .On(buffer)
                .ExecuteInChunks(new UploadGCodeCommand(file));
            
            await Write(buffer)
                .ConfigureAwait(false);

            var reader = new MigoReader(_socket);
            var result = await reader.Get(Parsers.UploadGCodeResult)
                .ConfigureAwait(false);

            return result;
        }
    }
}
