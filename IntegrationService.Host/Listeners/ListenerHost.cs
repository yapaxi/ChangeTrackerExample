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

namespace IntegrationService.Host.Listeners
{
    public class ListenerHost : IDisposable
    {
        private readonly IBus _bus;
        private readonly ILifetimeScope _scope;
        private Dictionary<string, IDisposable> _subscriptions;
        private readonly object _lock;

        private bool _disposed;

        public ListenerHost(ILifetimeScope scope)
        {
            _scope = scope;
            _lock = new object();
            _subscriptions = new Dictionary<string, IDisposable>();
            _bus = _scope.ResolveNamed<IBus>(Buses.Messaging);
        }

        public void Accept(string queue, MappingProperty[] schemaProperties, StagingTable stagingTable)
        {
            if (_disposed)
            {
                return;
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                IDisposable currentSubscription;
                if (_subscriptions.TryGetValue(queue, out currentSubscription))
                {
                    Console.WriteLine($"Closing existing subscription for queue {queue}");
                    currentSubscription.Dispose();
                }

                Console.WriteLine($"Creating new subscription for {queue} -> {stagingTable.Name}");
                var handler = new RawMessageHandler(schemaProperties, stagingTable);
                var subscr = _bus.Advanced.Consume(new Queue(queue, false), (data, properties, info) => handler.Handle(data, properties, info));
                _subscriptions[queue] = subscr;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var s in _subscriptions)
                {
                    Console.WriteLine($"Closing existing subscription for queue {s.Key}");
                    s.Value.Dispose();
                }

                _disposed = true;
            }
        }

    }
}
