using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using System.Threading.Tasks;
using MigoLib;
using Command = System.CommandLine.Command;

namespace MigoToolCli
{
    class Program
    {
        private const int Success = 0;
        private const int Failure = 1;
        
        static async Task<int> Main(string[] args)
        {
            var zOffsetCommand = new Command("zoffset", "Sets zoffset");
            zOffsetCommand.AddArgument(new Argument<double>("zoffset"));
            zOffsetCommand.Handler = CommandHandler.Create<ParseResult, double>(SetZOffset);

            var setCommand = new Command("set", "Sets parameter");
            setCommand.AddCommand(zOffsetCommand);

            var root = new RootCommand("Unofficial Migo CLI tool");
            root.AddCommand(setCommand);
            root.AddOption(new Option<MigoEndpoint>("--endpoint"));

            return await root.InvokeAsync(args).ConfigureAwait(false);
        }

        private static async Task<int> SetZOffset(ParseResult parseResult, double zOffset)
        {
            var endpoint = parseResult.RootCommandResult
                .OptionResult("--endpoint")?
                .GetValueOrDefault<MigoEndpoint>();

            if (endpoint == default)
            {
                Console.WriteLine($"Input error: invalid endpoint");
                return Failure;
            }

            try
            {
                Console.WriteLine(endpoint);
                var migo = new Migo(endpoint.Ip.ToString(), endpoint.Port);
                var result = await migo.SetZOffset(zOffset)
                    .ConfigureAwait(false);
                Console.WriteLine($"ZOffset = {result.ZOffset.ToString()}");
                Console.WriteLine("OK.");
            }
            catch (SocketException socketException)
            {
                Console.WriteLine($"Connection error: {(int)socketException.SocketErrorCode} {socketException.Message}");
                return Failure;
            }

            return Success;
        }
    }
}