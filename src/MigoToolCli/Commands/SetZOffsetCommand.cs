using System;
using System.Threading.Tasks;
using MigoLib;

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
            Console.WriteLine($"Setting ZOffset to {zoffset.ToString()}..");
            var migo = new Migo(endpoint.Ip.ToString(), endpoint.Port);
            var result = await migo.SetZOffset(zoffset)
                .ConfigureAwait(false);
            Console.WriteLine($"ZOffset = {result.ZOffset.ToString()}");
        }
    }
}