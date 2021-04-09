using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib;
using MigoLib.Print;
using MigoLib.State;
using MigoLib.ZOffset;
using MigoToolGui.Bootstrap;

namespace MigoToolGui.Domain
{
    public class MigoProxyService : IDisposable
    {
        private readonly Migo _migo;
        private readonly CancellationTokenSource _tokenSource;
        private readonly ILogger<MigoProxyService> _logger;

        public MigoProxyService(ConfigProvider configProvider, ILoggerFactory loggerFactory)
        {
            _tokenSource = new CancellationTokenSource();
            var config = configProvider.GetConfig();
            var endpoint = new MigoEndpoint(config.Ip, config.Port);
            _migo = new Migo(loggerFactory, endpoint);
            _logger = loggerFactory.CreateLogger<MigoProxyService>();
        }

        public IAsyncEnumerable<MigoStateModel> GetStateStream(CancellationToken token)
            => _migo.GetStateStream(token);

        public Task<ZOffsetModel> GetZOffset() => _migo.GetZOffset();
        
        public Task<ZOffsetModel> SetZOffset(double zOffset) 
            => _migo.SetZOffset(zOffset);
        
        public void Dispose()
        {
            _tokenSource.Cancel();
        }

        public Task<StartPrintResult> StartPrint(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return _migo.StartPrint(fileName);
        }

        public Task<StopPrintResult> StopPrint()
            => _migo.StopPrint();
    }
}