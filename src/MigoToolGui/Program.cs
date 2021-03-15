using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Logging;
using MigoLib.Tests;
using MigoToolGui.Bootstrap;
using MigoToolGui.Domain;
using MigoToolGui.ViewModels;
using ReactiveUI;
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
                .MinimumLevel.Debug()
                .CreateLogger();

            var container = new StashboxContainer();
            var loggerFactory = new SerilogLoggerFactory(logger);
            var fakeMigoLogger = loggerFactory.CreateLogger<FakeMigo>();

            var cancellationTokenSource = new CancellationTokenSource();
            var fakeMigo = new FakeMigo("127.0.0.1", 10086, fakeMigoLogger);
            fakeMigo.ReplyRealStream(cancellationTokenSource.Token);
            
            fakeMigo.Start();
            
            SetupDependencies(container, logger);

            var resolver = new StashboxDependencyResolver(container);
            Locator.SetLocator(resolver);
            
            Locator.CurrentMutable.InitializeSplat();
            Locator.CurrentMutable.InitializeReactiveUI();
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());
            
            BuildAvaloniaApp(container)
                .StartWithClassicDesktopLifetime(args);
            
            fakeMigo.Stop();
        }

        public static AppBuilder BuildAvaloniaApp(IStashboxContainer container)
        {
            var app = AppBuilder.Configure<App>()
                .UseReactiveUI()
                .UsePlatformDetect()
                .LogToTrace();
            
            return app;
        }
        
        public static void SetupDependencies(IStashboxContainer container, ILogger logger)
        {
            container.RegisterInstance(logger);
            container.Register<ILoggerFactory, SerilogLoggerFactory>();
            container.RegisterSingleton<ConfigProvider>();
            container.RegisterSingleton<MigoStateService>();

            container.Register<MainWindowViewModel>();
            //container.RegisterSingleton<IViewFor<MainWindowViewModel>, MainWindow>();
        }
    }

}
