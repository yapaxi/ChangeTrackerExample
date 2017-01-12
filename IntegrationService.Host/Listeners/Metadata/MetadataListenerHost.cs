using Autofac;
using Common;
using EasyNetQ;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Metadata;
using Newtonsoft.Json;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners.Metadata
{
    public class MetadataListenerHost : ListenerBase, IDisposable
    {
        private readonly object _lock;
        private readonly IBus _bus;
        private IDisposable _syncMetadataSubscription;

        public MetadataListenerHost(ILifetimeScope scope)
            : base(scope)
        {
            _lock = new object();
            _bus = scope.ResolveNamed<IBus>(Buses.ISSync);
        }

        public event EventHandler<ActivatedSchemaEventArgs> OnRowByRowActivatedSchema;
        public event EventHandler<ActivatedSchemaEventArgs> OnBulkActivatedSchema;
        public event EventHandler<SchemaEventArgs> OnDeactivatedSchema;

        public void RecoverKnownSchemas()
        {
            UsingScope(scope =>
            {
                var service = scope.Resolve<DBSchemaService>();

                foreach (var mapping in service.GetActiveMappings())
                {
                    try
                    {
                        OnRowByRowActivatedSchema?.Invoke(
                                this,
                                new ActivatedSchemaEventArgs(
                                    mapping.QueueName,
                                    mapping.Name,
                                    new RuntimeMappingSchema(JsonConvert.DeserializeObject<MappingSchema>(mapping.Schema)),
                                    new WriteDestination(JsonConvert.DeserializeObject<StagingTable>(mapping.StagingTables)))
                        );
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

                    RequireDeactivation(request);

                    var activatedSchemas = ActivateSchemas(
                        request: request,
                        service: requestScope.Resolve<DBSchemaService>()
                    );

                    RequireActivation(request, activatedSchemas);
                    
                    Console.WriteLine("Sync request handeled");

                    return new SyncMetadataResponse()
                    {
                        Items = activatedSchemas.Select(Convert).ToArray()
                    };
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static IReadOnlyCollection<SchemaActivationResult> ActivateSchemas(SyncMetadataRequest request, DBSchemaService service)
        {
            var activatedSchemas = new List<SchemaActivationResult>();

            foreach (var item in request.Items)
            {
                var activationResult = service.ActivateSchema(item.Name, item.QueueName, item.Schema);
                activatedSchemas.Add(activationResult);
            }

            return activatedSchemas;
        }

        private void RequireDeactivation(SyncMetadataRequest request)
        {
            foreach (var v in request.Items)
            {
                OnDeactivatedSchema?.Invoke(this, new SchemaEventArgs(v.Name));
            }
        }

        private void RequireActivation(SyncMetadataRequest request, IReadOnlyCollection<SchemaActivationResult> activationResult)
        {
            foreach (var activatedSchema in activationResult.Where(e => !e.IsFailed))
            {
                var details = request.Items.Where(e => e.Name == activatedSchema.Name).Single();

                var eventArgs = new ActivatedSchemaEventArgs(
                            details.QueueName,
                            details.Name,
                            new RuntimeMappingSchema(details.Schema),
                            new WriteDestination(activatedSchema.StagingTable));

                if (activatedSchema.FullRebuildRequired)
                {
                    OnDeactivatedSchema?.Invoke(this, new SchemaEventArgs(details.Name));
                    OnBulkActivatedSchema?.Invoke(this, eventArgs);
                }
                else
                {
                    OnRowByRowActivatedSchema?.Invoke(this, eventArgs);
                }
            }
        }

        private static SyncMetadataResponseItem Convert(SchemaActivationResult e)
        {
            return new SyncMetadataResponseItem()
            {
                Name = e.Name,
                FullRebuildRequired = e.FullRebuildRequired,
                Message = e.Exception?.Message,
                Result = ChooseActivationResult(e)
            };
        }

        private static SyncMetadataResult ChooseActivationResult(SchemaActivationResult e)
        {
            SyncMetadataResult result;

            if (!e.IsFailed)
            {
                result = SyncMetadataResult.Success;
            }
            else
            {
                result = SyncMetadataResult.UnhandledError;
            }

            return result;
        }

        public void Dispose()
        {
            _syncMetadataSubscription?.Dispose();
            _syncMetadataSubscription = null;
        }
    }
}
