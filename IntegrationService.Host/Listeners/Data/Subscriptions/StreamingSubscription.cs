using Autofac;
using EasyNetQ;
using EasyNetQ.Topology;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners.Data
{
    public partial class DataListenerHost
    {
        private class StreamingSubscription : IDisposable
        {
            private readonly object _lock;
            private readonly IDisposable _subscription;

            private bool _disposed;

            public StreamingSubscription(DataListenerHost host, IAdvancedBus bus, Queue queue, Action<ILifetimeScope, RawMessage> onMessage)
            {
                _lock = new object();
                _subscription = bus.Consume(
                    queue,
                    (data, properties, info) =>
                    {
                        lock (_lock)
                        {
                            if (_disposed)
                            {
                                return;
                            }
                        }

                        host.UsingScope(scope => onMessage(
                            scope,
                            new RawMessage((int)properties.Headers[ISMessageHeader.SCHEMA_ENTITY_ID], data))
                        );
                    }
                );
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    _disposed = true;
                }

                _subscription.Dispose();
            }
        }
    }
}
