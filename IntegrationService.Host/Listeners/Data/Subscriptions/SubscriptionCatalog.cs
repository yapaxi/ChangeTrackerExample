using Common;
using IntegrationService.Host.DAL;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners.Data.Subscriptions
{
    public class SubscriptionCatalog : IDisposable
    {
        private readonly Dictionary<DataMode, Dictionary<string, IDisposable>> _subscriptions;
        private readonly object _lock;

        private bool _disposed;

        public SubscriptionCatalog()
        {
            _lock = new object();
            _subscriptions = new Dictionary<DataMode, Dictionary<string, IDisposable>>()
            {
                { DataMode.RowByRow, new Dictionary<string, IDisposable>() },
                { DataMode.Bulk, new Dictionary<string, IDisposable>() },
            };
        }

        public void RemoveAndCloseAllSubscriptions(string entityName)
        {
            Console.WriteLine($"Removing all subscriptions for {entityName}");

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionCatalog));
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

        public bool SubscriptionExists(string name, DataMode mode)
        {
            lock (_lock)
            {
                return _subscriptions[mode].ContainsKey(name);
            }
        }

        public void AddSubscription(DataMode mode, string entityName, IDisposable subscription)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionCatalog));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                EnsureSubscriptionNotExists(entityName);
                
                _subscriptions[mode].Add(entityName, subscription);
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
