using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

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
            Log.Information($"Uploading file from {fileName}...");
            var migo = MigoFactory.Create(endpoint);
            var result = await migo.UploadGCodeFile(fileName)
                .ConfigureAwait(false);
            
            var json = JsonSerializer.Serialize(result);
            Log.Information(json);
        }
    }
}