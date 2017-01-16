using Autofac;
using EasyNetQ;
using EasyNetQ.NonGeneric;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using System.Diagnostics;
using IntegrationService.Host.DAL;
using IntegrationService.Host.DAL.Contexts;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Converters;
using Autofac.Core;
using Common;
using Newtonsoft.Json;
using IntegrationService.Host.Middleware;
using IntegrationService.Host.Subscriptions;
using IntegrationService.Host.Services;
using IntegrationService.Contracts.v3;

namespace IntegrationService.Host
{
    public class Program
    {
        static void Main(string[] args)
        {
            var rootScopeName = "root";
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule(new RabbitAutofacModule(rootScopeName));

            containerBuilder.Register(e => new SchemaContext(@"server =.;database=SchemaDB;integrated security=SSPI"));
            containerBuilder.Register(e => new DataContext(@"server =.;database=SchemaDB;integrated security=SSPI"));

            containerBuilder.RegisterType<SchemaRepository>().InstancePerLifetimeScope();
            containerBuilder.RegisterType<DataRepository>().InstancePerLifetimeScope();
            containerBuilder.RegisterType<SchemaPersistenceService>().InstancePerLifetimeScope();

            containerBuilder.RegisterType<RequestLifetimeHandler>().As<IRequestLifetimeHandler>().SingleInstance();

            containerBuilder.Register(e => new SubscriptionManager(
                handler: e.Resolve<IRequestLifetimeHandler>(),
                isBus: e.ResolveNamed<IBus>(Buses.ISSync),
                simpleBus: e.ResolveNamed<IBus>(Buses.SimpleMessaging), 
                bulkBus: e.ResolveNamed<IBus>(Buses.BulkMessaging)
            )).SingleInstance();

            containerBuilder.RegisterType<FlatMessageConverter>().SingleInstance();

            containerBuilder.RegisterType<MetadataSyncService>()
                .As<IRequestResponseService<SyncMetadataRequest, SyncMetadataResponse>>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<MessagingService>()
                .As<IMessagingService<RawMessage>>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<MessagingService>()
                .As<IMessagingService<IReadOnlyCollection<RawMessage>>>()
                .InstancePerLifetimeScope();

            using (var container = containerBuilder.Build())
            using (var rootScope = container.BeginLifetimeScope(rootScopeName))
            {
                var dbSchemaService = rootScope.Resolve<SchemaPersistenceService>();
                var subscriptionManager = rootScope.Resolve<SubscriptionManager>();

                foreach (var mapping in dbSchemaService.GetActiveMappings())
                {
                    try
                    {
                        subscriptionManager.SubscribeOnDataFlow(
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

                subscriptionManager.SubscribeOnMetadataSync();

                Console.WriteLine("All Run");

                Process.GetCurrentProcess().WaitForExit();
            }
        }
        
    }
}
