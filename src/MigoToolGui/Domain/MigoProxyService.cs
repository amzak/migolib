using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib;
using MigoLib.FileUpload;
using MigoLib.Print;
using MigoLib.Scenario;
using MigoLib.State;
using MigoLib.ZOffset;
using MigoToolGui.Bootstrap;

namespace MigoToolGui.Domain
{
    public class MigoProxyService : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private Migo _migo;
        private readonly CancellationTokenSource _tokenSource;
        private readonly ILogger<MigoProxyService> _logger;

        public MigoProxyService(ConfigProvider configProvider, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _tokenSource = new CancellationTokenSource();
            var config = configProvider.GetConfig();
            var endpoint = new MigoEndpoint(config.Ip, config.Port);
            _migo = new Migo(loggerFactory, endpoint);
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

        public Task<ZOffsetModel> GetZOffset() => _migo.GetZOffset();
        
        public Task<ZOffsetModel> SetZOffset(double zOffset) 
            => _migo.SetZOffset(zOffset);
        
        public void Dispose()
        {
            _tokenSource.Cancel();
        }

        public Task<StartPrintResult> PreheatAndPrint(string filePath, double preheatTemperature)
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
            _logger.LogDebug($"switching to {endpoint}");
            _migo.Dispose();
            _logger.LogDebug("old migo connection disposed");
            _migo = new Migo(_loggerFactory, endpoint);
            _logger.LogDebug("created new migo connection");
        }
    }
}