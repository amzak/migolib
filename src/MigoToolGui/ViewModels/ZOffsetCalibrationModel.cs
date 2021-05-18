using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MigoToolGui.Domain;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MigoToolGui.ViewModels
{
    public class ZOffsetCalibrationModel : ViewModelBase, IActivatableViewModel
    {
        private readonly MigoProxyService _migoProxyService;
        public ViewModelActivator Activator { get; }

        [Reactive]
        public double ZOffset { get; set; }

        public ReactiveCommand<double, Unit> SetZOffsetCommand { get; set; }

        public ZOffsetCalibrationModel(MigoProxyService migoProxyService)
        {
            Activator = new ViewModelActivator();
            
            _migoProxyService = migoProxyService;
            
            SetZOffsetCommand = ReactiveCommand.CreateFromTask(
                (Func<double, Task>)SetZOffset);

            this.WhenActivated(OnActivated);
        }

        private void OnActivated(CompositeDisposable disposable)
        {
            Observable.StartAsync(_migoProxyService.GetZOffset, RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(model => InitialZOffsetValue(model.ZOffset));
        }

        private Task SetZOffset(double offset) 
            => _migoProxyService.SetZOffset(offset);

        private void InitialZOffsetValue(double zOffset) => ZOffset = zOffset;
    }
}