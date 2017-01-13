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

namespace IntegrationService.Host.Listeners.Data.Subscriptions
{
    internal class BufferingSubscription : IDisposable
    {
        private class RawMessageWithProgress : RawMessage
        {
            public RawMessageWithProgress(byte[] body, int entityCount, bool isLast, int rangeId) 
                : base(body, entityCount)
            {
                IsLast = isLast;
                RangeId = rangeId;
            }

            public bool IsLast { get;  }

            public int RangeId { get; }
        }

        private readonly object _lock;
        private readonly IDisposable _subscription;
        private readonly Action<IReadOnlyCollection<RawMessage>> _onMessage;
        private readonly Action _onComplete;
        
        private bool _disposed;

        public BufferingSubscription(
            IAdvancedBus bus,
            Queue queue,
            Action<IEnumerable<RawMessage>> onMessage,
            Action onComplete)
        {
            _onComplete = onComplete;
            _onMessage = onMessage;
            _lock = new object();

            var messages = new List<RawMessageWithProgress>();

            _subscription = bus.Consume(
                queue,
                (data, properties, info) => HandleMessage(data, properties, messages)
            );
        }

        private void HandleMessage(byte[] data, MessageProperties properties, List<RawMessageWithProgress> buffer)
        {
            try
            {
                bool lastReceived;
                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    var rawMessage = new RawMessageWithProgress(
                        data,
                        (int)properties.Headers[ISMessageHeader.ENTITY_COUNT],
                        (bool)properties.Headers[ISMessageHeader.BATCH_IS_LAST],
                        (int)properties.Headers[ISMessageHeader.BATCH_ORDINAL]
                    );

                    lastReceived = rawMessage.IsLast;

                    Console.WriteLine($"[{nameof(BufferingSubscription)}] Accepted range: rangeId={rawMessage.RangeId},isLast={rawMessage.IsLast}");

                    buffer.Add(rawMessage);

                    if (buffer.Count >= 10 || lastReceived)
                    {
                        Console.WriteLine($"[{nameof(BufferingSubscription)}] Flushing buffer");
                        _onMessage(buffer);
                        buffer.Clear();
                    }
                }

                if (lastReceived)
                {
                    Console.WriteLine("Last message received");
                    _onComplete();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
                _subscription.Dispose();
            }
        }
    }
}
