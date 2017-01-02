using Autofac;
using EasyNetQ;
using IntegrationService.Contracts.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Metadata
{
    public class ISSynchronizerHost : IDisposable
    {
        private readonly IBus _bus;
        private readonly ILifetimeScope _scope;
        private IDisposable _syncMetadataSubscription;

        public ISSynchronizerHost(ILifetimeScope scope)
        {
            _scope = scope;
            _bus = _scope.Resolve<IBus>();
        }

        public void Start()
        {
            _syncMetadataSubscription = _bus.Respond<SyncMetadataRequest, SyncMetadataResponse>(HandleSyncMetadataRequest);
        }

        private SyncMetadataResponse HandleSyncMetadataRequest(SyncMetadataRequest request)
        {
            try
            {
                Console.WriteLine("Accepted sync request");
                var responseItems = new List<SyncMetadataResponseItem>();
                using (var dbAccessScope = _scope.BeginLifetimeScope())
                {
                    var dbSchemaService = dbAccessScope.Resolve<DBSchemaService>();

                    foreach (var item in request.Items)
                    {
                        dbSchemaService.ActivateSchema(item.Name, item.QueueName, item.Schema);

                        responseItems.Add(new SyncMetadataResponseItem()
                        {
                            FullRebuildRequired = false,
                            Name = item.Name,
                            Result = SyncMetadataResult.Success
                        });
                        Console.WriteLine("Sync request line handeled");
                    }
                }
                Console.WriteLine("Sync request handeled");
                return new SyncMetadataResponse() { Items = responseItems.ToArray() };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        public void Dispose()
        {
            _syncMetadataSubscription?.Dispose();
            _syncMetadataSubscription = null;
        }
    }
}
