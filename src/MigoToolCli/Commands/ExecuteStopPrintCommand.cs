using System.Text.Json;
using System.Threading.Tasks;
using MigoLib;
using Serilog;

namespace MigoToolCli.Commands
{
    public class ExecuteStopPrintCommand : MigoCliCommand
    {
        private new const string Name = "stop";
        private new const string Description = "Stops printing";

        public ExecuteStopPrintCommand()
            : base(Name, Description)
        {
        }

        protected override async Task Handle(MigoEndpoint endpoint)
        {
            Log.Information($"Stopping print job...");
            var migo = MigoFactory.Create(endpoint);
            var result = await migo.StopPrint()
                .ConfigureAwait(false);
            
            var json = JsonSerializer.Serialize(result);
            Log.Information(json);
        }
    }
}