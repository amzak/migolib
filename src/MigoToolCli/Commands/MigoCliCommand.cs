using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using System.Threading.Tasks;
using Command = System.CommandLine.Command;

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

            if (endpoint == default)
            {
                Console.WriteLine("Input error: invalid endpoint");
                return ResultCode.Failure;
            }

            try
            {
                await Handle(endpoint).ConfigureAwait(false);
                Console.WriteLine("OK.");
            }
            catch (SocketException socketException)
            {
                Console.WriteLine($"Connection error: {(int)socketException.SocketErrorCode} {socketException.Message}");
                return ResultCode.Failure;
            }
            
            return ResultCode.Success;
        }

        protected abstract Task Handle(MigoEndpoint endpoint);
    }
    
    public abstract class MigoCliCommand<T> : Command
    {
        protected MigoCliCommand(string name, string description)
            : base(name, description)
        {
            AddArgument(new Argument<T>("parameter"));
            Handler = CommandHandler.Create<ParseResult, T>(HandleInternal);
        }

        private async Task<int> HandleInternal(ParseResult parseResult, T parameter)
        {
            var endpoint = parseResult.RootCommandResult
                .OptionResult("--endpoint")?
                .GetValueOrDefault<MigoEndpoint>();

            if (endpoint == default)
            {
                Console.WriteLine("Input error: invalid endpoint");
                return ResultCode.Failure;
            }

            try
            {
                await Handle(endpoint, parameter).ConfigureAwait(false);
                Console.WriteLine("OK.");
            }
            catch (SocketException socketException)
            {
                Console.WriteLine($"Connection error: {(int)socketException.SocketErrorCode} {socketException.Message}");
                return ResultCode.Failure;
            }
            
            return ResultCode.Success;
        }

        protected abstract Task Handle(MigoEndpoint endpoint, T param);
    }
}