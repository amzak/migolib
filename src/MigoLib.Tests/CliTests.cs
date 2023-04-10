using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MigoLib.Fake;
using NUnit.Framework;
using Serilog;

namespace MigoLib.Tests
{
    [TestFixture]
    public class CliTests
    {
        private long _gCodeSize;

        private MigoEndpoint Endpoint => TestEnvironment.Endpoint;

        [OneTimeSetUp]
        public void Init()
        {
            var fileInfo = new FileInfo(TestEnvironment.GCodeFile);
            
            // total length = sizeOf(file) + preamble;
            var fileSize = fileInfo.Length;
            _gCodeSize = fileSize 
                         +"filestart;".Length 
                         + fileSize.ToString().Length
                         + ";;".Length
                         + TestEnvironment.GCodeFileName.Length;
        }
        
        [Test]
        public async Task Should_set_z_offset()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyZOffset(1.0);
            
            var process = await ExecuteCommand(Endpoint, "set zoffset 1.0")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        [Test]
        public async Task Should_get_z_offset()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyZOffset(1.0);
            
            var process = await ExecuteCommand(Endpoint, "get zoffset")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        [Test]
        public async Task Should_get_state()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.Stream) // state can only be responded as incoming stream
                .StreamReplyCount(1);
            
            var process = await ExecuteCommand(Endpoint, "get state")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        [Test]
        public async Task Should_execute_gcode()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyGCodeDone();
            
            var process = await ExecuteCommand(Endpoint, "exec gcode \"M851\"")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }
        
        [Test]
        public async Task Should_upload_gcode_file()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyUploadCompleted()
                .ExpectBytes(_gCodeSize);
            
            var process = await ExecuteCommand(Endpoint, "exec upload \"Resources/3DBenchy.gcode\"")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        [Test]
        public async Task Should_start_printing_selected_file()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyPrintStarted("3DBenchy.gcode");

            var process = await ExecuteCommand(Endpoint, "exec print \"3DBenchy.gcode\"")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        [Test]
        public async Task Should_stop_printing()
        {
            TestEnvironment.FakeMigo
                .ReplyPrintStopped();

            var process = await ExecuteCommand(Endpoint, "exec stop")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        [Test]
        public async Task Should_set_bed_temperature()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyGCodeDone();

            var process = await ExecuteCommand(Endpoint, "set temperature --bed=100")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        [Test]
        public async Task Should_set_nozzle_temperature()
        {
            TestEnvironment.FakeMigo
                .ReplyMode(FakeMigoMode.RequestReply)
                .ReplyGCodeDone();

            var process = await ExecuteCommand(Endpoint, "set temperature --nozzle=250")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        private async Task<Process> ExecuteCommand(MigoEndpoint endpoint, string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(
                    "dotnet", 
                    $"run -p ../../../../../src/MigoToolCli/MigoToolCli.csproj -- --endpoint={endpoint} {command}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            var err = await process.StandardError.ReadToEndAsync();
            Log.Information(output);
            Log.Information(err);

            return process;
        }
    }
}