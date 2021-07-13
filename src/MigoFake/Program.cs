using System;
using MigoLib;
using MigoLib.Tests;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace MigoFake
{
    class Program
    {
        private static FakeMigo _migo;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);           

            var log = new LoggerConfiguration()
                .WriteTo
                .Console()
                .CreateLogger();
            Log.Logger = log;
            
            var endpoint = new MigoEndpoint("127.0.0.1", 10086);
            _migo = new FakeMigo(endpoint);
            _migo.Start();

            Console.ReadKey();
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e) 
            => _migo.Stop();
    }
}