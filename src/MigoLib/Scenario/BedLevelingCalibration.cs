using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib.CurrentPosition;
using MigoLib.ZOffset;

namespace MigoLib.Scenario
{
    public class BedLevelingCalibration
    {
        private readonly BedLevelingCalibrationMode _calibrationMode;
        private readonly ILogger<BedLevelingCalibration> _logger;
        private readonly Migo _migo;

        public BedLevelingCalibration(ILogger<BedLevelingCalibration> logger, Migo migo, BedLevelingCalibrationMode calibrationMode)
        {
            _calibrationMode = calibrationMode;
            _migo = migo;
            _logger = logger;
        }

        public async IAsyncEnumerable<BedLevelingCalibrationResult> Execute(CancellationToken token)
        {
            var points = GetPoints(_calibrationMode);

            _logger.LogInformation("Home X&Y");

            ZOffsetModel zOffset = await _migo.GetZOffset().ConfigureAwait(false);
            await HomeXY().ConfigureAwait(false);

            foreach (var (x, y) in points)
            {
                _logger.LogInformation($"Moving to ({x},{y})");
                await MoveTo(x, y, 10).ConfigureAwait(false);
                var z = zOffset.ZOffset;
                _logger.LogInformation($"Moving to zoffset {z}");
                await MoveTo(x, y, z).ConfigureAwait(false);
                
                yield return new BedLevelingCalibrationResult
                {
                    Current = new Position(x, y, z)
                };
                
                await MoveTo(x, y, 10).ConfigureAwait(false);
                
                _logger.LogInformation($"Step completed.");
            }
            
            _logger.LogInformation($"Scenario completed.");
        }

        private Task HomeXY() => ExecuteGCode("G28 X0 Y0");

        private Task ExecuteGCode(string gcode)
        {
            var lines = new[]
            {
                gcode
            };

            return _migo.ExecuteGCode(lines);
        }
        
        private Task MoveTo(double x, double y, double z)
            => _migo.SetCurrentPosition(x, y, z);

        private IEnumerable<(double, double)> GetPoints(BedLevelingCalibrationMode calibrationMode)
        {
            switch (calibrationMode)
            {
                case BedLevelingCalibrationMode.FivePoints:
                    yield return (20, 20);
                    yield return (80, 20);
                    yield return (50, 50);
                    yield return (20, 80);
                    yield return (80, 80);
                    break;
                case BedLevelingCalibrationMode.NinePoints:
                    yield return (20, 20);
                    yield return (50, 20);
                    yield return (80, 20);
                    yield return (80, 50);
                    yield return (50, 50);
                    yield return (20, 50);
                    yield return (20, 80);
                    yield return (50, 80);
                    yield return (80, 80);
                    break;
            }
        }
    }
}