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

namespace IntegrationService.Host
{
    public class Program
    {
        private static readonly string RABBIT_URI = ConfigurationManager.ConnectionStrings["rabbitUri"].ConnectionString;

        static void Main(string[] args)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new RabbitAutofacModule(RABBIT_URI));

            containerBuilder.Register(e => new SchemaContext(@"server=.\sqlexpress;database=SchemaDB;integrated security=SSPI"));
            containerBuilder.RegisterType<SchemaRepository>();
            containerBuilder.RegisterType<DBSchemaService>();

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            using (var host = new ISSynchronizerHost(scope))
            { 
                host.Start();

                Console.WriteLine("All Run");
                Process.GetCurrentProcess().WaitForExit();
            }
        }


        private static void DrainQueueToConsole(IQueue queue, ILifetimeScope container)
        {
            Console.WriteLine($"Draining {queue.Name}");
            var bus = container.Resolve<IBus>();
            bus.Advanced.Consume(queue, (b, p, a) => WriteMessageToConsole(b, p, a));
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
