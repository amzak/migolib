﻿using System.Threading;
using Avalonia;
using Avalonia.Logging;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Logging;
using MigoLib.Fake;
using MigoToolGui.Domain;
using MigoToolGui.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Formatters;
using ReactiveUI.Validation.Formatters.Abstractions;
using Serilog;
using Serilog.Extensions.Logging;
using Splat;
using Stashbox;
using ILogger = Serilog.ILogger;

namespace MigoToolGui
{
    class Program
    {
        public static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .Console()
                .MinimumLevel.Verbose()
                .CreateLogger();

            var container = new StashboxContainer();
            var loggerFactory = new SerilogLoggerFactory(logger);
            var fakeMigoLogger = loggerFactory.CreateLogger<FakeMigo>();

            var cancellationTokenSource = new CancellationTokenSource();
            var fakeMigo = new FakeMigo("127.0.0.1", 10086, fakeMigoLogger);
            var fakeMigo2 = new FakeMigo("127.0.0.1", 10087, fakeMigoLogger);
            
            fakeMigo.ReplyRealStream(cancellationTokenSource.Token);
            fakeMigo2.ReplyRealStream(cancellationTokenSource.Token);
            
            fakeMigo.Start();
            fakeMigo2.Start();
            
            var resolver = new StashboxDependencyResolver(container);
            Locator.SetLocator(resolver);
            SetupDependencies(container, logger);
            
            Locator.CurrentMutable.InitializeSplat();
            Locator.CurrentMutable.InitializeReactiveUI();
            
            BuildAvaloniaApp(container)
                .StartWithClassicDesktopLifetime(args);

            fakeMigo.Stop();
            fakeMigo2.Stop();
        }

        private static AppBuilder BuildAvaloniaApp(StashboxContainer container)
        {
            var app = AppBuilder.Configure(() => new App(container))
                .UsePlatformDetect()
                .LogToTrace(level: LogEventLevel.Debug)
                .UseReactiveUI();
            
            return app;
        }

        public static void SetupDependencies(IStashboxContainer container, ILogger logger)
        {
            container.RegisterInstance(logger);
            container.Register<ILoggerFactory, SerilogLoggerFactory>();
            container.Register(typeof(IValidationTextFormatter<>), typeof(SingleLineFormatter));
            
            container.RegisterSingleton<ConfigProvider>();
            container.RegisterSingleton<MigoProxyService>();

            container.Register<MainWindowViewModel>();
        }
    }
}
