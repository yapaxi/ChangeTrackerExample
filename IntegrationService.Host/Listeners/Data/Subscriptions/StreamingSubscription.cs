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

    internal class StreamingSubscription : IDisposable
    {
        private readonly object _lock;
        private readonly IDisposable _subscription;

        private bool _disposed;

        public StreamingSubscription(IAdvancedBus bus, Queue queue, Action<RawMessage> onMessage)
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

                    onMessage(new RawMessage(data, (int)properties.Headers[ISMessageHeader.ENTITY_COUNT]));
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
