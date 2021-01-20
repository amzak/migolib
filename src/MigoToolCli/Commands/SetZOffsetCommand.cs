using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using System.Threading.Tasks;
using MigoLib;
using Command = System.CommandLine.Command;

namespace MigoToolCli.Commands
{
    public class SetZOffsetCommand : Command
    {
        private new const string Name = "zoffset";
        private new const string Description = "Sets zoffset";
        
        public SetZOffsetCommand() : base(Name, Description)
        {
            Handler = CommandHandler.Create<ParseResult, double>(SetZOffset);
            AddArgument(new Argument<double>("zoffset"));
        }
        
        private static async Task<int> SetZOffset(ParseResult parseResult, double zOffset)
        {
            var endpoint = parseResult.RootCommandResult
                .OptionResult("--endpoint")?
                .GetValueOrDefault<MigoEndpoint>();

            if (endpoint == default)
            {
                Console.WriteLine($"Input error: invalid endpoint");
                return ResultCode.Failure;
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
                return ResultCode.Failure;
            }

            return ResultCode.Success;
        }
    }
}