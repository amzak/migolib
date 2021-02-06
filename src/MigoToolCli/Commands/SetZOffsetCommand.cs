using System.Threading.Tasks;
using MigoLib;
using Serilog;

namespace MigoToolCli.Commands
{
    public class SetZOffsetCommand : MigoCliCommand<double>
    {
        private new const string Name = "zoffset";
        private new const string Description = "Sets zoffset";

        public SetZOffsetCommand() 
            : base(Name, Description)
        {
        }
        
        protected override async Task Handle(MigoEndpoint endpoint, double zoffset)
        {
            Log.Information($"Setting ZOffset to {zoffset.ToString()}...");
            var migo = MigoFactory.Create(endpoint);
            var result = await migo.SetZOffset(zoffset)
                .ConfigureAwait(false);
            Log.Information($"ZOffset = {result.ZOffset.ToString()}");
        }
    }
}