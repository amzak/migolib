using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib;

namespace MigoToolCli.Commands
{
    public class SetTemperatureCommand : MigoCliCommand
    {
        private new const string Name = "temperature";
        private new const string Description = "Sets bed or nozzle temperature";

        private const string BedOption = "--bed";
        private const string NozzleOption = "--nozzle";
        
        public SetTemperatureCommand() 
            : base(Name, Description)
        {
            var bedOption = new Option<double>(BedOption, "Set bed temperature");  
            AddOption(bedOption);
            var nozzleOption = new Option<double>(NozzleOption, "Set nozzle temperature");  
            AddOption(nozzleOption);
        }

        protected override async Task Handle(MigoEndpoint endpoint)
        {
            var logger = Program.LoggerFactory.CreateLogger<SetTemperatureCommand>();

            var bedTemperature = CurrentParseResult.CommandResult
                .OptionResult(BedOption)?
                .GetValueOrDefault<double>();
            
            var nozzleTemperature = CurrentParseResult.CommandResult
                .OptionResult(NozzleOption)?
                .GetValueOrDefault<double>();
            
            var migo = MigoFactory.Create(endpoint);

            if (bedTemperature.HasValue)
            {
                logger.LogInformation($"Heating bed to {bedTemperature.Value}...");
                await SetBedTemperature(migo, bedTemperature.Value).ConfigureAwait(false);
            }

            if (nozzleTemperature.HasValue)
            {
                logger.LogInformation($"Heating nozzle to {nozzleTemperature.Value}...");
                await SetNozzleTemperature(migo, nozzleTemperature.Value).ConfigureAwait(false);
            }
            
            logger.LogInformation("OK.");
        }

        private async Task SetNozzleTemperature(Migo migo, double nozzleTemperature)
        {
            var gcode = new[]
            {
                $"M104 S{nozzleTemperature.ToString("###")}"
            };

            await migo.ExecuteGCode(gcode)
                .ConfigureAwait(false);
        }

        private async Task SetBedTemperature(Migo migo, double bedTemperature)
        {
            var gcode = new[]
            {
                $"M140 S{bedTemperature.ToString("###")}"
            };

            await migo.ExecuteGCode(gcode)
                .ConfigureAwait(false);
        }
    }
}