using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib.Print;

namespace MigoLib.Scenario
{
    public class PreheatAndPrint
    {
        private const int Tolerance = 5;

        private readonly ILogger<PreheatAndPrint> _logger;
        private readonly Migo _migo;
        private readonly string _fileName;
        private readonly double? _preheat;

        public PreheatAndPrint(ILogger<PreheatAndPrint> logger, Migo migo, string fileName, double? preheat)
        {
            _logger = logger;
            _migo = migo;
            _fileName = fileName;
            _preheat = preheat;
        }

        public async Task<StartPrintResult> Execute(CancellationToken token)
        {
            _logger.LogInformation($"Executing scenario {nameof(PreheatAndPrint)}");
            if (_preheat.HasValue)
            {
                _logger.LogInformation($"Preheating bed to  {_preheat.Value.ToString("F0")}");

                await SetBedTemperature(_preheat.Value)
                    .ConfigureAwait(false);

                CancellationCheck(token);
                
                var cts = new CancellationTokenSource();
                
                await foreach (var state in _migo.GetStateStream(cts.Token))
                {
                    CancellationCheck(token);
                    if (state.BedTemp > _preheat.Value - Tolerance)
                    {
                        cts.Cancel();
                    }
                }

                _logger.LogInformation("Preheating completed");
            }

            _logger.LogInformation($"Starting print of {_fileName} ...");

            CancellationCheck(token);

            var result = await _migo.StartPrint(_fileName)
                .ConfigureAwait(false);

            _logger.LogInformation("Scenario completed");

            return result;
        }

        private static void CancellationCheck(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException("Scenario was aborted");
            }
        }

        private async Task SetBedTemperature(double preheat)
        {
            var gcode = new[]
            {
                $"M190 S{preheat.ToString("###")}"
            };

            await _migo.ExecuteGCode(gcode)
                .ConfigureAwait(false);
        }
    }
}