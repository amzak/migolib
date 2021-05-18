using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MigoLib;
using MigoLib.FileUpload;
using MigoToolGui.Domain;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MigoToolGui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        private readonly MigoProxyService _migoProxyService;
        private readonly ConfigProvider _configProvider;
        private readonly CancellationTokenSource _cancellationTokenSource;

        [Reactive]
        public double NozzleT { get; set; }

        [Reactive]
        public double BedT { get; set; }

        [Reactive]
        public double ZOffset { get; set; }
        
        [Reactive]
        public string GcodeFileName { get; set; }
        
        [Reactive]
        public string State { get; set; }

        public ReactiveCommand<double, Unit> SetZOffsetCommand { get; set; }
        
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
        
        public ReactiveCommand<Unit, Unit> ShowEndpointsDialogCommand { get; set;}
        
        public ManualControlViewModel ManualControl { get; }
        
        public MainWindowViewModel(MigoProxyService migoProxyService, ConfigProvider configProvider)
        {
            Activator = new ViewModelActivator();

            _cancellationTokenSource = new CancellationTokenSource();
            _migoProxyService = migoProxyService;
            _configProvider = configProvider;
            
            PreheatEnabled = true;
            PreheatTemperature = 100;
            GcodeFileName = string.Empty;
            State = "Idle";

            ManualControl = new ManualControlViewModel(migoProxyService);

            Endpoints = new ObservableCollection<MigoEndpoint>();
            NozzleTValues = new ObservableCollection<TemperaturePoint>();
            BedTValues = new ObservableCollection<TemperaturePoint>();

            ShowEndpointsDialog = new Interaction<EndpointsDialogViewModel, EndpointsListModel>();
            ShowEndpointsDialogCommand = ReactiveCommand.CreateFromTask(OnShowEndpointsDialog);

            GCodeFileSelected = ReactiveCommand.CreateFromTask(
                (Func<string, Task>)OnGCodeFileSelected);

            SetZOffsetCommand = ReactiveCommand.CreateFromTask(
                (Func<double, Task>)SetZOffset);
            
            var canStartPrint = this
                .WhenAnyValue(model => model.GcodeFileName)
                .Select(x => !string.IsNullOrEmpty(x))
                .ObserveOn(RxApp.MainThreadScheduler);
            
            StartPrintCommand = ReactiveCommand.CreateFromTask(StartPrint, canStartPrint);
            StopPrintCommand = ReactiveCommand.CreateFromTask(StopPrint);

            this.WhenActivated(OnActivated);
        }

        private void OnSelectedEndpointChanged(MigoEndpoint endpoint)
        {
            if (!endpoint.IsValid())
            {
                return;
            }
            _migoProxyService.SwitchTo(endpoint);
            
            StartupObservables();
        }

        private async Task<Unit> OnShowEndpointsDialog()
        {
            var sourceConfig = await _configProvider.GetConfig().ConfigureAwait(false);
            var modelIn = new EndpointsDialogViewModel(sourceConfig.Endpoints);
            var modelOut = await ShowEndpointsDialog.Handle(modelIn);

            if (modelOut == null)
            {
                return Unit.Default;
            }
            
            PopulateEndpoints(modelOut.Endpoints);

            var config = new Config(SelectedEndpoint, modelOut.Endpoints);
            
            await _configProvider.SaveConfig(config).ConfigureAwait(false);
            
            return Unit.Default;
        }

        private void PopulateEndpoints(IReadOnlyCollection<MigoEndpoint> endpoints)
        {
            var selected = SelectedEndpoint;
            
            Endpoints.Clear();

            if (selected.IsValid())
            {
                Endpoints.Add(selected);
            }
            
            foreach (var endpoint in endpoints)
            {
                if(endpoint.Equals(selected))
                {
                    continue;
                }
                Endpoints.Add(endpoint);
            }
            
            SelectedEndpoint = selected;
        }

        private async Task StartPrint()
        {
            State = "Uploading...";
            await _migoProxyService.UploadGcode(GcodeFileName);
            State = "Preparing for new print...";
            await _migoProxyService.PreheatAndPrint(GcodeFileName, PreheatTemperature);

            var printerInfo = await _migoProxyService.GetPrinterInfo();
            State = printerInfo.StatedDescription;
        }

        private Task StopPrint() 
            => _migoProxyService.StopPrint();

        private Task SetZOffset(double offset) 
            => _migoProxyService.SetZOffset(offset);

        private async Task OnGCodeFileSelected(string fileName)
        {
            GcodeFileName = fileName;
        }

        private void OnActivated(CompositeDisposable disposable)
        {
            Disposable.Create(_cancellationTokenSource, source => { source.Cancel(); })
                .DisposeWith(disposable);

            Observable.StartAsync(_configProvider.GetConfig, RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(InitConfig);

            this.WhenAnyValue(model => model.SelectedEndpoint)
                .Subscribe(OnSelectedEndpointChanged); 
        }

        private void InitConfig(Config config)
        {
            PopulateEndpoints(config.Endpoints);
            SelectedEndpoint = config.SelectedEndpoint;
        }

        private void StartupObservables()
        {
            SubscribeOnStateStream();
            SubscribeOnProgressStream();

            Observable.StartAsync(_migoProxyService.GetZOffset, RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(model => InitialZOffsetValue(model.ZOffset));
        }

        private void SubscribeOnProgressStream()
        {
            _migoProxyService.GetProgressStream(_cancellationTokenSource.Token)
                .ToObservable()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnProgressReport, IgnoreTaskCancelledException);
        }

        private void OnProgressReport(FilePercentResult percent)
        {
            ProgressStatus = percent.Percent;
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
                    var nozzlePoint = new TemperaturePoint(DateTime.Now.TimeOfDay, state.NozzleTemp);
                    var bedPoint = new TemperaturePoint(DateTime.Now.TimeOfDay, state.BedTemp);
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
