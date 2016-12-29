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

namespace IntegrationServiceExample
{
    public class Program
    {
        private static readonly string RABBIT_URI = ConfigurationManager.ConnectionStrings["rabbitUri"].ConnectionString;
        private static readonly string IS_QUEUE_1 = ConfigurationManager.ConnectionStrings["ISQueue1"].ConnectionString;
        private static readonly string IS_QUEUE_2 = ConfigurationManager.ConnectionStrings["ISQueue2"].ConnectionString;

        static void Main(string[] args)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new RabbitAutofacModule(RABBIT_URI));

            using (var container = containerBuilder.Build())
            {
                using (var outerScope = container.BeginLifetimeScope())
                {
                    var rabbitModelBuilder = new RabbitCommunicationModelBuilder(outerScope.Resolve<IBus>().Advanced);

                    var queue1 = rabbitModelBuilder.BuildISExpectationsContract(queueToReceiveFrom: IS_QUEUE_1);
                    var queue2 = rabbitModelBuilder.BuildISExpectationsContract(queueToReceiveFrom: IS_QUEUE_2);

                    Task.WaitAll
                    (
                        DrainQueueToConsole(queue1, outerScope),
                        DrainQueueToConsole(queue2, outerScope)
                    );
                }
            }
        }
        
        private static Task DrainQueueToConsole(IQueue queue, ILifetimeScope container)
        {
            Console.WriteLine($"Draining {queue.Name}");

            var cmplSource = new TaskCompletionSource<object>();
            var bus = container.Resolve<IBus>();

            bus.Advanced.Consume(queue, (b, p, a) => WriteMessageToConsole(b, p, a));

            return cmplSource.Task;
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
