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
        private const string MigoIp = "192.168.2.57";
        
        [Test]
        [Explicit]
        public async Task Should_set_zoffset_and_receive_correct_response()
        {
            var endpoint = new MigoEndpoint(MigoIp, MigoPort);
            var migo = new Migo(Init.LoggerFactory, endpoint, ErrorHandlingPolicy.Default);
            
            var expectedOffset = -0.8;
            var actualOffset = await migo.SetZOffset(expectedOffset).ConfigureAwait(false);

            actualOffset.Should().Be(expectedOffset);
        }

        [Test]
        [Explicit]
        public async Task Should_get_migo_state()
        {
            var endpoint = new MigoEndpoint(MigoIp, MigoPort);
            var migo = new Migo(Init.LoggerFactory, endpoint, ErrorHandlingPolicy.Default);

            var state = await migo.GetState().ConfigureAwait(false);
            
            Log.Information(JsonConvert.SerializeObject(state));
            state.NozzleTemp.Should().BePositive();
        }
        
        [Test]
        [Explicit]
        public async Task Should_get_migo_printerinfo()
        {
            var endpoint = new MigoEndpoint(MigoIp, MigoPort);
            var migo = new Migo(Init.LoggerFactory, endpoint, ErrorHandlingPolicy.Default);

            var state = await migo.GetPrinterInfo().ConfigureAwait(false);
            
            Log.Information(JsonConvert.SerializeObject(state));

            state.Success.Should().BeTrue();
        }

        [Test]
        [Explicit]
        public async Task Should_exec_gcode()
        {
            var endpoint = new MigoEndpoint(MigoIp, MigoPort);
            var migo = new Migo(Init.LoggerFactory, endpoint, ErrorHandlingPolicy.Default);

            var gcode = "G28 X0 Y0";
            var result = await migo.ExecuteGCode(new[] {gcode})
                .ConfigureAwait(false);
            
            result.Success.Should().BeTrue();
        }
    }
}