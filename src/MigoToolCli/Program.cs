using System.CommandLine;
using System.Threading.Tasks;
using MigoLib;
using MigoToolCli.Commands;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;

namespace MigoToolCli
{
    class Program
    {
        private const string AppDescription = "Unofficial Migo CLI tool";
        internal static SerilogLoggerFactory LoggerFactory { get; set; }

        static async Task<int> Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo
                .Console(
                    outputTemplate: "[{Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}",
                    theme: ConsoleTheme.None)
                .CreateLogger();
            Log.Logger = log;
            LoggerFactory = new SerilogLoggerFactory(log);

            var root = new RootCommand(AppDescription);
            root.AddOption(new Option<MigoEndpoint>("--endpoint"));

            root.AddCommand(new GetCommands());
            root.AddCommand(new SetCommands());
            root.AddCommand(new ExecCommands());

            return await root.InvokeAsync(args).ConfigureAwait(false);
        }
    }
}