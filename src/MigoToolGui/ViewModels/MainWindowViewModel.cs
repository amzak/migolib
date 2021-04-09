using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MigoLib.ZOffset;
using MigoToolGui.Domain;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MigoToolGui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        private readonly DateTime _startedAt;
        
        private readonly MigoProxyService _migoProxyService;
        private readonly CancellationTokenSource _cancellationTokenSource;

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

        [Reactive]
        public string GcodeFileName { get; set; }

        public ReactiveCommand<double, Unit> SetZOffsetCommand { get; }
        
        public ReactiveCommand<string, Unit> GCodeFileSelected { get; set; }

        public ReactiveCommand<Unit, Unit> StartPrintCommand { get; set; }

        public ReactiveCommand<Unit, Unit> StopPrintCommand { get; set; }

        public ObservableCollection<TemperaturePoint> NozzleTValues { get; set; }
        
        public ObservableCollection<TemperaturePoint> BedTValues { get; set; }

        private bool _preheatEnabled;

        public bool PreheatEnabled
        {
            get => _preheatEnabled;
            set => this.RaiseAndSetIfChanged(ref _preheatEnabled, value);
        }

        private double _preheatTemperature;

        public double PreheatTemperature
        {
            get => _preheatTemperature;
            set => this.RaiseAndSetIfChanged(ref _preheatTemperature, value);
        }

        public MainWindowViewModel(MigoProxyService migoProxyService)
        {
            _startedAt = DateTime.Now;
            _preheatEnabled = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _migoProxyService = migoProxyService;

            GcodeFileName = string.Empty;
    
            Activator = new ViewModelActivator();
            NozzleTValues = new ObservableCollection<TemperaturePoint>();
            BedTValues = new ObservableCollection<TemperaturePoint>();

            SetZOffsetCommand = ReactiveCommand.CreateFromTask(
                (Func<double, Task>)SetZOffset);
            
            GCodeFileSelected = ReactiveCommand.Create<string>(OnGCodeFileSelected);

            var canStartPrint = this
                .WhenAnyValue(model => model.GcodeFileName)
                .Select(x => !string.IsNullOrEmpty(x))
                .ObserveOn(RxApp.MainThreadScheduler);
            
            StartPrintCommand = ReactiveCommand.CreateFromTask(StartPrint, canStartPrint);
            StopPrintCommand = ReactiveCommand.CreateFromTask(StopPrint);

            this.WhenActivated(OnActivated);
        }

        private Task<ZOffsetModel> Execute(double offset)
        {
            return _migoProxyService.SetZOffset(offset);
        }   

        private async Task StartPrint()
        {
            await _migoProxyService.StartPrint(GcodeFileName);
        }

        private Task StopPrint() 
            => _migoProxyService.StopPrint();

        private Task SetZOffset(double offset) 
            => _migoProxyService.SetZOffset(offset);

        private void OnGCodeFileSelected(string fileName)
        {
            GcodeFileName = fileName;
        }

        private void OnActivated(CompositeDisposable disposable)
        {
            _migoProxyService.GetStateStream(_cancellationTokenSource.Token)
                .ToObservable()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(state =>
                {
                    NozzleT = state.NozzleTemp;
                    BedT = state.BedTemp;
                    var nozzlePoint = new TemperaturePoint(DateTime.Now.Subtract(_startedAt), state.NozzleTemp);
                    var bedPoint = new TemperaturePoint(DateTime.Now.Subtract(_startedAt), state.BedTemp);
                    NozzleTValues.Add(nozzlePoint);
                    BedTValues.Add(bedPoint);
                });

            Observable.StartAsync(_migoProxyService.GetZOffset, RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(model => InitialZOffsetValue(model.ZOffset));

            Disposable.Create(_cancellationTokenSource, source => { source.Cancel(); })
                .DisposeWith(disposable);
        }
        
        private void InitialZOffsetValue(double zOffset) => ZOffset = zOffset;
    }
}
