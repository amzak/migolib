using System;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using System.Threading.Tasks;
using MigoLib;
using Command = System.CommandLine.Command;

namespace MigoToolCli.Commands
{
    public class GetZOffsetCommand : Command
    {
        private new const string Name = "zoffset";
        private new const string Description = "Gets zoffset";

        public GetZOffsetCommand() : base(Name, Description)
        {
            Handler = CommandHandler.Create<ParseResult>(GetZOffset);
        }

        private async Task<int> GetZOffset(ParseResult parseResult)
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
                var migo = new Migo(endpoint.Ip.ToString(), endpoint.Port);
                var result = await migo.GetZOffset()
                    .ConfigureAwait(false);
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