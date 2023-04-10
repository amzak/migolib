using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MigoLib.Fake;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var ipAddress = new Option<string>(
            name: "--ip",
            description: "IP address of fake Migo",
            getDefaultValue: () => "127.0.0.1");

        var port = new Option<ushort>(
            name: "--port",
            description: "Port of fake Migo",
            getDefaultValue: () => 5100);

        var rootCommand = new RootCommand("Fake Migo CLI");
        rootCommand.AddOption(ipAddress);
        rootCommand.AddOption(port);

        rootCommand.SetHandler(RunFakeMigo, ipAddress, port, new LoggerBinder());

        return await rootCommand.InvokeAsync(args);
    }

    private static Task RunFakeMigo(string ip, ushort port, ILogger<FakeMigo> logger)
        => new FakeMigo(ip, port, logger)
            .ReplyMode(FakeMigoMode.RequestReply)
            .ReplyUploadCompleted()
            .ExpectBytes(88245)
            .Start();
}