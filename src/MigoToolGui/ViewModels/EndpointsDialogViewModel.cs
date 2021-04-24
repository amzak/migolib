using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using MigoLib;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;

namespace MigoToolGui.ViewModels
{
    public class EndpointsDialogViewModel : ViewModelBase
    {
        private SourceList<string> ConnectionsList { get; set; }

        private readonly ReadOnlyObservableCollection<string> _connections;
        public ReadOnlyObservableCollection<string> Connections => _connections;

        [Reactive]
        public string NewConnection { get; set; }
        
        [Reactive]
        public string SelectedConnection { get; set; }

        public ReactiveCommand<string, Unit> AddConnection { get; }

        public ReactiveCommand<string, Unit> RemoveConnection { get; }
        
        public ReactiveCommand<Unit, EndpointsListModel> ReturnEndpoints { get; }
        
        public EndpointsDialogViewModel(IReadOnlyCollection<MigoEndpoint> endpoints)
        {
            this.ValidationRule(
                x => x.NewConnection,
                MigoEndpoint.IsValid,
                "Invalid endpoint format, must be like 'ip:port'");

            ConnectionsList = new SourceList<string>();
            ConnectionsList
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _connections)
                .DisposeMany()
                .Subscribe();

            foreach (var endpoint in endpoints)
            {
                ConnectionsList.Add(endpoint.ToString());
            }

            var canAddConnection = this
                .IsValid()
                .ObserveOn(RxApp.MainThreadScheduler);

            var canRemoveConnection = this
                .WhenAnyValue(model => model.SelectedConnection)
                .Select(value => !string.IsNullOrEmpty(value))
                .ObserveOn(RxApp.MainThreadScheduler);

            AddConnection = ReactiveCommand.Create((Action<string>)OnAddConnection, canAddConnection);
            RemoveConnection = ReactiveCommand.Create((Action<string>)OnRemoveConnection, canRemoveConnection);

            ReturnEndpoints = ReactiveCommand.Create(OnReturnConnections);
        }

        private EndpointsListModel OnReturnConnections()
        {
            var connections = _connections
                .Select(item => new MigoEndpoint(item));

            var model = new EndpointsListModel
            {
                Endpoints = connections.ToList()
            };
            
            return model;
        }

        private void OnAddConnection(string connection)
        {
            ConnectionsList.Add(connection);
            NewConnection = "";        
        }

        private void OnRemoveConnection(string connection) 
            => ConnectionsList.Remove(connection);
    }
}