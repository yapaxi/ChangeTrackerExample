using Autofac;
using Common;
using EasyNetQ;
using IntegrationService.Contracts.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Metadata
{
    public class ISSynchronizerHost : IDisposable
    {
        private readonly object _lock;
        private readonly IBus _bus;
        private readonly ILifetimeScope _scope;
        private IDisposable _syncMetadataSubscription;

        public ISSynchronizerHost(ILifetimeScope scope)
        {
            _lock = new object();
            _scope = scope;
            _bus = _scope.ResolveNamed<IBus>(Buses.ISSync);
        }

        public event EventHandler<ActivatedSchemaEventArgs> OnActivatedSchema;

        public void RecoverKnownSchemas()
        {
            using (var dbAccessScope = _scope.BeginLifetimeScope())
            {
                var service = dbAccessScope.Resolve<DBSchemaService>();

                foreach (var mapping in service.GetActiveMappings())
                {
                    OnActivatedSchema?.Invoke(
                            this,
                            new ActivatedSchemaEventArgs(
                                mapping.Name,
                                JsonConvert.DeserializeObject<MappingSchema>(mapping.Schema),
                                mapping.QueueName,
                                new DAL.StagingTable(mapping.StagingTableName))
                    );
                }
            }
        }

        public void StartAcceptingExternalSchemas()
        {
            _syncMetadataSubscription = _bus.Respond<SyncMetadataRequest, SyncMetadataResponse>(HandleSyncMetadataRequest);
        }

        private SyncMetadataResponse HandleSyncMetadataRequest(SyncMetadataRequest request)
        {
            try
            {
                lock (_lock)
                {
                    Console.WriteLine("Accepted sync request");
                    var activationResult = ActivateSchemas(request.Items);

                    foreach (var activatedSchema in activationResult.Where(e => !e.IsFailed))
                    {
                        var details = request.Items.Where(e => e.Name == activatedSchema.Name).Single();
                        if (!activatedSchema.FullRebuildRequired)
                        {
                            OnActivatedSchema?.Invoke(
                                this,
                                new ActivatedSchemaEventArgs(
                                    details.Name,
                                    details.Schema,
                                    details.QueueName,
                                    activatedSchema.StagingTable)
                            );
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }

                    var items = activationResult.Select(Convert).ToArray();

                    Console.WriteLine("Sync request handeled");
                    return new SyncMetadataResponse() { Items = items };
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static SyncMetadataResponseItem Convert(SchemaActivationResult e)
        {
            var result = SyncMetadataResult.UnhandledError;

            if (!e.IsFailed)
            {
                result = SyncMetadataResult.Success;
            }
            else
            {
                throw new NotImplementedException(e.Exception.Message);
            }

            return new SyncMetadataResponseItem()
            {
                Name = e.Name,
                FullRebuildRequired = e.FullRebuildRequired,
                Message = e.Exception?.Message,
                Result = result
            };
        }

        private SchemaActivationResult[] ActivateSchemas(SyncMetadataRequestItem[] items)
        {
            using (var dbAccessScope = _scope.BeginLifetimeScope())
            {
                var dbSchemaService = dbAccessScope.Resolve<DBSchemaService>();
                return items.Select(item => dbSchemaService.ActivateSchema(item.Name, item.QueueName, item.Schema)).ToArray();
            }
        }

        public void Dispose()
        {
            _syncMetadataSubscription?.Dispose();
            _syncMetadataSubscription = null;
        }
    }
}
