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

namespace IntegrationService.Host.Listeners
{
    public class ListenerHost : IDisposable
    {
        private readonly IBus _bus;
        private readonly ILifetimeScope _scope;
        private Dictionary<string, Subscription> _subscriptions;
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
                return;
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

        public void Accept(string entityName, string queue, MappingSchema schema, StagingTable stagingTable)
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
                
                if (_subscriptions.ContainsKey(entityName))
                {
                    throw new InvalidOperationException($"Failed to create subscription for {entityName} -> {stagingTable.Name}, because there is existing one");
                }

                Console.WriteLine($"Creating new subscription for {entityName} -> {stagingTable.Name}:{schema.Checksum}");
                
                var converter = new FlatMessageConverter(schema);
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


        private class Subscription : IDisposable
        {
            private int _disposed;

            private readonly IDisposable _consumerSubscription;
            private readonly FlatMessageConverter _converter;
            private readonly StagingTable _stagingTable;
            private readonly ListenerHost _host;
            private readonly string _queue;

            public Subscription(ListenerHost host, string queue, FlatMessageConverter converter, StagingTable stagingTable)
            {
                _host = host;
                _stagingTable = stagingTable;
                _converter = converter;
                _disposed = 0;
                _queue = queue;
                _consumerSubscription = _host._bus.Advanced.Consume(
                    new Queue(queue, false),
                    (data, properties, info) => ConsumerRoutine(data, properties, info)
                );
            }

            private void ConsumerRoutine(byte[] data, MessageProperties properties, MessageReceivedInfo info)
            {
                if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
                {
                    return;
                }

                try
                {
                    var parameters = _converter.Convert(data, properties, info);
                    using (var dataInsertionScope = _host._scope.BeginLifetimeScope())
                    {
                        Console.WriteLine($"Inserting data. Version={_converter.Schema.Checksum}");
                        dataInsertionScope.Resolve<DataRepository>().Insert(_stagingTable.Name, parameters);
                        Console.WriteLine($"Inserted data. Version={_converter.Schema.Checksum}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }

            public void Dispose()
            {
                Console.WriteLine($"Closing {this}");
                Interlocked.Exchange(ref _disposed, 1);
                _consumerSubscription.Dispose();
            }

            public override string ToString()
            {
                return $"Subscription for queue {_queue} to table {_stagingTable.Name} for schema version {_converter.Schema.Checksum}";
            }
        }

    }
}
