using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MigoLib.Tests
{
    public class FakeMigo
    {
        private string _fixedReply;
        private readonly TcpListener _tcpListener;
        private readonly CancellationTokenSource _tokenSource;

        public FakeMigo(string ip, ushort port)
        {
            _tokenSource = new CancellationTokenSource();
            _tcpListener = new TcpListener(IPAddress.Parse(ip), port);
        }

        public void Start()
        {
            _tcpListener.Start();

            Task.Run(StartListening);
        }

        private async Task StartListening()
        {
            try
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                    var clientIp = ((IPEndPoint)tcpClient.Client.RemoteEndPoint)?.Address.ToString();
                    Console.WriteLine($"accepted client from {clientIp}");
                    var stream = tcpClient.GetStream();
                    var bytes = Encoding.UTF8.GetBytes(_fixedReply);
                    await stream.WriteAsync(bytes);
                    tcpClient.Close();
                    Console.WriteLine($"client from {clientIp} disconnected");
                }
            }
            finally
            {
                _tcpListener.Stop();
            }
        }

        public void Stop()
        {
            _tokenSource.Cancel();
        }

        public void FixReply(string data)
        {
            _fixedReply = data;
        }
    }
}