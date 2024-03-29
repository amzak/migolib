using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using MigoLib.CurrentPosition;
using MigoLib.Fake;
using MigoLib.State;
using NUnit.Framework;
using Serilog;

namespace MigoLib.Tests
{
    [TestFixture]
    public class MigoTests
    {
        private long _gCodeSize;
        private int _preambleSize;
        private Migo _migo;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {   
            var fileInfo = new FileInfo(TestEnvironment.GCodeFile);
            
            // total length = sizeOf(file) + preamble;
            var fileSize = fileInfo.Length;
            _preambleSize = "filestart;".Length
                            + fileSize.ToString().Length
                            + ";;".Length
                            + TestEnvironment.GCodeFileName.Length;

            _gCodeSize = fileSize + _preambleSize;
        }

        [SetUp]
        public void SetUp()
        {
            _migo = TestMigo();
        }

        private Migo TestMigo()
        {
            return new Migo(Init.LoggerFactory, TestEnvironment.Endpoint, ErrorHandlingPolicy.Default);
        }

        [TearDown]
        public void TearDown()
        {
            _migo.Dispose();
        }


        [Test]
        public async Task Should_get_migo_state()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.Stream) // state can only be responded as incoming stream
                .StreamReplyCount(1);

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
            TestEnvironment.FakeMigo
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
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.Reply)
                .ReplyZOffset(expectedOffset);

            var result = await _migo.GetZOffset()
                .ConfigureAwait(false);

            result.ZOffset.Should().Be(expectedOffset);
        }

        [Test]
        public async Task Should_execute_g_code()
        {
            TestEnvironment.FakeMigo
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
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyUploadCompleted()
                .ExpectBytes(_gCodeSize);

            await UploadFile().ConfigureAwait(false);

            TestEnvironment.FakeMigo.ReceivedBytes.Should().BePositive();
        }

        private async Task UploadFile()
        {
            await _migo.UploadGCodeFile(TestEnvironment.GCodeFile)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Should_verify_received_data_with_md5_hash()
        {
            var expected = await GetMD5(TestEnvironment.GCodeFile)
                .ConfigureAwait(false);

            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyUploadCompleted()
                .ExpectBytes(_gCodeSize);

            await UploadFile().ConfigureAwait(false);

            var hash = await TestEnvironment.FakeMigo.GetMD5(_preambleSize)
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
            Log.Debug("Should_get_file_upload_progress_percent()");
            byte expectedPercent = 10;
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.Reply)
                .ReplyFilePercent(expectedPercent);

            var percentResult = await _migo.GetFilePercent()
                .ConfigureAwait(false);

            percentResult.Percent.Should().Be(expectedPercent);
            Log.Debug("Should_get_file_upload_progress_percent() end");
        }

        [Test]
        public async Task Should_start_print_successfully()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyPrintStarted(TestEnvironment.GCodeFileName);

            var result = await _migo.StartPrint(TestEnvironment.GCodeFile)
                .ConfigureAwait(false);

            result.PrintStarted.Should().BeTrue();
            result.Success.Should().BeTrue();
        }
        
        [Test]
        public async Task Should_fail_to_start_printing()
        {
            Log.Debug("Should_fail_to_start_printing()");

            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyPrintFailed(TestEnvironment.GCodeFileName);

            var result = await _migo.StartPrint(TestEnvironment.GCodeFile)
                .ConfigureAwait(false);

            result.PrintStarted.Should().BeFalse();
            result.Success.Should().BeTrue();
            
            Log.Debug("Should_fail_to_start_printing() end");
        }

        [Test]
        public async Task Should_stop_print_successfully()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyPrintStopped();

            var result = await _migo.StopPrint()
                .ConfigureAwait(false);

            result.Success.Should().BeTrue();
        }

        [Test]
        public async Task Should_get_printer_info()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.Reply)
                .ReplyPrinterInfo();

            var result = await _migo.GetPrinterInfo()
                .ConfigureAwait(false);

            result.StatedDescription.Should().Be("modelprinting:3DBenchy.gcode");
        }

        [Test]
        public async Task Should_set_current_position()
        {
            var position = new Position(10, 5, 2);
            
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyCurrentPosition(position);

            var result = await _migo.SetCurrentPosition(position.X, position.Y, position.Z)
                .ConfigureAwait(false);

            result.Success.Should().BeTrue();
            result.X.Should().Be(position.X);
            result.Y.Should().Be(position.Y);
            result.Z.Should().Be(position.Z);
        }
    }
}