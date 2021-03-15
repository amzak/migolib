using System;
using System.Collections.Generic;
using Stashbox;
using IDependencyResolver = Splat.IDependencyResolver;

namespace MigoToolGui
{
    public class StashboxDependencyResolver : IDependencyResolver
    {
        private readonly IStashboxContainer _container;

        public StashboxDependencyResolver(IStashboxContainer container)
        {
            _container = container;
        }
        
        public bool HasRegistration(Type serviceType, string contract = null) 
            => _container.IsRegistered(serviceType, contract);

        public void Register(Func<object> factory, Type serviceType, string contract = null)
        {
            _container.Register(serviceType, configurator =>
                {
                    if (string.IsNullOrEmpty(contract))
                    {
                        configurator
                            .WithFactory(factory)
                            .ReplaceExisting();
                    }
                    else
                    {
                        configurator
                            .WithFactory(factory)
                            .WithName(contract)
                            .ReplaceExisting();
                    }
                });
        }

        public void UnregisterCurrent(Type serviceType, string contract = null) 
            => throw new NotImplementedException();

        public void UnregisterAll(Type serviceType, string contract = null)
            => throw new NotImplementedException();
    
        public IDisposable ServiceRegistrationCallback(Type serviceType, string contract, Action<IDisposable> callback) 
            => throw new NotImplementedException();

        public object GetService(Type serviceType, string contract = null) 
            => contract == null 
                ? _container.Resolve(serviceType)
                : _container.Resolve(serviceType, contract);

        public IEnumerable<object> GetServices(Type serviceType, string contract = null)
        {
            var services = _container.ResolveAll(serviceType);
            return services;
        }

        public void Dispose()
        {
            _container?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}