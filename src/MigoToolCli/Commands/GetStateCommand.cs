using System;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using System.Threading.Tasks;
using MigoLib;
using Command = System.CommandLine.Command;

namespace MigoToolCli.Commands
{
    public class GetStateCommand : Command
    {
        private new const string Name = "state";
        private new const string Description = "Gets state";

        public GetStateCommand() : base(Name, Description)
        {
            Handler = CommandHandler.Create<ParseResult>(GetState);
        }

        private async Task<int> GetState(ParseResult parseResult)
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
                var result = await migo.GetState()
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