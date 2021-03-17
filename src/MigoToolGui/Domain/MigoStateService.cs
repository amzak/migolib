using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib;
using MigoLib.State;
using MigoLib.ZOffset;
using MigoToolGui.Bootstrap;

namespace MigoToolGui.Domain
{
    public class MigoStateService : IDisposable
    {
        private readonly Migo _migo;
        private readonly CancellationTokenSource _tokenSource;
        private readonly ILogger<MigoStateService> _logger;

        public MigoStateService(ConfigProvider configProvider, ILoggerFactory loggerFactory)
        {
            _tokenSource = new CancellationTokenSource();
            var config = configProvider.GetConfig();
            var endpoint = new MigoEndpoint(config.Ip, config.Port);
            _migo = new Migo(loggerFactory, endpoint);
            _logger = loggerFactory.CreateLogger<MigoStateService>();
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
    }
}