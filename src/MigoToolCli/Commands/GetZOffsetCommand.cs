using System.Threading.Tasks;
using MigoLib;

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
            var migo = new Migo(endpoint.Ip.ToString(), endpoint.Port);
            var result = await migo.GetZOffset()
                .ConfigureAwait(false);
        }
    }
}