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
            var rootScope = "root";
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule(new RabbitAutofacModule(rootScope));

            containerBuilder.Register(e => new SchemaContext(@"server =.;database=SchemaDB;integrated security=SSPI"));
            containerBuilder.Register(e => new DataContext(@"server =.;database=SchemaDB;integrated security=SSPI"));
            containerBuilder.RegisterType<SchemaRepository>();
            containerBuilder.RegisterType<DataRepository>();
            containerBuilder.RegisterType<DBSchemaService>();
            containerBuilder.RegisterType<FlatMessageConverter>().As<IConverter<FlatMessage>>().SingleInstance();

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope(rootScope))
            using (var syncHost = new ISSynchronizerHost(scope))
            using (var simpleListenerHost = new ListenerHost(scope.ResolveNamed<IBus>(Buses.SimpleMessaging)))
            {
                syncHost.OnDeactivatedSchema += (s, e) => simpleListenerHost.Reject(e.EntityName);
                syncHost.OnActivatedSchema += (s, e) => simpleListenerHost.Accept(e.EntityName, e.Queue, (rawMessage) =>
                {
                    using (var dataWriterScope = scope.BeginLifetimeScope())
                    {
                        var context = dataWriterScope.Resolve<DataRepository>();
                        var schemaParam = new TypedParameter(typeof(RuntimeMappingSchema), e.Schema);
                        var converter = dataWriterScope.Resolve<IConverter<FlatMessage>>(schemaParam);
                        var writer = new RowByRowWriter(context, e.Destination);
                        writer.Write(converter.Convert(rawMessage.Body).Payload);
                        Console.WriteLine($"Written entity with id {rawMessage.EntityId}");
                    }
                });

                syncHost.RecoverKnownSchemas();
                syncHost.StartAcceptingExternalSchemas();

                Console.WriteLine("All Run");
                Process.GetCurrentProcess().WaitForExit();
            }
        }

        private static readonly object LOCK = new object();

        private static void WriteMessageToConsole(byte[] body, MessageProperties p, MessageReceivedInfo args)
        {
            lock (LOCK)
            {
                Console.WriteLine($"Got message from {args.Exchange}: {p.Headers[ISMessageHeader.SCHEMA_ENTITY_ID]}");
            }
        }
    }
}
