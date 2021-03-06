﻿using System;
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
using MigoLib.State;
using MigoLib.ZOffset;

namespace MigoLib
{
    public class Migo : IDisposable
    {
        private readonly ILogger<Migo> _logger;
        private readonly MigoReaderWriter _readerWriter;

        public Migo(ILoggerFactory loggerFactory, MigoEndpoint endpoint)
        {   
            _logger = loggerFactory.CreateLogger<Migo>();

            var (ip, port) = endpoint;
            var endPoint = new IPEndPoint(ip, port);
            
            _readerWriter = new MigoReaderWriter(endPoint, loggerFactory);
        }

        public async Task<ZOffsetModel> SetZOffset(double zOffset)
        {
            var chunks = CommandChain
                .Start()
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
            var buffer = await CommandChain
                .Start()
                .GetZOffset()
                .ExecuteAsync()
                .ConfigureAwait(false);
    
            await _readerWriter.Write(buffer)
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
            var buffer = await CommandChain
                .Start()
                .ExecuteGCode(lines)
                .ExecuteAsync()
                .ConfigureAwait(false);

            await _readerWriter.Write(buffer)
                .ConfigureAwait(false);

            var result = await _readerWriter.Get(Parsers.GetGCodeResult)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<UploadGCodeResult> UploadGCodeFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return new UploadGCodeResult
                {
                    Success = false,
                    Error = "File not found"
                };
            }
            
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
            var buffer = await CommandChain
                .Start()
                .StartPrint(fileName)
                .ExecuteAsync()
                .ConfigureAwait(false);
    
            await _readerWriter.Write(buffer)
                .ConfigureAwait(false);
            
            var result = await _readerWriter.Get(Parsers.StartPrintResult)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<StopPrintResult> StopPrint()
        {
            var buffer = await CommandChain
                .Start()
                .StopPrint()
                .ExecuteAsync()
                .ConfigureAwait(false);
    
            await _readerWriter.Write(buffer)
                .ConfigureAwait(false);
            
            var result = await _readerWriter.Get(Parsers.StopPrintResult)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<PrinterInfoResult> GetPrinterInfo()
        {
            var buffer = await CommandChain
                .Start()
                .GetPrinterInfo()
                .ExecuteAsync()
                .ConfigureAwait(false);
    
            await _readerWriter.Write(buffer)
                .ConfigureAwait(false);

            var result = await _readerWriter.Get(Parsers.GetPrinterInfo)
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
            var buffer = await CommandChain
                .Start()
                .SetCurrentPosition(x, y, z)
                .ExecuteAsync()
                .ConfigureAwait(false);

            await _readerWriter.Write(buffer)
                .ConfigureAwait(false);
            
            var result = await _readerWriter.Get(Parsers.GetCurrentPosition)
                .ConfigureAwait(false);
            
            return result;
        }

        public IAsyncEnumerable<MigoStateModel> GetStateStream(CancellationToken token) 
            => _readerWriter.GetStream(Parsers.GetState, token);

        public IAsyncEnumerable<FilePercentResult> GetProgressStream(CancellationToken token) 
            => _readerWriter.GetStream(Parsers.GetFilePercent, token);
        
        public void Dispose()
        {
            _readerWriter?.Dispose();
        }
    }
}
