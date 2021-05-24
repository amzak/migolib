using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using MigoToolGui.Domain;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MigoToolGui.ViewModels
{
    public class ManualControlViewModel : ViewModelBase, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        private readonly MigoProxyService _service;

        [Reactive]
        public double StepSize { get; set; }
        
        public double NegStepSize => -StepSize;

        [Reactive]
        public double NozzleT { get; set; }
        
        [Reactive]
        public double BedT { get; set; }

        public ReactiveCommand<Unit, Unit> HomeXY { get; set; }
        public ReactiveCommand<Unit, Unit> HomeZ { get; set; }
        
        public ReactiveCommand<double, Unit> MoveX { get; set; }
        public ReactiveCommand<double, Unit> MoveY { get; set; }
        public ReactiveCommand<double, Unit> MoveZ { get; set; }
        
        public ReactiveCommand<double, Unit> SetNozzleT { get; set; }
        public ReactiveCommand<double, Unit> SetBedT { get; set; }
        
        public ReactiveCommand<Unit, Unit> ResetNozzleT { get; set; }
        public ReactiveCommand<Unit, Unit> ResetBedT { get; set; }

        public ManualControlViewModel(MigoProxyService service)
        {
            _service = service;
            Activator = new ViewModelActivator();
            
            StepSize = 5;
            BedT = 110;
            NozzleT = 235;
            
            this.WhenActivated(OnActivated);
        }

        private void OnActivated(CompositeDisposable disposable)
        {
            HomeXY = ReactiveCommand.CreateFromTask(DoHomeXY);
            HomeZ = ReactiveCommand.CreateFromTask(DoHomeZ);

            MoveX = ReactiveCommand.CreateFromTask((Func<double, Task>)DoMoveX);
            MoveY = ReactiveCommand.CreateFromTask((Func<double, Task>)DoMoveY);
            MoveZ = ReactiveCommand.CreateFromTask((Func<double, Task>)DoMoveZ);
            
            SetNozzleT = ReactiveCommand.CreateFromTask((Func<double, Task>)DoSetNozzleT);
            SetBedT = ReactiveCommand.CreateFromTask((Func<double, Task>)DoSetBedT);
            ResetNozzleT = ReactiveCommand.CreateFromTask(DoResetNozzleT);
            ResetBedT = ReactiveCommand.CreateFromTask(DoResetBedT);
        }

        private Task DoHomeXY()
        {
            var lines = new[] {"G28 X0 Y0"};
            return _service.ExecuteGCode(lines);
        }

        private Task DoHomeZ() 
            => _service.ExecuteGCode(new[] {"G28 Z0"});

        private Task DoMoveX(double delta)
            => DoMove("X", delta);

        private Task DoMove(string axis, double delta)
        {
            var lines = new []
            {
                $"G92 {axis}0",
                $"G0 F200 {axis}{delta.ToString("#.###")}"
            };
            return _service.ExecuteGCode(lines);
        }

        private Task DoMoveY(double delta)
            => DoMove("Y", delta);
        
        private Task DoMoveZ(double delta)
            => DoMove("Z", delta);
        
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