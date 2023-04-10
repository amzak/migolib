using System.CommandLine.Binding;
using Microsoft.Extensions.Logging;

namespace MigoLib.Fake;

public class LoggerBinder : BinderBase<ILogger<FakeMigo>>
{
    protected override ILogger<FakeMigo> GetBoundValue(BindingContext bindingContext) => GetLogger(bindingContext);

    private ILogger<FakeMigo> GetLogger(BindingContext context)
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Trace));
        ILogger<FakeMigo> logger = loggerFactory.CreateLogger<FakeMigo>();
        return logger;
    }
}