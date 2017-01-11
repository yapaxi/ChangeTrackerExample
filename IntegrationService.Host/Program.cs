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
            containerBuilder.RegisterType<FlatMessageConverter>().As<IConverter<FlatMessage>>().SingleInstance();
            containerBuilder.RegisterType<RowByRowWriter>();

            using (var container = containerBuilder.Build())
            using (var rootScope = container.BeginLifetimeScope(rootScopeName))
            using (var syncHost = new ISSynchronizerHost(rootScope))
            using (var listenerHost = new ListenerHost(rootScope))
            {
                syncHost.OnDeactivatedSchema += (s, e) => listenerHost.Reject(e.EntityName);

                syncHost.OnActivatedSchema += (s, e) => listenerHost.Accept(e.EntityName, e.Queue, (scope, rawMessage) =>
                {
                    var schemaParam = new TypedParameter(typeof(RuntimeMappingSchema), e.Schema);
                    var destinationParam = new TypedParameter(typeof(WriteDestination), e.Destination);

                    var converter = scope.Resolve<IConverter<FlatMessage>>(schemaParam);
                    var writer = scope.Resolve<RowByRowWriter>(destinationParam);

                    writer.Write(converter.Convert(rawMessage.Body).Payload);

                    Console.WriteLine($"\tWritten entity with id {rawMessage.EntityId}");
                });

                syncHost.RecoverKnownSchemas();
                syncHost.StartAcceptingExternalSchemas();

                Console.WriteLine("All Run");
                Process.GetCurrentProcess().WaitForExit();
            }
        }
    }
}
