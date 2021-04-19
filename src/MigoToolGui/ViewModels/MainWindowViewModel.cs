using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MigoLib;
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

        [Reactive]
        public double NozzleT { get; set; }

        [Reactive]
        public double BedT { get; set; }

        [Reactive]
        public double ZOffset { get; set; }
        
        [Reactive]
        public string GcodeFileName { get; set; }

        public ReactiveCommand<double, Unit> SetZOffsetCommand { get; }
        
        public ReactiveCommand<string, Unit> GCodeFileSelected { get; set; }

        public ReactiveCommand<Unit, Unit> StartPrintCommand { get; set; }

        public ReactiveCommand<Unit, Unit> StopPrintCommand { get; set; }

        public ObservableCollection<TemperaturePoint> NozzleTValues { get; set; }
        
        public ObservableCollection<TemperaturePoint> BedTValues { get; set; }

        [Reactive]
        public bool PreheatEnabled { get; set; }
        
        [Reactive]
        public double PreheatTemperature { get; set; }

        [Reactive]
        public byte ProgressStatus { get; set; }

        public ObservableCollection<MigoEndpoint> Endpoints { get; set; }
        
        [Reactive]
        public MigoEndpoint SelectedEndpoint { get; set; }

        public Interaction<EndpointsDialogViewModel, EndpointsListModel> ShowEndpointsDialog { get; }
        
        public ReactiveCommand<Unit, Unit> ShowEndpointsDialogCommand { get; }
        
        public MainWindowViewModel(MigoProxyService migoProxyService)
        {
            _startedAt = DateTime.Now;
            _cancellationTokenSource = new CancellationTokenSource();
            _migoProxyService = migoProxyService;

            Endpoints = new ObservableCollection<MigoEndpoint>();

            PreheatEnabled = true;
            PreheatTemperature = 100;
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

            ShowEndpointsDialog = new Interaction<EndpointsDialogViewModel, EndpointsListModel>();
            ShowEndpointsDialogCommand = ReactiveCommand.CreateFromTask(OnShowEndpointsDialog);

            this.WhenAnyValue(model => model.SelectedEndpoint)
                .Subscribe(OnSelectedEndpointChanged);
            
            this.WhenActivated(OnActivated);
        }

        private void OnSelectedEndpointChanged(MigoEndpoint endpoint)
        {
            if (!endpoint.IsValid())
            {
                return;
            }
            _migoProxyService.SwitchTo(endpoint);
            
            SubscribeOnStateStream();
            SubscribeOnProgressStream();
        }

        private async Task<Unit> OnShowEndpointsDialog()
        {
            var modelIn = new EndpointsDialogViewModel();
            var modelOut = await ShowEndpointsDialog.Handle(modelIn);

            Endpoints.Clear();
            foreach (var endpoint in modelOut.Endpoints)
            {
                Endpoints.Add(endpoint);
            }
            
            return Unit.Default;
        }

        private async Task StartPrint() 
            => await _migoProxyService.PreheatAndPrint(GcodeFileName, PreheatTemperature);

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
            SubscribeOnStateStream();
            SubscribeOnProgressStream();

            Observable.StartAsync(_migoProxyService.GetZOffset, RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(model => InitialZOffsetValue(model.ZOffset));

            Disposable.Create(_cancellationTokenSource, source => { source.Cancel(); })
                .DisposeWith(disposable);
        }

        private void SubscribeOnProgressStream()
        {
            _migoProxyService.GetProgressStream(_cancellationTokenSource.Token)
                .ToObservable()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(percent => { ProgressStatus = percent.Percent; }, IgnoreTaskCancelledException);
        }

        private void SubscribeOnStateStream()
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
                }, IgnoreTaskCancelledException);
        }

        private void IgnoreTaskCancelledException(Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                return;
            }

            throw ex;
        }

        private void InitialZOffsetValue(double zOffset) => ZOffset = zOffset;
    }
}
