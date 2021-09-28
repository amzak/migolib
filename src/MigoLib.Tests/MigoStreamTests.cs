using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigoLib.Fake;
using NUnit.Framework;

namespace MigoLib.Tests
{
    [TestFixture]
    public class MigoStreamTests
    {
        private const int ExpectedStreamSize = 10; 
        
        private Migo _migo;
        private CancellationTokenSource _tokenSource;
        private ILogger<MigoStreamTests> _logger;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _logger = Init.LoggerFactory.CreateLogger<MigoStreamTests>();
            var logger = Init.LoggerFactory.CreateLogger<FakeMigo>();

            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.Stream)
                .StreamReplyCount(ExpectedStreamSize);
        }

        [SetUp]
        public void Setup()
        {
            _migo = new Migo(Init.LoggerFactory, TestEnvironment.Endpoint, ErrorHandlingPolicy.Default);
            _tokenSource = new CancellationTokenSource();
        }

        [TearDown]
        public void Cleanup()
        {
            _migo.Dispose();
        }
        
        [Test]
        public async Task Should_receive_state_stream_of_known_size()
        {
            var stream = _migo.GetStateStream(_tokenSource.Token);

            var counter = await RunStreamTest(stream, ExpectedStreamSize)
                .ConfigureAwait(false);

            counter.Should().Be(ExpectedStreamSize);
        }

        [Test]
        public async Task Should_receive_progress_stream_of_known_size()
        {
            var stream = _migo.GetProgressStream(_tokenSource.Token);
            
            var counter = await RunStreamTest(stream, ExpectedStreamSize)
                .ConfigureAwait(false);

            counter.Should().Be(ExpectedStreamSize);
        }

        private async Task<int> RunStreamTest<T>(IAsyncEnumerable<T> stream, int expectedStreamSize)
        {
            int counter = 0;
            await foreach (var _ in stream)
            {
                counter++;
                
                _logger.LogDebug(counter.ToString());

                if (counter == expectedStreamSize)
                {
                    _tokenSource.Cancel();
                }
            }

            return counter;
        }

        [Test]
        public async Task Should_stop_stream()
        {
            TestEnvironment.FakeMigo.StreamReplyCount(0);

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