using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using MigoToolGui.ViewModels;
using ReactiveUI;
using System;

namespace MigoToolGui.Views
{
    public class EndpointsDialog : ReactiveWindow<EndpointsDialogViewModel>
    {
        public EndpointsDialog()
        {
            AvaloniaXamlLoader.Load(this);
            
#if DEBUG
            this.AttachDevTools();
#endif
            
            this.WhenActivated(
                d 
                    => d(ViewModel
                        .ReturnEndpoints
                        .Subscribe(Close)));
        }
    }
}