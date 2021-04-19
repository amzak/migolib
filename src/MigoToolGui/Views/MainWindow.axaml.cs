using System.Threading.Tasks;
using Avalonia;
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
            this.WhenActivated(d 
                => d(ViewModel
                    .ShowEndpointsDialog
                    .RegisterHandler(DoShowDialogAsync)));
        }
        
        private async Task DoShowDialogAsync(InteractionContext<EndpointsDialogViewModel, EndpointsListModel> interaction)
        {
            var dialog = new EndpointsDialog
            {
                DataContext = interaction.Input
            };

            var result = await dialog.ShowDialog<EndpointsListModel>(this);
            interaction.SetOutput(result);
        }
    }
}