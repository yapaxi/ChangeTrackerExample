using Autofac;
using EasyNetQ;
using EasyNetQ.NonGeneric;
using RabbitModel;
using System;
using System.Diagnostics;
using IntegrationService.Host.Subscriptions;
using IntegrationService.Host.Services;
using IntegrationService.Host.DI;
using NLog;

namespace IntegrationService.Host
{
    public class Program
    {
        static void Main(string[] args)
        {
            var rootScopeName = "root";

            var builder = new ContainerBuilder();
            var logFactory = new NLogFactory();

            builder.RegisterModule(new ISModule<ILogger>(
                schemaDBConnectionString: @"server =.;database=SchemaDB;integrated security=SSPI",
                dataDBConnectionString: @"server =.;database=SchemaDB;integrated security=SSPI",
                rootScopeName: rootScopeName,
                loggerFactory: logFactory)
            );

            using (var container = builder.Build())
            using (var rootScope = container.BeginLifetimeScope(rootScopeName))
            {
                var programLogger = logFactory.CreateForType(typeof(Program));
                var dbSchemaService = rootScope.Resolve<ISchemaPersistenceService>();
                var subscriptionManager = rootScope.Resolve<ISubscriptionManager>();
                
                BindOnExistingMappings(dbSchemaService, subscriptionManager, programLogger);

                subscriptionManager.SubscribeOnMetadataSync();

                programLogger.Info("All Run");

                Process.GetCurrentProcess().WaitForExit();
            }
        }

        private static void BindOnExistingMappings(ISchemaPersistenceService dbSchemaService, ISubscriptionManager subscriptionManager, ILogger programLogger)
        {
            foreach (var mapping in dbSchemaService.GetActiveMappings())
            {
                try
                {
                    subscriptionManager.SubscribeOnDataFlow(
                        DataMode.RowByRow,
                        mapping.QueueName,
                        mapping.EntityName,
                        mapping.Schema,
                        mapping.Destination);
                }
                catch (Exception e)
                {
                    programLogger.Error(e);
                    throw;
                }
            }
        }
    }
}
