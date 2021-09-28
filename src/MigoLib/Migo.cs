using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib.CurrentPosition;
using MigoLib.FileUpload;
using MigoLib.GCode;
using MigoLib.Print;
using MigoLib.PrinterInfo;
using MigoLib.Socket;
using MigoLib.State;
using MigoLib.ZOffset;

namespace MigoLib
{
    public class Migo : IDisposable
    {
        private readonly ILogger<Migo> _logger;
        private readonly MigoReaderWriter _readerWriter;

        public Migo(ILoggerFactory loggerFactory, MigoEndpoint endpoint, ErrorHandlingPolicy errorPolicy)
        {   
            _logger = loggerFactory.CreateLogger<Migo>();

            var (ip, port) = endpoint;
            var endPoint = new IPEndPoint(ip, port);
            
            _readerWriter = new MigoReaderWriter(endPoint, loggerFactory, errorPolicy);
        }

        public async Task<ZOffsetModel> SetZOffset(double zOffset)
        {
            var result = await CommandChain
                .Start()
                .SetZOffset(zOffset)
                .GetZOffset()
                .GetResult(Parsers.GetZOffset)
                .ExecuteChunksAsync(_readerWriter)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<ZOffsetModel> GetZOffset()
        {
            var result = await CommandChain
                .Start()
                .GetZOffset()
                .GetResult(Parsers.GetZOffset)
                .ExecuteAsync(_readerWriter)
                .ConfigureAwait(false);
            
            return result;
        }
        
        public async Task<MigoStateModel> GetState()
        {
            var result = await _readerWriter.Get(Parsers.GetState)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<GCodeResultModel> ExecuteGCode(string[] lines)
        {
            var result = await CommandChain
                .Start()
                .ExecuteGCode(lines)
                .GetResult(Parsers.GetGCodeResult)
                .ExecuteAsync(_readerWriter)
                .ConfigureAwait(false);

            return result;
        }

        public Task<UploadGCodeResult> UploadGCodeFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return Task.FromResult(new UploadGCodeResult
                {
                    Success = false,
                    Error = "File not found"
                });
            }
            
            var file = new GCodeFile(fileName);

            var command = new UploadGCodeCommand(file);
            
            return _readerWriter.Get(command.Chunks, Parsers.UploadGCodeResult);
        }

        public async Task<StartPrintResult> StartPrint(string fileName)
        {
            var result = await CommandChain
                .Start()
                .StartPrint(fileName)
                .GetResult(Parsers.StartPrintResult)
                .ExecuteAsync(_readerWriter)
                .ConfigureAwait(false);
    
            return result;
        }

        public async Task<StopPrintResult> StopPrint()
        {
            var result = await CommandChain
                .Start()
                .StopPrint()
                .GetResult(Parsers.StopPrintResult)
                .ExecuteAsync(_readerWriter)
                .ConfigureAwait(false);
    
            return result;
        }

        public async Task<PrinterInfoResult> GetPrinterInfo()
        {
            _logger.LogDebug("GetPrinterInfo()");
            var result = await CommandChain
                .Start()
                .GetPrinterInfo()
                .GetResult(Parsers.GetPrinterInfo)
                .ExecuteAsync(_readerWriter)
                .ConfigureAwait(false);
    
            return result;
        }

        public async Task<FilePercentResult> GetFilePercent()
        {
            var result = await _readerWriter.Get(Parsers.GetFilePercent)
                .ConfigureAwait(false);

            return result;
        }
        
        public async Task<CurrentPositionResult> SetCurrentPosition(double x, double y, double z)
        {
            var result = await CommandChain
                .Start()
                .SetCurrentPosition(x, y, z)
                .GetResult(Parsers.GetCurrentPosition)
                .ExecuteAsync(_readerWriter)
                .ConfigureAwait(false);

            return result;
        }

        public IAsyncEnumerable<MigoStateModel> GetStateStream(CancellationToken token) 
            => _readerWriter.GetStream(Parsers.GetState, token);

        public IAsyncEnumerable<FilePercentResult> GetProgressStream(CancellationToken token) 
            => _readerWriter.GetStream(Parsers.GetFilePercent, token);
        
        public IAsyncEnumerable<SafeSocketStatus> GetConnectionStatusStream(CancellationToken token) 
            => _readerWriter.GetConnectionStatusStream(token);

        public void Dispose()
        {
            _readerWriter?.Dispose();
        }
    }
}
