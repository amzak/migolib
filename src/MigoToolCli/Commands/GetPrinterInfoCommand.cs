using System.Text.Json;
using System.Threading.Tasks;
using MigoLib;
using Serilog;

namespace MigoToolCli.Commands
{
    public class GetPrinterInfoCommand : MigoCliCommand
    {
        private new const string Name = "info";
        private new const string Description = "Gets printer info";

        public GetPrinterInfoCommand() 
            : base(Name, Description)
        {
        }

        protected override async Task Handle(MigoEndpoint endpoint)
        {
            var migo = MigoFactory.Create(endpoint);
            var result = await migo.GetPrinterInfo()
                .ConfigureAwait(false);

            var json = JsonSerializer.Serialize(result);
            Log.Information(json);
        }
    }
}