using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using Serilog;

namespace MigoLib.Tests
{
    [TestFixture]
    public class MigoStreamTests
    {
        private const string Ip = "127.0.0.1";
        private const ushort Port = 5100;

        private const int TestStreamSize = 10; 
        
        private FakeMigo _fakeMigo;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var logger = Init.LoggerFactory.CreateLogger<FakeMigo>();
            _fakeMigo = new FakeMigo(Ip, Port, logger);
            _fakeMigo.Start();

            _fakeMigo
                .ReplyMode(FakeMigoMode.Stream)
                .ReplyCount(TestStreamSize);
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _fakeMigo.Stop();
        }

        [Test]
        public async Task Should_receive_events_stream_of_known_size()
        {
            var endpoint = new MigoEndpoint(Ip, Port);
            using var migo = new Migo(Init.LoggerFactory, endpoint);
            var tokenSource = new CancellationTokenSource();
            int maxCount = TestStreamSize;
            
            int counter = 0;
            await foreach (var _ in migo.GetStateStream(tokenSource.Token))
            {
                counter++;

                if (counter == maxCount)
                {
                    tokenSource.Cancel();
                }
            }

            counter.Should().Be(TestStreamSize);
        }
    }
}