using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MigoLib.Tests
{
    [TestFixture]
    public class MigoStreamTests
    {
        private const string Ip = "127.0.0.1";
        private const ushort Port = 5100;

        private const int TestStreamSize = 10; 
        
        private Migo _migo;
        private FakeMigo _fakeMigo;
        private CancellationTokenSource _tokenSource;

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

        [SetUp]
        public void Setup()
        {
            var endpoint = new MigoEndpoint(Ip, Port);
            _migo = new Migo(Init.LoggerFactory, endpoint);
            _tokenSource = new CancellationTokenSource();
        }

        [TearDown]
        public void Cleanup()
        {
            _migo.Dispose();
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _fakeMigo.Stop();
        }

        [Test]
        public async Task Should_receive_state_stream_of_known_size()
        {
            var stream = _migo.GetStateStream(_tokenSource.Token);
            
            var counter = await RunStreamTest(stream)
                .ConfigureAwait(false);

            counter.Should().Be(TestStreamSize);
        }

        [Test]
        public async Task Should_receive_progress_stream_of_known_size()
        {
            var stream = _migo.GetProgressStream(_tokenSource.Token);
            
            var counter = await RunStreamTest(stream)
                .ConfigureAwait(false);

            counter.Should().Be(TestStreamSize);
        }

        private async Task<int> RunStreamTest<T>(IAsyncEnumerable<T> stream)
        {
            int counter = 0;
            await foreach (var _ in stream)
            {
                counter++;

                if (counter == TestStreamSize)
                {
                    _tokenSource.Cancel();
                }
            }

            return counter;
        }
    }
}