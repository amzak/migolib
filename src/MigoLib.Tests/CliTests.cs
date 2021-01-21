using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using MigoToolCli;
using NUnit.Framework;

namespace MigoLib.Tests
{
    [TestFixture]
    public class CliTests
    {
        private MigoEndpoint _endpoint;
        private FakeMigo _fakeMigo;

        [OneTimeSetUp]
        public void Init()
        {
            _endpoint = new MigoEndpoint(IPAddress.Parse("127.0.0.1"), 5100);
            _fakeMigo = new FakeMigo(_endpoint, TimeSpan.FromSeconds(5));
            _fakeMigo.Start();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _fakeMigo.Stop();
        }
        
        [Test]
        public async Task Should_set_z_offset()
        {
            _fakeMigo.ReplyZOffset(1.0);
            
            var process = await ExecuteCommand(_endpoint, "set zoffset 1.0")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        [Test]
        public async Task Should_get_z_offset()
        {
            _fakeMigo.ReplyZOffset(1.0);
            
            var process = await ExecuteCommand(_endpoint, "get zoffset")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        [Test]
        public async Task Should_get_state()
        {
            _fakeMigo.ReplyState();
            
            var process = await ExecuteCommand(_endpoint, "get state")
                .ConfigureAwait(false);
            
            process.ExitCode.Should().Be(0);
        }

        private async Task<Process> ExecuteCommand(MigoEndpoint endpoint, string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(
                    "dotnet", 
                    $"run -p ../../../../src/MigoToolCli/MigoToolCli.csproj -- --endpoint={endpoint} {command}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            var err = await process.StandardError.ReadToEndAsync();
            Console.Write(output);
            Console.Write(err);

            return process;
        }
    }
}