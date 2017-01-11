using Autofac;
using EasyNetQ;
using EasyNetQ.Topology;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace IntegrationService.Host.Listeners.Data
{
    public partial class DataListenerHost
    {
        private class BufferingSubscription : IDisposable
        {
            private readonly object _lock;
            private readonly IDisposable _subscription;
            private readonly Timer _timer;
            private readonly DataListenerHost _host;
            private readonly Action<ILifetimeScope, IReadOnlyCollection<RawMessage>> _onMessage;

            private bool _disposed;
            private int _inProgress;
            private List<RawMessage> _messages;

            public BufferingSubscription(DataListenerHost host, IAdvancedBus bus, Queue queue, Action<ILifetimeScope, IReadOnlyCollection<RawMessage>> onMessage)
            {
                _onMessage = onMessage;
                _messages = new List<RawMessage>();
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

                            _messages.Add(new RawMessage((int)properties.Headers[ISMessageHeader.SCHEMA_ENTITY_ID], data));
                        }
                    }
                );

                _timer = new Timer(Flush, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            }

            private void Flush(object state)
            {
                try
                {
                    if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
                    {
                        return;
                    }

                    lock (_lock)
                    {
                        var messages = _messages;
                        _messages = new List<RawMessage>();
                        _host.UsingScope(scope =>
                        {
                            _onMessage(scope, messages);
                        });
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _inProgress, 0);
                }
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
