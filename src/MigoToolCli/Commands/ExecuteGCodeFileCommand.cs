using System;
using System.Text.Json;
using System.Threading.Tasks;
using MigoLib;

namespace MigoToolCli.Commands
{
    public class ExecuteGCodeFileCommand : MigoCliCommand<string>
    {
        private new const string Name = "file";
        private new const string Description = "Executes gcode file";
        
        public ExecuteGCodeFileCommand() 
            : base(Name, Description)
        {
        }

        protected override async Task Handle(MigoEndpoint endpoint, string fileName)
        {
            Console.WriteLine($"Uploading file from {fileName}...");
            var migo = new Migo(endpoint.Ip.ToString(), endpoint.Port);
            var result = await migo.UploadGCodeFile(fileName)
                .ConfigureAwait(false);
            
            var json = JsonSerializer.Serialize(result);
            Console.WriteLine(json);
        }
    }
}