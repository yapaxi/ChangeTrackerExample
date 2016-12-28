using Autofac;
using EasyNetQ.Management.Client;
using RabbitModel;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                using (var outerScope = container.BeginLifetimeScope("outer"))
                {
                    var rabbitModelBuilder = new RabbitCommunicationModelBuilder(outerScope.Resolve<ManagementClient>());

                    rabbitModelBuilder.BuildISExpectationsContract(queueToReceiveFrom: IS_QUEUE_1);
                    rabbitModelBuilder.BuildISExpectationsContract(queueToReceiveFrom: IS_QUEUE_2);

                    Task.WaitAll
                    (
                        DrainQueueToConsole(IS_QUEUE_1, outerScope),
                        DrainQueueToConsole(IS_QUEUE_2, outerScope)
                    );
                }
            }
        }
        
        private static Task DrainQueueToConsole(string queue, ILifetimeScope container)
        {
            Console.WriteLine($"Draining {queue}");

            var cmplSource = new TaskCompletionSource<object>();
            var connection = container.Resolve<IConnection>();

            var model = connection.CreateModel();
            var consoleConsumer1 = new EventingBasicConsumer(model);
            consoleConsumer1.Received += (s, o) => WriteMessageToConsole(o);
            consoleConsumer1.ConsumerCancelled += (s, o) => cmplSource.TrySetCanceled();
            model.BasicConsume(queue, true, consoleConsumer1);

            return cmplSource.Task;
        }

        private static readonly object LOCK = new object();

        private static void WriteMessageToConsole(BasicDeliverEventArgs args)
        {
            lock (LOCK)
            {
                Console.WriteLine($"Got message from {args.Exchange}");
            }
        }
    }
}
