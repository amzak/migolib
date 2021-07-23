using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using MigoLib;
using MigoLib.FileUpload;
using MigoToolGui.Domain;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MigoToolGui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IActivatableViewModel
    {
        private const int PointsBufferSize = 60;
        
        private static SolidColorBrush Green = SolidColorBrush.Parse("green");
        private static SolidColorBrush Red = SolidColorBrush.Parse("red");
        private static SolidColorBrush Yellow = SolidColorBrush.Parse("yellow");
            
        public ViewModelActivator Activator { get; }

        private readonly MigoProxyService _migoProxyService;
        private readonly ConfigProvider _configProvider;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        [Reactive]
        public string ConnectionStatus { get; set; }

        [Reactive]
        public SolidColorBrush ConnectionStatusColor { get; set; } 

        [Reactive]
        public double NozzleT { get; set; }

        [Reactive]
        public double BedT { get; set; }

        [Reactive]
        public string GcodeFileName { get; set; }
        
        [Reactive]
        public string State { get; set; }

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
        
        public ZOffsetCalibrationModel ZOffsetCalibration { get; }

        public ExtruderControlViewModel ExtruderControl { get; }
        
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
            ZOffsetCalibration = new ZOffsetCalibrationModel(migoProxyService);
            ExtruderControl = new ExtruderControlViewModel(migoProxyService);

            Endpoints = new ObservableCollection<MigoEndpoint>();
            NozzleTValues = new ObservableCollection<TemperaturePoint>();
            BedTValues = new ObservableCollection<TemperaturePoint>();

            ShowEndpointsDialog = new Interaction<EndpointsDialogViewModel, EndpointsListModel>();
            ShowEndpointsDialogCommand = ReactiveCommand.CreateFromTask(OnShowEndpointsDialog);

            GCodeFileSelected = ReactiveCommand.CreateFromTask(
                (Func<string, Task>)OnGCodeFileSelected);

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
            await _migoProxyService.PreheatAndPrint(GcodeFileName, PreheatEnabled ? PreheatTemperature : default);

            var printerInfo = await _migoProxyService.GetPrinterInfo();
            State = printerInfo.StatedDescription;
        }

        private Task StopPrint() 
            => _migoProxyService.StopPrint();
        
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
            SubscribeOnConnectionStream();
        }

        private void SubscribeOnConnectionStream()
        {
            _migoProxyService.GetConnectionStatusStream(_cancellationTokenSource.Token)
                .ToObservable()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(status =>
                {
                    ConnectionStatus = status.Message;

                    ConnectionStatusColor = status.IsConnected
                        ? Green : status.IsDead ? Red : Yellow;
                }, IgnoreTaskCancelledException);
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

                    if (NozzleTValues.Count > PointsBufferSize)
                    {
                        NozzleTValues.RemoveAt(0);
                        BedTValues.RemoveAt(0);
                    }
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
    }
}
