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
using IntegrationService.Contracts.v2;
using IntegrationService.Host.DAL;
using IntegrationService.Host.DAL.Contexts;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Listeners;
using IntegrationService.Host.Converters;
using IntegrationService.Host.Writers;
using Autofac.Core;
using Common;
using IntegrationService.Host.Listeners.Metadata;
using IntegrationService.Host.Listeners.Data;

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

            containerBuilder.RegisterType<RowByRowWriter>().As<IWriter<FlatMessage>>();
            containerBuilder.RegisterType<FlatMessageConverter>().As<IConverter<RawMessage, FlatMessage>>().SingleInstance();
            containerBuilder.RegisterType<DataFlow<RawMessage, FlatMessage>>().As<IDataFlow<RawMessage>>();

            containerBuilder.RegisterType<BulkWriter>().As<IWriter<IEnumerable<FlatMessage>>>();
            containerBuilder.RegisterType<FlatMessageConverter>().As<IConverter<IEnumerable<RawMessage>, IEnumerable<FlatMessage>>>().SingleInstance();
            containerBuilder.RegisterType<DataFlow<IEnumerable<RawMessage>, IEnumerable<FlatMessage>>>().As<IDataFlow<IEnumerable<RawMessage>>>();

            using (var container = containerBuilder.Build())
            using (var rootScope = container.BeginLifetimeScope(rootScopeName))
            using (var metadataListenerHost = new MetadataListenerHost(rootScope))
            using (var dataListenerHost = new DataListenerHost(rootScope))
            {
                metadataListenerHost.OnDeactivatedSchema += (s, e) => dataListenerHost.UnbindAll(e.EntityName);

                metadataListenerHost.OnBulkActivatedSchema += 
                    (s, e) => dataListenerHost.Bind(DataMode.Bulk, e.EntityName, e.Queue, e.Schema, e.Destination);

                metadataListenerHost.OnRowByRowActivatedSchema += 
                    (s, e) => dataListenerHost.Bind(DataMode.RowByRow, e.EntityName, e.Queue, e.Schema, e.Destination);

                metadataListenerHost.RecoverKnownSchemas();
                metadataListenerHost.StartAcceptingExternalSchemas();

                Console.WriteLine("All Run");
                Process.GetCurrentProcess().WaitForExit();
            }
        }
        
    }
}
