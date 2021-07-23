using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using MigoToolGui.ViewModels;

namespace MigoToolGui.Views
{
    public class ExtruderControlView : ReactiveUserControl<ExtruderControlViewModel>
    {
        public ExtruderControlView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}