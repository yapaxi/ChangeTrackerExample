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
using IntegrationService.Contracts.v1;
using IntegrationService.Host.DAL;
using IntegrationService.Host.DAL.Contexts;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Listeners;

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

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope(rootScope))
            using (var syncHost = new ISSynchronizerHost(scope))
            using (var listenHost = new ListenerHost(scope))
            {
                syncHost.OnDeactivatedSchema += (s, e) => listenHost.Reject(e.EntityName);
                syncHost.OnActivatedSchema += (s, e) => listenHost.Accept(e.EntityName, e.Queue, e.Schema, e.StagingTable);

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
