using Autofac;
using Common;
using EasyNetQ.Topology;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Subscriptions;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Services
{
    public class MetadataSyncService : IRequestResponseService<SyncMetadataRequest, SyncMetadataResponse>
    {
        private readonly SchemaPersistenceService _dbSchemaService;
        private readonly SubscriptionManager _subscriptionManager;

        public MetadataSyncService(SchemaPersistenceService service, SubscriptionManager subscriptionManager)
        {
            _dbSchemaService = service;
            _subscriptionManager = subscriptionManager;
        }
        
        public SyncMetadataResponse Response(SyncMetadataRequest request)
        {
            Console.WriteLine("Accepted sync request");

            var responseItems = new List<SyncMetadataResponseItem>();
            foreach (var item in request.Items)
            {
                Console.WriteLine($"Handling entity: {item.EntityName}");
                try
                {
                    var responseItem = new SyncMetadataResponseItem() { Name = item.EntityName };
                    var status = _dbSchemaService.GetSchemaStatus(item.EntityName, item.QueueName, item.Schema);

                    if (_subscriptionManager.SubscriptionExists(item.EntityName, DataMode.Bulk))
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
                            _subscriptionManager.CloseAllEntitySubscriptions(item.EntityName);
                        }
                        else
                        {
                            mode = DataMode.RowByRow;
                        }

                        Console.WriteLine($"Altering schema for {item.EntityName}");

                        var destinationTable = _dbSchemaService.UseSchema(item.EntityName, item.QueueName, item.Schema);

                        Console.WriteLine($"Subscribing for {item.EntityName} in {mode} mode");

                        _subscriptionManager.SubscribeOnDataFlow(
                            mode,
                            item.EntityName,
                            item.QueueName,
                            new RuntimeMappingSchema(item.Schema),
                            new WriteDestination(destinationTable));
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
}
