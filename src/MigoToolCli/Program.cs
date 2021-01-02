using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace MigoToolCli
{
    class Program
    {
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

        private static void SetZOffset(ParseResult result, double zOffset)
        {
            var endpoint = result.RootCommandResult
                .OptionResult("--endpoint")?
                .GetValueOrDefault<MigoEndpoint>();
            
            Console.WriteLine(zOffset.ToString(), endpoint);
        }
    }
}