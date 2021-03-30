using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using System.Threading.Tasks;
using MigoLib;
using Serilog;
using Command = System.CommandLine.Command;
using ParseResult = System.CommandLine.Parsing.ParseResult;

namespace MigoToolCli.Commands
{
    public abstract class MigoCliCommand : Command
    {
        protected MigoCliCommand(string name, string description)
            : base(name, description)
        {
            Handler = CommandHandler.Create<ParseResult>(HandleInternal);
        }

        private async Task<int> HandleInternal(ParseResult parseResult)
        {
            var endpoint = parseResult.RootCommandResult
                .OptionResult("--endpoint")?
                .GetValueOrDefault<MigoEndpoint>();

            if (!endpoint.HasValue)
            {
                Log.Error("Invalid endpoint");
                return ResultCode.Failure;
            }

            try
            {
                await Handle(endpoint.Value).ConfigureAwait(false);
                Log.Information("OK.");
            }
            catch (SocketException socketException)
            {
                Log.Error($"{(int)socketException.SocketErrorCode} {socketException.Message}");
                return ResultCode.Failure;
            }
            
            return ResultCode.Success;
        }

        protected abstract Task Handle(MigoEndpoint endpoint);
    }
    
    public abstract class MigoCliCommand<T> : Command
    {
        protected ParseResult CurrentParseResult;

        protected MigoCliCommand(string name, string description)
            : base(name, description)
        {
            AddArgument(new Argument<T>("parameter"));
            Handler = CommandHandler.Create<ParseResult, T>(HandleInternal);
        }

        private async Task<int> HandleInternal(ParseResult parseResult, T parameter)
        {
            CurrentParseResult = parseResult;
            
            var endpoint = parseResult.RootCommandResult
                .OptionResult("--endpoint")?
                .GetValueOrDefault<MigoEndpoint>();

            if (!endpoint.HasValue)
            {
                Log.Error("Invalid endpoint");
                return ResultCode.Failure;
            }

            try
            {
                await Handle(endpoint.Value, parameter).ConfigureAwait(false);
                Log.Information("OK.");
            }
            catch (SocketException socketException)
            {
                Log.Error($"{(int)socketException.SocketErrorCode} {socketException.Message}");
                return ResultCode.Failure;
            }
            
            return ResultCode.Success;
        }

        protected abstract Task Handle(MigoEndpoint endpoint, T param);
    }
}