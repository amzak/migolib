using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib;
using MigoLib.Scenario;
using Serilog;

namespace MigoToolCli.Commands
{
    public class ExecuteStartPrintCommand : MigoCliCommand<string>
    {
        private new const string Name = "print";
        private new const string Description = "Starts printing selected file";

        private const string PreheatOption = "--preheat";
        
        public ExecuteStartPrintCommand() 
            : base(Name, Description)
        {
            var option = new Option<double>(PreheatOption, "Set bed preheat temperature");  
            AddOption(option);
        }

        protected override async Task Handle(MigoEndpoint endpoint, string fileName)
        {
            var logger = Program.LoggerFactory.CreateLogger<PreheatAndPrint>();
            var migo = MigoFactory.Create(endpoint);

            var preheatTemperature = CurrentParseResult.CommandResult
                .OptionResult(PreheatOption)?
                .GetValueOrDefault<double>();

            var scenario = new PreheatAndPrint(logger, migo, fileName, preheatTemperature);

            var result = await scenario.Execute(CancellationToken.None)
                .ConfigureAwait(false);

            var json = JsonSerializer.Serialize(result);
            Log.Information(json);
        }
    }
}