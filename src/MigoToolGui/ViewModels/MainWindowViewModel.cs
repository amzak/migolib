using System;
using System.Collections.ObjectModel;
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
        public ViewModelActivator Activator { get; }

        public DateTime StartedAt { get; }
        
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
        private CancellationTokenSource _cancellationTokenSource;

        public double ZOffset
        {
            get => _zOffset;
            set => this.RaiseAndSetIfChanged(ref _zOffset, value);
        }
        
        public ReactiveCommand<double, Unit> SetZOffsetCommand { get; }

        public ObservableCollection<TemperaturePoint> NozzleTValues { get; set; }
        public ObservableCollection<TemperaturePoint> BedTValues { get; set; }
        
        public MainWindowViewModel(MigoStateService migoStateService)
        {
            StartedAt = DateTime.Now;

            NozzleTValues = new ObservableCollection<TemperaturePoint>();
            BedTValues = new ObservableCollection<TemperaturePoint>();

            Activator = new ViewModelActivator();
            _cancellationTokenSource = new();

            _migoStateService = migoStateService;
            
            SetZOffsetCommand = ReactiveCommand.Create<double>(SetZOffset);
            
            this.WhenActivated(OnActivated);
        }

        private void OnActivated(CompositeDisposable disposable)
        {
            _migoStateService.GetStateStream(_cancellationTokenSource.Token)
                .ToObservable()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(state =>
                {
                    NozzleT = state.NozzleTemp;
                    BedT = state.BedTemp;
                    var nozzlePoint = new TemperaturePoint(DateTime.Now.Subtract(StartedAt), state.NozzleTemp);
                    var bedPoint = new TemperaturePoint(DateTime.Now.Subtract(StartedAt), state.BedTemp);
                    NozzleTValues.Add(nozzlePoint);
                    BedTValues.Add(bedPoint);
                });

            Observable.StartAsync(_migoStateService.GetZOffset, RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(model => InitialZOffsetValue(model.ZOffset));

            Disposable.Create(_cancellationTokenSource, source => { source.Cancel(); })
                .DisposeWith(disposable);
        }

        private void InitialZOffsetValue(double zOffset) => ZOffset = zOffset;

        private void SetZOffset(double zOffset) => _migoStateService.SetZOffset(zOffset);
    }
}
