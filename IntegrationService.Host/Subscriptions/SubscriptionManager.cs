﻿using Common;
using Common.Runtime;
using EasyNetQ;
using EasyNetQ.Topology;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.DI;
using IntegrationService.Host.Middleware;
using NLog;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Subscriptions
{
    public class SubscriptionManager : ISubscriptionManager, IDisposable
    {
        private IDisposable _syncMetadataSubscription;
        private readonly Dictionary<DataMode, Dictionary<string, IDisposable>> _subscriptions;
        private readonly object _lock;

        private readonly IBus _isBus;
        private readonly IBus _simpleBus;
        private readonly IBus _bulkBus;
        private readonly IRequestLifetimeHandler _messageHandler;

        private readonly ILogger _logger;
        private readonly ILoggerFactory<ILogger> _loggerFactory;

        private readonly int _bulkBufferSize;

        private bool _disposed;

        public SubscriptionManager(
            IRequestLifetimeHandler handler,
            IBus isBus, IBus simpleBus, IBus bulkBus,
            ILogger logger, ILoggerFactory<ILogger> loggerFactory,
            int bulkBufferSize = 10)
        {
            _lock = new object();

            _bulkBufferSize = bulkBufferSize;

            _logger = logger;
            _loggerFactory = loggerFactory;

            _isBus = isBus;
            _simpleBus = simpleBus;
            _bulkBus = bulkBus;
            _messageHandler = handler;

            _subscriptions = new Dictionary<DataMode, Dictionary<string, IDisposable>>()
            {
                { DataMode.RowByRow, new Dictionary<string, IDisposable>() },
                { DataMode.Bulk, new Dictionary<string, IDisposable>() },
            };
        }


        public bool SubscriptionExists(string name, DataMode mode)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionManager));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                return _subscriptions[mode].ContainsKey(name);
            }
        }

        public void SubscribeOnMetadataSync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionManager));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                if (_syncMetadataSubscription != null)
                {
                    throw new InvalidOperationException();
                }

                _syncMetadataSubscription = _isBus.Respond<SyncMetadataRequest, SyncMetadataResponse>(
                    e => _messageHandler.Response<SyncMetadataRequest, SyncMetadataResponse>(e));
            }
        }

        public void SubscribeOnDataFlow(DataMode mode, string entityName, string queue, IRuntimeMappingSchema schema, IWriteDestination destination)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionManager));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                EnsureSubscriptionNotExists(entityName);

                var messageInfo = new MessageInfo(schema, destination);

                switch (mode)
                {
                    case DataMode.RowByRow:
                        var streamingSubscription = new StreamingSubscription(
                            bus: _simpleBus.Advanced,
                            queue: queue,
                            onMessage: (message) => _messageHandler.HandleDataMessage(message, messageInfo),
                            logger: _loggerFactory.CreateForType(typeof(BufferingSubscription)));
                        _subscriptions[mode].Add(entityName, streamingSubscription);
                        break;
                    case DataMode.Bulk:
                        var bulkSubscription = new BufferingSubscription(
                            bus: _bulkBus.Advanced,
                            queue: queue,
                            onMessage: (message) => _messageHandler.HandleDataMessage(message, messageInfo),
                            onComplete: () => CloseAllEntitySubscriptions(entityName),
                            bufferSize: _bulkBufferSize,
                            logger: _loggerFactory.CreateForType(typeof(BufferingSubscription)));
                        _subscriptions[mode].Add(entityName, bulkSubscription);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected data mode: {mode}");
                }
            }
        }

        public void CloseAllEntitySubscriptions(string entityName)
        {
            _logger.Info($"Removing all subscriptions for {entityName}");

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionManager));
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
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

        private void EnsureSubscriptionNotExists(string entityName)
        {
            if (_subscriptions.Any(e => e.Value.ContainsKey(entityName)))
            {
                throw new InvalidOperationException($"Failed to create subscription for {entityName}, because there is existing one");
            }

            _logger.Info($"Creating new subscription for {entityName}");
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
