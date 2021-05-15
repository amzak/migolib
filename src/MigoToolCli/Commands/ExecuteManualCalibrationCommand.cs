using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib;
using MigoLib.Scenario;

namespace MigoToolCli.Commands
{
    public class ExecuteManualCalibrationCommand : MigoCliCommand<BedLevelingCalibrationMode>
    {
        private new const string Name = "bedcalibration";
        private new const string Description = "Starts bed level calibration (moves the nozzle through a set of points, lowering it to zoffset)";

        public ExecuteManualCalibrationCommand() 
            : base(Name, Description)
        {
        }

        protected override async Task Handle(MigoEndpoint endpoint, BedLevelingCalibrationMode mode)
        {
            Console.WriteLine($"Starting manual bed calibration with mode = {mode}");

            var logger = Program.LoggerFactory.CreateLogger<BedLevelingCalibration>();
            var migo = MigoFactory.Create(endpoint);

            var scenario = new BedLevelingCalibration(logger, migo, mode);

            await foreach (var calibrationResult in scenario.Execute(CancellationToken.None))
            {
                Console.WriteLine($"Current point is ({calibrationResult.Current.X},{calibrationResult.Current.Y})");
                Console.WriteLine("Press any key to continue...");
                await Console.In.ReadLineAsync().ConfigureAwait(false);
                Console.WriteLine($"Next point is ({calibrationResult.Next.X},{calibrationResult.Next.Y})");
            }
        }
    }
}