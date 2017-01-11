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
using IntegrationService.Host.Listeners.Metadata;

namespace IntegrationService.Host.Listeners.Data
{
    public partial class DataListenerHost : ListenerBase, IDisposable
    {
        private readonly IBus _simpleBus;
        private readonly IBus _bulkBus;
        private readonly Dictionary<DataMode, Dictionary<string, IDisposable>> _subscriptions;
        private readonly object _lock;

        private bool _disposed;

        public DataListenerHost(ILifetimeScope scope)
            : base(scope)
        {
            _lock = new object();
            _subscriptions = new Dictionary<DataMode, Dictionary<string, IDisposable>>()
            {
                { DataMode.RowByRow, new Dictionary<string, IDisposable>() },
                { DataMode.Bulk, new Dictionary<string, IDisposable>() },
            };
            _simpleBus = scope.ResolveNamed<IBus>(Buses.SimpleMessaging);
            _bulkBus = scope.ResolveNamed<IBus>(Buses.BulkMessaging);
        }

        public void UnbindAll(string entityName)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DataListenerHost));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                foreach (var modeSubscriptions in _subscriptions)
                {
                    IDisposable currentSubscription;
                    if (modeSubscriptions.Value.TryGetValue(entityName, out currentSubscription))
                    {
                        currentSubscription.Dispose();
                        modeSubscriptions.Value.Remove(entityName);
                    }
                }
            }
        }

        public void BindBulk(string entityName, string queue, Action<ILifetimeScope, IReadOnlyCollection<RawMessage>> onMessage)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DataListenerHost));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                EnsureSubscriptionNotExists(entityName);

                var subscription = new BufferingSubscription(this, _bulkBus.Advanced, new Queue(queue, false), onMessage);

                _subscriptions[DataMode.Bulk].Add(entityName, subscription);
            }
        }

        public void BindRowByRow(string entityName, string queue, Action<ILifetimeScope, RawMessage> onMessage)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DataListenerHost));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                EnsureSubscriptionNotExists(entityName);

                var subscription = new StreamingSubscription(this, _simpleBus.Advanced, new Queue(queue, false), onMessage);

                _subscriptions[DataMode.RowByRow].Add(entityName, subscription);
            }
        }

        private void EnsureSubscriptionNotExists(string entityName)
        {
            if (_subscriptions.Any(e => e.Value.ContainsKey(entityName)))
            {
                throw new InvalidOperationException($"Failed to create subscription for {entityName}, because there is existing one");
            }

            Console.WriteLine($"Creating new subscription for {entityName}");
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var s in _subscriptions.SelectMany(e => e.Value))
                {
                    s.Value.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
