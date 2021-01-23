using System;
using System.Text.Json;
using System.Threading.Tasks;
using MigoLib;

namespace MigoToolCli.Commands
{
    public class ExecuteGCodeCommand : MigoCliCommand<string>
    {
        private new const string Name = "gcode";
        private new const string Description = "Executes gcode";

        public ExecuteGCodeCommand() 
            : base(Name, Description)
        {
        }

        protected override async Task Handle(MigoEndpoint endpoint, string command)
        {
            var lines = command.Split(';');
            
            var migo = new Migo(endpoint.Ip.ToString(), endpoint.Port);
            var result = await migo.ExecuteGCode(lines)
                .ConfigureAwait(false);
            
            var json = JsonSerializer.Serialize(result);
            Console.WriteLine(json);
        }
    }
}