using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib;
using MigoLib.FileUpload;
using MigoLib.Print;
using MigoLib.PrinterInfo;
using MigoLib.Scenario;
using MigoLib.Socket;
using MigoLib.State;
using MigoLib.ZOffset;

namespace MigoToolGui.Domain
{
    public class MigoProxyService : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private Migo _migo;
        private readonly CancellationTokenSource _tokenSource;
        private readonly ILogger<MigoProxyService> _logger;
        private MigoEndpoint _endpoint;

        public MigoProxyService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _tokenSource = new CancellationTokenSource();
            _logger = loggerFactory.CreateLogger<MigoProxyService>();
        }

        public IAsyncEnumerable<MigoStateModel> GetStateStream(CancellationToken token)
        {
            try
            {
                return _migo.GetStateStream(token);
            }
            catch (TaskCanceledException _)
            {
                // ignore
            }

            return default!;
        }

        public IAsyncEnumerable<FilePercentResult> GetProgressStream(CancellationToken token)
        {
            try
            {
                return _migo.GetProgressStream(token);
            }
            catch (TaskCanceledException _)
            {
                // ignore
            }

            return default!;
        }

        public IAsyncEnumerable<SafeSocketStatus> GetConnectionStatusStream(CancellationToken token)
        {
            try
            {
                return _migo.GetConnectionStatusStream(token);
            }
            catch (TaskCanceledException _)
            {
                // ignore
            }

            return default!;
        }

        public Task<ZOffsetModel> GetZOffset() => _migo.GetZOffset();
        
        public Task<ZOffsetModel> SetZOffset(double zOffset) 
            => _migo.SetZOffset(zOffset);
        
        public void Dispose()
        {
            _tokenSource.Cancel();
        }

        public Task<StartPrintResult> PreheatAndPrint(string filePath, double? preheatTemperature)
        {
            var fileName = Path.GetFileName(filePath);
            var logger = _loggerFactory.CreateLogger<PreheatAndPrint>();

            var preheatAndPrint = new PreheatAndPrint(logger, _migo, fileName, preheatTemperature);
            
            return preheatAndPrint.Execute(CancellationToken.None);
        }

        public Task<StopPrintResult> StopPrint()
            => _migo.StopPrint();

        public void SwitchTo(MigoEndpoint endpoint)
        {
            _endpoint = endpoint;
            
            Reconnect();
        }

        public Task UploadGcode(string fileName)
        {
            _logger.LogInformation($"Uploading {fileName}...");
            try
            {
                return _migo.UploadGCodeFile(fileName);
            }
            finally
            {
                _logger.LogInformation($"Upload of file {fileName} completed");
            }
        }

        public Task<PrinterInfoResult> GetPrinterInfo() => _migo.GetPrinterInfo();

        public Task ExecuteGCode(string[] lines) => _migo.ExecuteGCode(lines);

        public Task ExecuteGCode(string line) => _migo.ExecuteGCode(new [] {line});

        public Task MoveToZOffset(double zOffset) => _migo.SetCurrentPosition(50, 50, zOffset);

        public IAsyncEnumerable<BedLevelingCalibrationResult> StartZOfsetCalibration(CancellationToken token)
        {
            var logger = _loggerFactory.CreateLogger<BedLevelingCalibration>();
            var scenario = new BedLevelingCalibration(logger, _migo, BedLevelingCalibrationMode.FivePoints);
            return scenario.Execute(token);
        }

        public void Reconnect()
        {
            _logger.LogDebug($"connecting to {_endpoint}");
            _migo?.Dispose();
            
            _migo = new Migo(_loggerFactory, _endpoint, ErrorHandlingPolicy.Default);
            _logger.LogDebug("created new migo connection");
        }
    }
}