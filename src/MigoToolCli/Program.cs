using System.CommandLine;
using System.Threading.Tasks;
using MigoToolCli.Commands;

namespace MigoToolCli
{
    class Program
    {
        private const string AppDescription = "Unofficial Migo CLI tool";
        
        static async Task<int> Main(string[] args)
        {
            var root = new RootCommand(AppDescription);
            root.AddOption(new Option<MigoEndpoint>("--endpoint"));

            root.AddCommand(new GetCommands());
            root.AddCommand(new SetCommands());
            root.AddCommand(new ExecCommands());

            return await root.InvokeAsync(args).ConfigureAwait(false);
        }
    }
}