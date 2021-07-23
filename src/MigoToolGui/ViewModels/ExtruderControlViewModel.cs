using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using MigoToolGui.Domain;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MigoToolGui.ViewModels
{
    public class ExtruderControlViewModel : ViewModelBase, IActivatableViewModel
    {
        private readonly MigoProxyService _service;
        public ViewModelActivator Activator { get; }
        
        [Reactive]
        public double NozzleT { get; set; }
        
        [Reactive]
        public double BedT { get; set; }

        [Reactive]
        public double Amount { get; set; }

        public ReactiveCommand<double, Unit> SetNozzleT { get; set; }
        public ReactiveCommand<double, Unit> SetBedT { get; set; }
        
        public ReactiveCommand<Unit, Unit> ResetNozzleT { get; set; }
        public ReactiveCommand<Unit, Unit> ResetBedT { get; set; }

        public ReactiveCommand<double, Unit> Extrude { get; set; }
        public ReactiveCommand<double, Unit> Retract { get; set; }

        public ExtruderControlViewModel(MigoProxyService service)
        {
            _service = service;
            Activator = new ViewModelActivator();
            
            BedT = 110;
            NozzleT = 235;
            Amount = 100;

            this.WhenActivated(OnActivated);
        }
        
        private void OnActivated(CompositeDisposable disposable)
        {
            SetNozzleT = ReactiveCommand.CreateFromTask((Func<double, Task>)DoSetNozzleT);
            SetBedT = ReactiveCommand.CreateFromTask((Func<double, Task>)DoSetBedT);
            ResetNozzleT = ReactiveCommand.CreateFromTask(DoResetNozzleT);
            ResetBedT = ReactiveCommand.CreateFromTask(DoResetBedT);

            Extrude = ReactiveCommand.CreateFromTask((Func<double, Task>) DoExtrude);
            Retract = ReactiveCommand.CreateFromTask((Func<double, Task>) DoRetract);
        }

        private Task DoExtrude(double amount)
            => _service.ExecuteGCode(new[]
            {
                "G92 E0",
                $"G1 F300 E{amount.ToString("#")}"
            });

        private Task DoRetract(double amount)
            => _service.ExecuteGCode(new[]
            {
                "G92 E0",
                $"G1 F300 E-{amount.ToString("#")}"
            });

        private Task DoSetNozzleT(double temp)
            => _service.ExecuteGCode(new []
            {
                "M106 S255",
                $"M104 S{temp.ToString("#")}"
            });
        
        private Task DoResetNozzleT()
            => _service.ExecuteGCode("M104 S0");
        
        private Task DoSetBedT(double temp)
            => _service.ExecuteGCode($"M140 S{temp.ToString("#")}");
        
        private Task DoResetBedT()
            => _service.ExecuteGCode("M140 S0");        
    }
}