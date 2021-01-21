using System;
using System.CommandLine;
using System.Threading.Tasks;
using MigoLib;

namespace MigoToolCli.Commands
{
    public class SetZOffsetCommand : MigoCliCommand<double>
    {
        private new const string Name = "zoffset";
        private new const string Description = "Sets zoffset";
        
        public SetZOffsetCommand() : base(Name, Description)
        {
            AddArgument(new Argument<double>("zoffset"));
        }
        
        protected override async Task Handle(MigoEndpoint endpoint, double zOffset)
        {
            var migo = new Migo(endpoint.Ip.ToString(), endpoint.Port);
            var result = await migo.SetZOffset(zOffset)
                .ConfigureAwait(false);
            Console.WriteLine($"ZOffset = {result.ZOffset.ToString()}");
        }
    }
}