using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using IntegrationService.Host.DAL;
using EasyNetQ;
using EasyNetQ.Topology;
using IntegrationService.Host.DAL.Contexts;
using IntegrationService.Host.Converters;
using System.Threading;
using RabbitModel;

namespace IntegrationService.Host.Listeners
{
    public partial class ListenerHost : IDisposable
    {
        private readonly IBus _bus;
        private readonly ILifetimeScope _scope;
        private readonly Dictionary<string, Subscription> _subscriptions;
        private readonly object _lock;

        private bool _disposed;

        public ListenerHost(ILifetimeScope scope)
        {
            _scope = scope;
            _lock = new object();
            _subscriptions = new Dictionary<string, Subscription>();
            _bus = _scope.ResolveNamed<IBus>(Buses.Messaging);
        }

        public void Reject(string entityName)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ListenerHost));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                Subscription currentSubscription;
                if (_subscriptions.TryGetValue(entityName, out currentSubscription))
                {
                    currentSubscription.Dispose();
                    _subscriptions.Remove(entityName);
                }
            }
        }

        public void Accept(string entityName, string queue, RuntimeMappingSchema runtimeSchema, IStagingTable stagingTable)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ListenerHost));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                
                if (_subscriptions.ContainsKey(entityName))
                {
                    throw new InvalidOperationException($"Failed to create subscription for {entityName} -> {stagingTable.FullName}, because there is existing one");
                }

                Console.WriteLine($"Creating new subscription for {entityName} -> {stagingTable.FullName}:{runtimeSchema.Schema.Checksum}");
                
                var converter = new FlatMessageConverter(runtimeSchema);
                var subscription = new Subscription(this, queue, converter, stagingTable);

                _subscriptions.Add(entityName, subscription);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var s in _subscriptions)
                {
                    s.Value.Dispose();
                }

                _disposed = true;
            }
        }


    }
}
