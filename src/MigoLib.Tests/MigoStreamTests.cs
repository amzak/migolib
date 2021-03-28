using System;
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
        private ILogger<MigoStreamTests> _logger;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _logger = Init.LoggerFactory.CreateLogger<MigoStreamTests>();
            var logger = Init.LoggerFactory.CreateLogger<FakeMigo>();
            _fakeMigo = new FakeMigo(Ip, Port, logger);
            _fakeMigo.Start();

            _fakeMigo
                .ReplyMode(FakeMigoMode.Stream)
                .StreamReplyCount(TestStreamSize);
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

        [Test]
        public async Task Should_stop_stream()
        {
            _fakeMigo.StreamReplyCount(0);

            _tokenSource.CancelAfter(TimeSpan.FromSeconds(1));

            await Task.WhenAny(
                CheckStream(_migo, _tokenSource.Token),
                CancelStream(_tokenSource));
        }

        private async Task CancelStream(CancellationTokenSource tokenSource)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            tokenSource.Cancel();
        }

        private async Task CheckStream(Migo migo, CancellationToken token)
        {
            try
            {
                await foreach (var _ in migo.GetStateStream(token))
                {
                    // nop
                }
            }
            catch (TaskCanceledException _)
            {   
                // ignore
            }
        }
    }
}