using Autofac;
using Common;
using EasyNetQ;
using EasyNetQ.Topology;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Listeners.Data;
using IntegrationService.Host.Listeners.Data.Subscriptions;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Writers;
using Newtonsoft.Json;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners
{
    public class ListenerHost : ListenerBase, IDisposable
    {
        private readonly object _lock;

        private readonly IBus _bus;
        private readonly IBus _simpleBus;
        private readonly IBus _bulkBus;
        private readonly SubscriptionCatalog _catalog;

        private IDisposable _syncMetadataSubscription;

        public ListenerHost(ILifetimeScope scope)
            : base(scope)
        {
            _lock = new object();
            _bus = scope.ResolveNamed<IBus>(Buses.ISSync);
            _simpleBus = scope.ResolveNamed<IBus>(Buses.SimpleMessaging);
            _bulkBus = scope.ResolveNamed<IBus>(Buses.BulkMessaging);
            _catalog = scope.Resolve<SubscriptionCatalog>();
        }

        public void RecoverKnownSchemas()
        {
            UsingScope(scope =>
            {
                var service = scope.Resolve<DBSchemaService>();

                foreach (var mapping in service.GetActiveMappings())
                {
                    try
                    {
                        Subscribe(
                            DataMode.RowByRow,
                            mapping.QueueName,
                            mapping.Name,
                            new RuntimeMappingSchema(JsonConvert.DeserializeObject<MappingSchema>(mapping.Schema)),
                            new WriteDestination(JsonConvert.DeserializeObject<StagingTable>(mapping.StagingTables)));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            });
        }

        public void StartAcceptingExternalSchemas()
        {
            _syncMetadataSubscription = _bus.Respond<SyncMetadataRequest, SyncMetadataResponse>(
                request => UsingScope(scope => HandleSyncMetadataRequest(scope, request))
            );
        }

        private SyncMetadataResponse HandleSyncMetadataRequest(ILifetimeScope requestScope, SyncMetadataRequest request)
        {
            try
            {
                lock (_lock)
                {
                    Console.WriteLine("Accepted sync request");
                    
                    var service = requestScope.Resolve<DBSchemaService>();
                    var responseItems = new List<SyncMetadataResponseItem>();
                    foreach (var item in request.Items)
                    {
                        Console.WriteLine($"Handling entity: {item.EntityName}");
                        try
                        {
                            var responseItem = new SyncMetadataResponseItem() { Name = item.EntityName };
                            var status = service.GetSchemaStatus(item.EntityName, item.QueueName, item.Schema);
                            
                            if (_catalog.SubscriptionExists(item.EntityName, DataMode.Bulk))
                            {
                                Console.WriteLine($"Bulk subscription already exists for {item.EntityName}");
                                responseItem.FullRebuildInProgress = true;
                            }
                            else
                            {
                                DataMode mode;
                                if (status.FullRebuildRequired)
                                {
                                    mode = DataMode.Bulk;
                                    responseItem.FullRebuildRequired = true;
                                    Console.WriteLine($"Full rebuild required; closing all existing subscriptions for {item.EntityName}");
                                    _catalog.RemoveAndCloseAllSubscriptions(item.EntityName);
                                }
                                else
                                {
                                    mode = DataMode.RowByRow;
                                }

                                Console.WriteLine($"Altering schema for {item.EntityName}");

                                var table = service.UseSchema(item.EntityName, item.QueueName, item.Schema);

                                Console.WriteLine($"Subscribing for {item.EntityName} in {mode} mode");

                                Subscribe(
                                    mode,
                                    item.EntityName,
                                    item.QueueName,
                                    new RuntimeMappingSchema(item.Schema),
                                    new WriteDestination(table));
                            }

                            responseItem.Result = SyncMetadataResult.Success;

                            responseItems.Add(responseItem);
                        }
                        catch (Exception e)
                        {
                            responseItems.Add(new SyncMetadataResponseItem()
                            {
                                Message = e.Message,
                                Name = item.EntityName,
                                Result = SyncMetadataResult.UnhandledError
                            });
                        }
                    }

                    Console.WriteLine("Sync request handeled");

                    return new SyncMetadataResponse() { Items = responseItems.ToArray() };
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void Subscribe(DataMode mode, string entityName, string queue, RuntimeMappingSchema schema, WriteDestination destination)
        {
            switch (mode)
            {
                case DataMode.RowByRow:
                    var streamingSubscription = new StreamingSubscription(
                        bus: _simpleBus.Advanced,
                        queue: new Queue(queue, false),
                        onMessage: (message) => HandleMessage(schema, destination, message));
                    _catalog.AddSubscription(mode, entityName, streamingSubscription);
                    break;
                case DataMode.Bulk:
                    var bulkSubscription = new BufferingSubscription(
                        bus: _bulkBus.Advanced,
                        queue: new Queue(queue, false),
                        onMessage: (message) => HandleMessage(schema, destination, message),
                        onComplete: () => _catalog.RemoveAndCloseAllSubscriptions(entityName)
                    );
                    _catalog.AddSubscription(mode, entityName, bulkSubscription);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected data mode: {mode}");
            }
        }

        private void HandleMessage<TSource>(RuntimeMappingSchema schema, WriteDestination destination, TSource message)
        {
            var schemaParam = GenericParameter.From(schema);
            var destinationParam = GenericParameter.From(destination);

            try
            {
                UsingScope(e => e.Resolve<IDataFlow<TSource>>(schemaParam, destinationParam).Write(message));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private class GenericParameter
        {
            public static TypedParameter From<T>(T instance) => new TypedParameter(typeof(T), instance);
        }

        public void Dispose()
        {
            _syncMetadataSubscription?.Dispose();
            _syncMetadataSubscription = null;
        }
    }
}
