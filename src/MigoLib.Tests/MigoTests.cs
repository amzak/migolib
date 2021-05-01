using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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

        const string GCodeFile = "Resources/3DBenchy.gcode";
        private readonly string _gCodeFileName = Path.GetFileName(GCodeFile); 
        private long _gCodeSize;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var logger = Init.LoggerFactory.CreateLogger<FakeMigo>();

            var fileInfo = new FileInfo(GCodeFile);
            _gCodeSize = fileInfo.Length + 33;

            _fakeMigo = new FakeMigo(Ip, Port, logger);
            _fakeMigo.Start();
            
            var endpoint = new MigoEndpoint(Ip, Port);
            _migo = new Migo(Init.LoggerFactory, endpoint);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _migo.Dispose();
            _fakeMigo.Stop();
        }

        [Test]
        public async Task Should_get_migo_state()
        {
            _fakeMigo
                .ReplyMode(FakeMigoMode.Stream); // state can only be responded as incoming stream

            var state = await _migo.GetState()
                .ConfigureAwait(false);

            state.Should().BeEquivalentTo(Some.FixedStateModel);
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
            _fakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyZOffset(expectedOffset);

            var result = await _migo.SetZOffset(expectedOffset)
                .ConfigureAwait(false);

            result.ZOffset.Should().Be(expectedOffset);
        }

        [Test]
        public async Task Should_get_z_offset()
        {
            var expectedOffset = -0.8d;
            _fakeMigo
                .ReplyMode(FakeMigoMode.Reply)
                .ReplyZOffset(expectedOffset);

            var result = await _migo.GetZOffset()
                .ConfigureAwait(false);

            result.ZOffset.Should().Be(expectedOffset);
        }

        [Test]
        public async Task Should_execute_g_code()
        {
            _fakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyGCodeDone();

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
            _fakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyUploadCompleted()
                .ExpectBytes(_gCodeSize);

            await UploadFile().ConfigureAwait(false);

            _fakeMigo.ReceivedBytes.Should().BePositive();
        }

        private async Task UploadFile()
        {
            await _migo.UploadGCodeFile(GCodeFile)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Should_verify_received_data_with_md5_hash()
        {
            var expected = await GetMD5(GCodeFile)
                .ConfigureAwait(false);

            _fakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyUploadCompleted()
                .ExpectBytes(_gCodeSize);

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

        [Test]
        public async Task Should_get_file_upload_progress_percent()
        {
            byte expectedPercent = 10;
            _fakeMigo
                .ReplyMode(FakeMigoMode.Reply)
                .ReplyFilePercent(expectedPercent);

            var percentResult = await _migo.GetFilePercent()
                .ConfigureAwait(false);

            percentResult.Percent.Should().Be(expectedPercent);
        }

        [Test]
        public async Task Should_start_print_successfully()
        {
            _fakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyPrintStarted(_gCodeFileName);

            var result = await _migo.StartPrint(GCodeFile)
                .ConfigureAwait(false);

            result.PrintStarted.Should().BeTrue();
            result.Success.Should().BeTrue();
        }
        
        [Test]
        public async Task Should_fail_to_start_printing()
        {
            _fakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyPrintFailed(_gCodeFileName);

            var result = await _migo.StartPrint(GCodeFile)
                .ConfigureAwait(false);

            result.PrintStarted.Should().BeFalse();
            result.Success.Should().BeTrue();
        }

        [Test]
        public async Task Should_stop_print_successfully()
        {
            _fakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyPrintStopped();

            var result = await _migo.StopPrint()
                .ConfigureAwait(false);

            result.Success.Should().BeTrue();
        }

        [Test]
        public async Task Should_get_printer_info()
        {
            _fakeMigo
                .ReplyMode(FakeMigoMode.Reply)
                .ReplyPrinterInfo();

            var result = await _migo.GetPrinterInfo()
                .ConfigureAwait(false);

            result.StatedDescription.Should().Be("modelprinting:3DBenchy.gcode");
        }
    }
}