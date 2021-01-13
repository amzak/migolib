using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MigoLib.State;
using NUnit.Framework;

namespace MigoLib.Tests
{
    [TestFixture]
    public class MigoTests
    {
        private const string Ip = "127.0.0.1";
        private const ushort Port = 5100;
        private FakeMigo _fakeMigo;
        private Migo _migo;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _fakeMigo = new FakeMigo(Ip, Port);
            _fakeMigo.Start();
        }

        [SetUp]
        public void Setup()
        {
            _migo = new Migo(Ip, Port);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _fakeMigo.Stop();
        }

        [Test]
        public async Task Should_receive_migo_state()
        {
            var expected = Some.MigoStateModel;
            var reply = FormatReply(expected);
            _fakeMigo.FixReply(reply);

            var state = await _migo.GetState()
                .ConfigureAwait(false);

            state.Should().BeEquivalentTo(expected);
        }

        private string FormatReply(MigoStateModel migoStateModel)
        {
            return $"@#state;{migoStateModel.HeadX.ToString("F2")};" +
                   $"{migoStateModel.HeadX.ToString("F2")};" +
                   $"{migoStateModel.BedTemp.ToString()};" +
                   $"{migoStateModel.NozzleTemp.ToString()};0;10;1;0;0;0#@";
        }

        [Test]
        public async Task Should_set_z_offset()
        {
            var expectedOffset = -0.8d;
            _fakeMigo.FixReply($"@#ZOffsetValue:{expectedOffset.ToString("F2")}#@");
            _fakeMigo.ExpectBytes(40);

            var result = await _migo.SetZOffset(expectedOffset)
                .ConfigureAwait(false);

            result.ZOffset.Should().Be(expectedOffset);
        }
        
        [Test]
        public async Task Should_execute_g_code()
        {
            _fakeMigo.FixReply($"@#gcodedone;#@");

            var gcode = new[]
            {
                "G92 X5",
                "G0 F1200 X0",
                "G0 X5"
            };

            var result = await _migo.ExecuteGCode(gcode)
                .ConfigureAwait(false);

            result.Should().BeTrue();
        }

        [Test]
        public async Task Should_upload_gcode_file()
        {
            var filePath = "Resources/3DBenchy.gcode";
            var fileInfo = new FileInfo(filePath);
            
            _fakeMigo.FixReply($"@#fend;#@");

            var result = await _migo.UploadGCodeFile(filePath)
                .ConfigureAwait(false);

            result.Success.Should().BeTrue();
        }
    }
}