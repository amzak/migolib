using NUnit.Framework;
using Serilog;
using Serilog.Extensions.Logging;

namespace MigoLib.Tests
{
    [SetUpFixture]
    public class Init
    {
        internal static SerilogLoggerFactory LoggerFactory { get; set; }
        
        [OneTimeSetUp]
        public void SetUp()
        {
            var log = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo
                    .NUnitOutput(outputTemplate: "[{Timestamp:mm:ss:fff} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            
            Log.Logger = log;
            LoggerFactory = new SerilogLoggerFactory(log);
            
            Log.Debug("Logger set up");
        }
    }
}