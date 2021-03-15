using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using MigoToolGui.ViewModels;
using ReactiveUI;

namespace MigoToolGui.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            
#if DEBUG
            this.AttachDevTools();
#endif
        }
    }
}