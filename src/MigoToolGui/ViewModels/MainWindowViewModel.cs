using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
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
        
        private double _zOffset;

        public double ZOffset
        {
            get => _zOffset;
            set => this.RaiseAndSetIfChanged(ref _zOffset, value);
        }
        
        public ViewModelActivator Activator { get; }

        public ReactiveCommand<double, Unit> SetZOffsetCommand { get; }

        public MainWindowViewModel(MigoStateService migoStateService)
        {
            Activator = new ViewModelActivator();

            _migoStateService = migoStateService;

            CancellationTokenSource cancellationTokenSource = new();

            SetZOffsetCommand = ReactiveCommand.Create<double>(SetZOffset);

            this.WhenActivated(disposable =>
            {
                _migoStateService.GetStateStream(cancellationTokenSource.Token)
                    .ToObservable()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(state =>
                    {
                        NozzleT = state.NozzleTemp;
                        BedT = state.BedTemp;
                    });
                
                Observable
                    .StartAsync(_migoStateService.GetZOffset, RxApp.TaskpoolScheduler)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(model => InitialZOffsetValue(model.ZOffset));

                Disposable
                    .Create(cancellationTokenSource, source =>
                    {
                        source.Cancel();
                    })
                    .DisposeWith(disposable);
            });
        }
        
        private void InitialZOffsetValue(double zOffset) => ZOffset = zOffset;

        private void SetZOffset(double zOffset)
        {
            _migoStateService.SetZOffset(zOffset);
        }
    }
}
