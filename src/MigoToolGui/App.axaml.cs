using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MigoToolGui.ViewModels;
using MigoToolGui.Views;
using Stashbox;

namespace MigoToolGui
{
    public class App : Application
    {
        private readonly IStashboxContainer _container;

        public App()
        {
            
        }
        
        public App(IStashboxContainer container)
        {
            _container = container;
        }
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = _container.Resolve<MainWindowViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}