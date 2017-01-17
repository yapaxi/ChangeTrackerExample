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
using IntegrationService.Host.DI;
using NLog;
using NLog.Targets;

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

                subscriptionManager.SubscribeOnMetadataSync();

                programLogger.Info("All Run");

                Process.GetCurrentProcess().WaitForExit();
            }
        }
        
    }
}
