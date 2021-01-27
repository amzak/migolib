using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using Serilog;

namespace MigoLib.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        private const ushort MigoPort = 10086;
        private const string MigoIp = "192.168.1.66";
        
        [Test]
        [Explicit]
        public async Task Should_set_zoffset_and_receive_correct_response()
        {
            var migo = new Migo(Init.LoggerFactory, MigoIp, MigoPort);
            
            var expectedOffset = -0.8;
            var actualOffset = await migo.SetZOffset(expectedOffset).ConfigureAwait(false);

            actualOffset.Should().Be(expectedOffset);
        }

        [Test]
        [Explicit]
        public async Task Should_get_migo_state()
        {
            var migo = new Migo(Init.LoggerFactory, MigoIp, MigoPort);

            var state = await migo.GetState().ConfigureAwait(false);
            
            Log.Information(JsonConvert.SerializeObject(state));
            state.NozzleTemp.Should().BePositive();
        }
    }
}