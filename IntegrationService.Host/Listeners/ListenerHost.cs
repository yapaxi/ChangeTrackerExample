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
        private readonly Dictionary<string, IDisposable> _subscriptions;
        private readonly object _lock;

        private bool _disposed;

        public ListenerHost(IBus bus)
        {
            _lock = new object();
            _subscriptions = new Dictionary<string, IDisposable>();
            _bus = bus;
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

                IDisposable currentSubscription;
                if (_subscriptions.TryGetValue(entityName, out currentSubscription))
                {
                    currentSubscription.Dispose();
                    _subscriptions.Remove(entityName);
                }
            }
        }

        public void Accept(string entityName, string queue, Action<RawMessage> callback)
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
                    throw new InvalidOperationException($"Failed to create subscription for {entityName}, because there is existing one");
                }

                Console.WriteLine($"Creating new subscription for {entityName}");
                
                var subscription = _bus.Advanced.Consume(
                    new Queue(queue, false),
                    (data, properties, info) => callback(new RawMessage()
                    {
                        Body = data,
                        EntityId = (int)properties.Headers[ISMessageHeader.SCHEMA_ENTITY_ID]
                    })
                );
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
