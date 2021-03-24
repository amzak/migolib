using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MigoLib.FileUpload;
using MigoLib.GCode;
using MigoLib.Print;
using MigoLib.State;
using MigoLib.ZOffset;

namespace MigoLib
{
    public class Migo : IDisposable
    {
        private bool _isConnected;
        private IPEndPoint _endPoint;
        
        private readonly ILogger<Migo> _logger;
        private readonly MigoReaderWriter _readerWriter;

        public Migo(ILoggerFactory loggerFactory, MigoEndpoint endpoint)
        {   
            _logger = loggerFactory.CreateLogger<Migo>();
            var rwLogger = loggerFactory.CreateLogger<MigoReaderWriter>();

            var (ip, port) = endpoint;
            _endPoint = new IPEndPoint(ip, port);
            
            _readerWriter = new MigoReaderWriter(_endPoint, rwLogger);
        }

        public async Task<ZOffsetModel> SetZOffset(double zOffset)
        {
            byte[] buffer = new byte[100];
            
            var chunks = CommandChain
                .On(buffer)
                .SetZOffset(zOffset)
                .GetZOffset()
                .AsChunks();

            await _readerWriter.Write(chunks)
                .ConfigureAwait(false);
            
            var result = await _readerWriter.Get(Parsers.GetZOffset)
                .ConfigureAwait(false);
            
            return result;
        }

        public async Task<ZOffsetModel> GetZOffset()
        {
            byte[] buffer = new byte[100];

            var length = await CommandChain
                .On(buffer)
                .GetZOffset()
                .ExecuteAsync()
                .ConfigureAwait(false);
    
            await _readerWriter.Write(buffer, length)
                .ConfigureAwait(false);
            
            var result = await _readerWriter.Get(Parsers.GetZOffset)
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
            byte[] buffer = new byte[100];
            
            var length = await CommandChain
                .On(buffer)
                .ExecuteGCode(lines)
                .ExecuteAsync()
                .ConfigureAwait(false);

            await _readerWriter.Write(buffer, length)
                .ConfigureAwait(false);

            var result = await _readerWriter.Get(Parsers.GetGCodeResult)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<UploadGCodeResult> UploadGCodeFile(string fileName)
        {
            var file = new GCodeFile(fileName);

            var command = new UploadGCodeCommand(file);
            
            await _readerWriter.Write(command.Chunks)
                .ConfigureAwait(false);

            var result = await _readerWriter.Get(Parsers.UploadGCodeResult)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<StartPrintResult> StartPrint(string fileName)
        {
            byte[] buffer = new byte[100];

            var length = await CommandChain
                .On(buffer)
                .StartPrint(fileName)
                .ExecuteAsync()
                .ConfigureAwait(false);
    
            await _readerWriter.Write(buffer, length)
                .ConfigureAwait(false);
            
            var result = await _readerWriter.Get(Parsers.StartPrintResult)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<FilePercentResult> GetFilePercent()
        {
            var result = await _readerWriter.Get(Parsers.GetFilePercent)
                .ConfigureAwait(false);

            return result;
        }

        public void Dispose()
        {
            _readerWriter?.Dispose();
        }

        public IAsyncEnumerable<MigoStateModel> GetStateStream(CancellationToken token) 
            => _readerWriter.GetStream(Parsers.GetState, token);
    }
}
