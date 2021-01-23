using System.IO;
using System.Security.Cryptography;
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
        public async Task Should_get_migo_state()
        {
            var expected = Some.MigoStateModel;
            var reply = FormatReply(expected);
            _fakeMigo.FixReply(reply, true);

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
            _fakeMigo.ReplyZOffset(expectedOffset);

            var result = await _migo.SetZOffset(expectedOffset)
                .ConfigureAwait(false);

            result.ZOffset.Should().Be(expectedOffset);
        }
        
        [Test]
        public async Task Should_get_z_offset()
        {
            var expectedOffset = -0.8d;
            _fakeMigo.ReplyZOffset(expectedOffset);

            var result = await _migo.GetZOffset()
                .ConfigureAwait(false);

            result.ZOffset.Should().Be(expectedOffset);
        }

        [Test]
        public async Task Should_execute_g_code()
        {
            _fakeMigo.ReplyGCodeDone();

            var gcode = new[]
            {
                "G92 X5",
                "G0 F1200 X0",
                "G0 X5"
            };

            var result = await _migo.ExecuteGCode(gcode)
                .ConfigureAwait(false);

            result.Success.Should().BeTrue();
        }

        [Test]
        public async Task Should_upload_gcode_file()
        {
            _fakeMigo.FixReply($"@#fend;#@");

            await UploadFile().ConfigureAwait(false);

            _fakeMigo.ReceivedBytes.Should().BePositive();
        }

        private async Task UploadFile()
        {
            var filePath = "Resources/3DBenchy.gcode";
            await _migo.UploadGCodeFile(filePath)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Should_verify_received_data_with_md5_hash()
        {
            var expected = await GetMD5("Resources/3DBenchy.gcode")
                .ConfigureAwait(false);

            await UploadFile().ConfigureAwait(false);

            var hash = await _fakeMigo.GetMD5(33)
                .ConfigureAwait(false);

            hash.Should().BeEquivalentTo(expected);
        }

        private async Task<byte[]> GetMD5(string fileName)
        {
            using var md5 = MD5.Create();
            await using var stream = File.OpenRead(fileName);
            
            return await md5.ComputeHashAsync(stream);
        }
    }
}