using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using MigoToolGui.Domain;
using ReactiveUI;

namespace MigoToolGui.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly MigoStateService _migoStateService;
        private double _nozzleT;

        public double NozzleT
        {
            get => _nozzleT;
            set => this.RaiseAndSetIfChanged(ref _nozzleT, value);
        }
        
        private double _bedT;

        public double BedT
        {
            get => _bedT;
            set => this.RaiseAndSetIfChanged(ref _bedT, value);
        }
        
        private string _log;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public string Log
        {
            get => _log;
            set => this.RaiseAndSetIfChanged(ref _log, value);
        }

        public ViewModelActivator Activator { get; }

        public MainWindowViewModel(MigoStateService migoStateService)
        {
            Activator = new ViewModelActivator();

            _migoStateService = migoStateService;
            _log = "";

            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => FetchMigoState(_cancellationTokenSource));

            this.WhenActivated(disposable =>
            {
                Disposable
                    .Create(() => { })
                    .DisposeWith(disposable);
            });
        }

        private async Task FetchMigoState(CancellationTokenSource cancellationTokenSource)
        {
            await foreach (var stateModel in _migoStateService.GetStateStream(cancellationTokenSource.Token))
            {
                NozzleT = stateModel.NozzleTemp;
                BedT = stateModel.BedTemp;
            }
        }
    }
}
