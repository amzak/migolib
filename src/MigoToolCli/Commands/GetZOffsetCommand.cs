using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace MigoToolCli.Commands
{
    public class GetZOffsetCommand : MigoCliCommand
    {
        private new const string Name = "zoffset";
        private new const string Description = "Gets zoffset";

        public GetZOffsetCommand() : base(Name, Description)
        {
        }

        protected override async Task Handle(MigoEndpoint endpoint)
        {
            var migo = MigoFactory.Create(endpoint);
            var result = await migo.GetZOffset()
                .ConfigureAwait(false);

            var json = JsonSerializer.Serialize(result);
            Log.Information(json);
        }
    }
}