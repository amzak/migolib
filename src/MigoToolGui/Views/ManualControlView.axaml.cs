using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using MigoToolGui.ViewModels;

namespace MigoToolGui.Views
{
    public class ManualControlView : ReactiveUserControl<ManualControlViewModel>
    {
        public ManualControlView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}