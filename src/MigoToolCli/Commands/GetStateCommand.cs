using System.Threading.Tasks;
using MigoLib;

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
            var migo = new Migo(endpoint.Ip.ToString(), endpoint.Port);
            var result = await migo.GetState()
                .ConfigureAwait(false);
        }
    }
}