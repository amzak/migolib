using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using MigoToolGui.ViewModels;

namespace MigoToolGui.Views
{
    public class ZOffsetCalibrationView : ReactiveUserControl<ZOffsetCalibrationModel>
    {
        public ZOffsetCalibrationView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}