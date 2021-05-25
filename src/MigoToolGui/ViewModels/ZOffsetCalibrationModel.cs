using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MigoLib.Scenario;
using MigoToolGui.Domain;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MigoToolGui.ViewModels
{
    public class ZOffsetCalibrationModel : ViewModelBase, IActivatableViewModel
    {
        private readonly MigoProxyService _migoProxyService;
        
        private CancellationTokenSource? _cts;
        private IAsyncEnumerator<BedLevelingCalibrationResult>? _iterator;

        public ViewModelActivator Activator { get; }

        [Reactive]
        public double ZOffset { get; set; }
        
        [Reactive]
        public BedLevelingCalibrationMode CalibrationMode { get; set; }
        
        [Reactive]
        public bool CanContinue { get; set; }
        
        public ReadOnlyObservableCollection<BedLevelingCalibrationMode> CalibrationModes { get; set; } 

        public ReactiveCommand<double, Unit> SetZOffsetCommand { get; set; }
        
        public ReactiveCommand<Unit, Unit> MoveToZOffsetCommand { get; set; }
        
        public ReactiveCommand<Unit, Unit> StartCalibrationCommand { get; set; }

        public ReactiveCommand<Unit, Unit> CalibrateNextCommand { get; set; }
        
        public ReactiveCommand<Unit, Unit> StopCalibrationCommand { get; set; }

        public ZOffsetCalibrationModel(MigoProxyService migoProxyService)
        {
            Activator = new ViewModelActivator();
            
            _migoProxyService = migoProxyService;

            var modes = new ObservableCollection<BedLevelingCalibrationMode>();

            foreach (var value in Enum.GetValues<BedLevelingCalibrationMode>())
            {
                modes.Add(value);
            }
            
            CalibrationModes =
                new ReadOnlyObservableCollection<BedLevelingCalibrationMode>(modes);

            CalibrationMode = BedLevelingCalibrationMode.FivePoints;
            
            SetZOffsetCommand = ReactiveCommand.CreateFromTask(
                (Func<double, Task>)SetZOffset);

            MoveToZOffsetCommand = ReactiveCommand.CreateFromTask(MoveToZOffset);

            StopCalibrationCommand = ReactiveCommand.CreateFromTask(StopCalibration);
                
            this.WhenActivated(OnActivated);
        }

        private void OnActivated(CompositeDisposable disposable)
        {
            Observable.StartAsync(_migoProxyService.GetZOffset, RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(model => InitialZOffsetValue(model.ZOffset));

            var canContinue = this.WhenAnyValue(x => x.CanContinue)
                .Select(x => x)
                .ObserveOn(RxApp.MainThreadScheduler);

            StartCalibrationCommand = ReactiveCommand.CreateFromTask(StartCalibration);
            CalibrateNextCommand = ReactiveCommand.CreateFromTask(CalibrateNext, canContinue);
        }

        private Task SetZOffset(double offset) 
            => _migoProxyService.SetZOffset(offset);

        private Task MoveToZOffset()
            => _migoProxyService.MoveToZOffset(ZOffset);

        private void InitialZOffsetValue(double zOffset) => ZOffset = zOffset;
        
        private async Task StartCalibration()
        {
            _cts = new CancellationTokenSource();
            _iterator = _migoProxyService.StartZOfsetCalibration(_cts.Token).GetAsyncEnumerator();

            CanContinue = await _iterator.MoveNextAsync().ConfigureAwait(false);
        }

        private async Task CalibrateNext()
        {
            if (_iterator != null)
            {
                CanContinue = await _iterator.MoveNextAsync().ConfigureAwait(false);
            }
        }

        private Task StopCalibration()
        {
            _cts?.Cancel();
            return CalibrateNext();
        }
    }
}