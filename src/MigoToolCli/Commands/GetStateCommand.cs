using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace MigoToolCli.Commands
{
    public class GetStateCommand : MigoCliCommand
    {
        private new const string Name = "state";
        private new const string Description = "Gets state";

        public GetStateCommand() : base(Name, Description)
        {
        }

        protected override async Task Handle(MigoEndpoint endpoint)
        {
            var migo = MigoFactory.Create(endpoint);
            var result = await migo.GetState()
                .ConfigureAwait(false);

            var json = JsonSerializer.Serialize(result);
            Log.Information(json);
        }
    }
}