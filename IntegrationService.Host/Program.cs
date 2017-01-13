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
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.DAL.Contexts;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Listeners;
using IntegrationService.Host.Converters;
using IntegrationService.Host.Writers;
using Autofac.Core;
using Common;
using IntegrationService.Host.Listeners.Data;
using IntegrationService.Host.Listeners.Data.Subscriptions;

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
            containerBuilder.RegisterType<SchemaRepository>();
            containerBuilder.RegisterType<DataRepository>();
            containerBuilder.RegisterType<DBSchemaService>();
            containerBuilder.RegisterType<SubscriptionCatalog>().SingleInstance();

            containerBuilder.RegisterType<FlatMessageConverter>().As<IConverter<RawMessage, FlatMessage>>().SingleInstance();
            containerBuilder.RegisterType<FlatMessageConverter>().As<IConverter<IEnumerable<RawMessage>, FlatMessage>>().SingleInstance();

            containerBuilder.RegisterType<RowByRowWriter>();
            containerBuilder.RegisterType<BulkWriter>();

            containerBuilder.RegisterType<DataFlow<RawMessage, FlatMessage, RowByRowWriter>>().As<IDataFlow<RawMessage>>();
            containerBuilder.RegisterType<DataFlow<IEnumerable<RawMessage>, FlatMessage, BulkWriter>>().As<IDataFlow<IEnumerable<RawMessage>>>();


            using (var container = containerBuilder.Build())
            using (var rootScope = container.BeginLifetimeScope(rootScopeName))
            using (var dataListenerHost = new ListenerHost(rootScope))
            {
                dataListenerHost.RecoverKnownSchemas();
                dataListenerHost.StartAcceptingExternalSchemas();

                Console.WriteLine("All Run");
                Process.GetCurrentProcess().WaitForExit();
            }
        }
        
    }
}
