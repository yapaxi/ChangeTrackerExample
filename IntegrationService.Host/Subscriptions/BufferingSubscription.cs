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
using NLog;
using System.Diagnostics;

namespace IntegrationService.Host.Subscriptions
{
    internal class BufferingSubscription : IDisposable
    {
        private readonly int _bufferSize;
        private readonly object _lock;
        private readonly IDisposable _subscription;
        private readonly Action<IReadOnlyCollection<RawMessage>> _onMessage;
        private readonly Action _onComplete;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;

        private bool _disposed;


        public BufferingSubscription(
            IAdvancedBus bus,
            string queue,
            Action<IReadOnlyCollection<RawMessage>> onMessage,
            Action onComplete,
            int bufferSize,
            ILogger logger)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            _logger = logger;
            _bufferSize = bufferSize;
            _onComplete = onComplete;
            _onMessage = onMessage;
            _lock = new object();

            var messages = new List<RawMessage>(_bufferSize);

            _subscription = bus.Consume(
                new Queue(queue, false),
                (data, properties, info) => HandleMessage(data, properties, messages)
            );
        }

        private void HandleMessage(byte[] data, MessageProperties properties, List<RawMessage> buffer)
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

                    var rawMessage = new RawMessage(data, (int)properties.Headers[ISMessageHeader.ENTITY_COUNT]);

                    lastReceived = (bool)properties.Headers[ISMessageHeader.BATCH_IS_LAST];

                    var rangeId = (int)properties.Headers[ISMessageHeader.BATCH_ORDINAL];

                    _logger.Info($"Accepted range: rangeId={rangeId},isLast={lastReceived}");

                    buffer.Add(rawMessage);

                    if (buffer.Count >= _bufferSize || lastReceived)
                    {
                        _logger.Info($"Flushing buffer");


                        _onMessage(buffer);


                        buffer.Clear();
                    }
                }

                if (lastReceived)
                {
                    _logger.Info("Last message received");
                    _onComplete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
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

            _stopwatch.Stop();
            _logger.Debug($"Buffering subscription lived for {_stopwatch.Elapsed}");
        }
    }
}
