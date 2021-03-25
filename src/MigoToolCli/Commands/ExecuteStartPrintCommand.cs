using System.Text.Json;
using System.Threading.Tasks;
using MigoLib;
using Serilog;

namespace MigoToolCli.Commands
{
    public class ExecuteStartPrintCommand : MigoCliCommand<string>
    {
        private new const string Name = "print";
        private new const string Description = "Starts printing selected file";

        public ExecuteStartPrintCommand() 
            : base(Name, Description)
        {
        }

        protected override async Task Handle(MigoEndpoint endpoint, string fileName)
        {
            Log.Information($"Starting print job of {fileName}...");
            var migo = MigoFactory.Create(endpoint);
            var result = await migo.StartPrint(fileName)
                .ConfigureAwait(false);
            
            var json = JsonSerializer.Serialize(result);
            Log.Information(json);
        }
    }
}