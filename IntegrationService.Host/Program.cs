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
            containerBuilder.RegisterType<FlatMessageConverter>().As<IConverter<RawMessage, FlatMessage>>().SingleInstance();
            containerBuilder.RegisterType<FlatMessageConverter>().As<IConverter<IEnumerable<RawMessage>, IEnumerable<FlatMessage>>>().SingleInstance();
            containerBuilder.RegisterType<RowByRowWriter>();

            using (var container = containerBuilder.Build())
            using (var rootScope = container.BeginLifetimeScope(rootScopeName))
            using (var metadataListenerHost = new MetadataListenerHost(rootScope))
            using (var dataListenerHost = new DataListenerHost(rootScope))
            {
                metadataListenerHost.OnDeactivatedSchema += (s, e) => dataListenerHost.UnbindAll(e.EntityName);

                metadataListenerHost.OnBulkActivatedSchema += (s, e) => dataListenerHost.BindBulk
                (
                    e.EntityName,
                    e.Queue,
                    (scope, rawMessages) => new WriteDispatcher<IEnumerable<RawMessage>, IEnumerable<FlatMessage>>(scope, e.Schema, e.Destination).Write(rawMessages)
                );

                metadataListenerHost.OnRowByRowActivatedSchema += (s, e) => dataListenerHost.BindRowByRow
                (
                    e.EntityName,
                    e.Queue, 
                    (scope, rawMessage) => new WriteDispatcher<RawMessage, FlatMessage>(scope, e.Schema, e.Destination).Write(rawMessage)
                );

                metadataListenerHost.RecoverKnownSchemas();
                metadataListenerHost.StartAcceptingExternalSchemas();

                Console.WriteLine("All Run");
                Process.GetCurrentProcess().WaitForExit();
            }
        }

#warning REWRITE

        private class WriteDispatcher<TSource, TResult>
        {
            private readonly IConverter<TSource, TResult> _converter;
            private readonly IWriter<TResult> _writer;

            public WriteDispatcher(ILifetimeScope scope, RuntimeMappingSchema schema, WriteDestination destination)
            {
                var schemaParam = new TypedParameter(typeof(RuntimeMappingSchema), schema);
                var destinationParam = new TypedParameter(typeof(WriteDestination), destination);
                _converter = scope.Resolve<IConverter<TSource, TResult>>(schemaParam);
                _writer = scope.Resolve<IWriter<TResult>>(destinationParam);
            }

            public void Write(TSource rawMessage)
            {
                _writer.Write(_converter.Convert(rawMessage));
            }
        }
    }
}
