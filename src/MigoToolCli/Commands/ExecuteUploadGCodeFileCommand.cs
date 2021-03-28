using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MigoLib;
using Serilog;

namespace MigoToolCli.Commands
{
    public class ExecuteUploadGCodeFileCommand : MigoCliCommand<string>
    {
        private CancellationTokenSource _cts;
        private new const string Name = "upload";
        private new const string Description = "Uploads gcode file to migo";
        
        public ExecuteUploadGCodeFileCommand() 
            : base(Name, Description)
        {
        }

        protected override async Task Handle(MigoEndpoint endpoint, string fileName)
        {
            Log.Information($"Uploading file from path {fileName}...");
            var migo = MigoFactory.Create(endpoint);

            _cts = new CancellationTokenSource();

            await Task.WhenAll(
                    UploadFile(migo, fileName, _cts),
                    OnProgressUpdate(migo, _cts.Token))
                .ConfigureAwait(false); 
        }

        private async Task UploadFile(Migo migo, string fileName, CancellationTokenSource cts)
        {   
            var result = await migo.UploadGCodeFile(fileName)
                .ConfigureAwait(false);
            
            var json = JsonSerializer.Serialize(result);
            Log.Information(json);

            cts.Cancel();
        }

        private async Task OnProgressUpdate(Migo migo, CancellationToken token)
        {
            try
            {
                var (left, top) = Console.GetCursorPosition();
                
                await foreach (var percentResult in migo.GetProgressStream(token))
                {
                    Console.Write($"{percentResult.Percent.ToString()}%");
                    Console.SetCursorPosition(left, top);
                }
            }
            catch (TaskCanceledException _)
            {
                // ignore
            }
        }
    }
}