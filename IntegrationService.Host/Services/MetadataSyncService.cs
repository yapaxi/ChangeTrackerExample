using Autofac;
using Common;
using Common.Runtime;
using EasyNetQ.Topology;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Services.Policy;
using IntegrationService.Host.Subscriptions;
using NLog;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Services
{
    [SerialExecution]
    public class MetadataSyncService : IRequestResponseService<SyncMetadataRequest, SyncMetadataResponse>
    {
        private readonly ISchemaPersistenceService _dbSchemaService;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ILogger _logger;

        public MetadataSyncService(ISchemaPersistenceService service, ISubscriptionManager subscriptionManager, ILogger logger)
        {
            _logger = logger;
            _dbSchemaService = service;
            _subscriptionManager = subscriptionManager;
        }
        
        public SyncMetadataResponse Response(SyncMetadataRequest request)
        {
            _logger.Info("Accepted sync request");

            var responseItems = new List<SyncMetadataResponseItem>();
            foreach (var item in request.Items)
            {
                _logger.Info($"Handling entity: {item.EntityName}");
                try
                {
                    var responseItem = new SyncMetadataResponseItem() { Name = item.EntityName };

                    if (_subscriptionManager.SubscriptionExists(item.EntityName, DataMode.Bulk))
                    {
                        _logger.Info($"Bulk subscription already exists for {item.EntityName}; Completion is required prior to any entity {item.EntityName} changes;");
                        responseItem.FullRebuildInProgress = true;
                    }
                    else
                    {
                        var status = _dbSchemaService.GetSchemaStatus(item.EntityName, item.QueueName, item.Schema);
                        if (status.FullRebuildRequired)
                        {
                            responseItem.FullRebuildRequired = true;
                            responseItem.FullRebuildInProgress = true;
                            _logger.Info($"Full rebuild required; closing all existing subscriptions for {item.EntityName}");
                            _subscriptionManager.CloseAllEntitySubscriptions(item.EntityName);
                            
                            CreateNewSubscription(item, DataMode.Bulk);
                        }
                        else if (!_subscriptionManager.SubscriptionExists(item.EntityName, DataMode.RowByRow))
                        {
                            CreateNewSubscription(item, DataMode.RowByRow);
                        }
                        else
                        {
                            _logger.Info($"No schema changes detected; {DataMode.RowByRow} subscription alread exists for {item.EntityName}; Nothing to do;");
                        }
                    }

                    responseItem.Result = SyncMetadataResult.Success;

                    responseItems.Add(responseItem);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Entity {item.EntityName} sync failed. {SyncMetadataResult.UnhandledError}.");
                    responseItems.Add(new SyncMetadataResponseItem()
                    {
                        Message = e.Message,
                        Name = item.EntityName,
                        Result = SyncMetadataResult.UnhandledError
                    });
                }
            }

            _logger.Info("Sync request handeled");

            return new SyncMetadataResponse() { Items = responseItems.ToArray() };
        }

        private void CreateNewSubscription(SyncMetadataRequestItem item, DataMode mode)
        {
            _logger.Info($"Try use schema for {item.EntityName}");

            var writeDestination = _dbSchemaService.UseSchema(item.EntityName, item.QueueName, item.Schema);

            _logger.Info($"Subscribing for {item.EntityName} in {mode} mode");

            _subscriptionManager.SubscribeOnDataFlow(
                mode,
                item.EntityName,
                item.QueueName,
                new RuntimeMappingSchema(item.Schema),
                writeDestination);
        }
    }
}
