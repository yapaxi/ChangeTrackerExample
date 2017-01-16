using Autofac;
using EasyNetQ;
using EasyNetQ.Topology;
using NLog;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Subscriptions
{

    internal class StreamingSubscription : IDisposable
    {
        private readonly object _lock;
        private readonly IDisposable _subscription;
        private readonly Action<RawMessage> _onMessage;
        private readonly ILogger _logger;
        private readonly string _queue;

        private bool _disposed;

        public StreamingSubscription(IAdvancedBus bus, string queue, Action<RawMessage> onMessage, ILogger logger)
        {
            _queue = queue;
            _logger = logger;
            _onMessage = onMessage;
            _lock = new object();
            _subscription = bus.Consume(
                new Queue(queue, false),
                (data, properties, info) => HandleMessage(data, properties)
            );
        }

        private void HandleMessage(byte[] data, MessageProperties properties)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
            }

            _logger.Debug($"Received message from queue {_queue}");
            _onMessage(new RawMessage(data, (int)properties.Headers[ISMessageHeader.ENTITY_COUNT]));
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
