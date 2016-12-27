using Autofac;
using ChangeTrackerExample.App;
using ChangeTrackerExample.Configuration;
using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChangeTrackerExample
{
    public class Program
    {
        private static readonly string RABBIT_URI = ConfigurationManager.ConnectionStrings["rabbitUri"].ConnectionString;
        private static readonly string CT_EXCHANGE_1 = ConfigurationManager.ConnectionStrings["ctExchange1"].ConnectionString;
        private static readonly string CT_EXCHANGE_2 = ConfigurationManager.ConnectionStrings["ctExchange2"].ConnectionString;
        private static readonly string CT_LOOPBACK_EXCHANGE = ConfigurationManager.ConnectionStrings["ctLoopbackExchange"].ConnectionString;
        private static readonly string CT_LOOPBACK_QUEUE = ConfigurationManager.ConnectionStrings["ctLoopbackQueue"].ConnectionString;
        private static readonly string[] RABBIT_NAMES = new[] { CT_EXCHANGE_1, CT_EXCHANGE_2, CT_LOOPBACK_EXCHANGE, CT_LOOPBACK_QUEUE };

        private static readonly string DEBUG_CT_EXCHANGE_1_QUEUE = CT_EXCHANGE_1 + "-to-console";
        private static readonly string DEBUG_CT_EXCHANGE_2_QUEUE = CT_EXCHANGE_2 + "-to-console";
        private static readonly string SETUP_MODEL = "RABBIT_SETUP";

        public static void Main(string[] args)
        {
            var containerBuilder = new ContainerBuilder();

            RegisterDB(containerBuilder);
            RegisterRabbit(containerBuilder);
            RegisterChangeTracker(containerBuilder);

            var container = containerBuilder.Build();

            SetupRabbit(container);

            var tracker = new ChangeTracker(CT_LOOPBACK_EXCHANGE, CT_LOOPBACK_QUEUE, container);

            RunDebug(container);
            RunDebugGenerator(container, tracker);

            Process.GetCurrentProcess().WaitForExit();
        }

        private static void SetupRabbit(IContainer container)
        {
            var model = container.ResolveNamed<IModel>(SETUP_MODEL);

            //output
            model.ExchangeDeclare(CT_EXCHANGE_1, "direct", true, false, null);
            model.ExchangeDeclare(CT_EXCHANGE_2, "direct", true, false, null);

            //loppback
            model.ExchangeDeclare(CT_LOOPBACK_EXCHANGE, "direct", true, false, null);
            model.QueueDeclare(CT_LOOPBACK_QUEUE, true, false, false, null);
            model.QueueBind(CT_LOOPBACK_QUEUE, CT_LOOPBACK_EXCHANGE, "", null);

            //debug drainers
            model.QueueDeclare(DEBUG_CT_EXCHANGE_1_QUEUE, true, false, false, null);
            model.QueueDeclare(DEBUG_CT_EXCHANGE_2_QUEUE, true, false, false, null);

            model.QueueBind(DEBUG_CT_EXCHANGE_1_QUEUE, CT_EXCHANGE_1, "", null);
            model.QueueBind(DEBUG_CT_EXCHANGE_2_QUEUE, CT_EXCHANGE_2, "", null);

            Console.WriteLine("SetupRabbit done");
        }

        private static void RunDebug(IContainer container)
        {
            var connection = container.Resolve<IConnection>();

            var model1 = connection.CreateModel();
            var consoleConsumer1 = new EventingBasicConsumer(model1);
            consoleConsumer1.Received += (s, o) => WriteMessageToConsole(o);
            model1.BasicConsume(DEBUG_CT_EXCHANGE_1_QUEUE, true, consoleConsumer1);

            var model2 = connection.CreateModel();
            var consoleConsumer2 = new EventingBasicConsumer(model2);
            consoleConsumer2.Received += (s, o) => WriteMessageToConsole(o);
            model2.BasicConsume(DEBUG_CT_EXCHANGE_2_QUEUE, true, consoleConsumer2);

            Console.WriteLine("RunDebug done");
        }

        private static void RunDebugGenerator(IContainer container, ChangeTracker tracker)
        {
            Console.WriteLine("RunDebugGenerator done");
            using (var context = container.Resolve<SourceContext>())
            {
                var rnd = new Random((int)DateTime.UtcNow.Ticks);
                while (true)
                {
                    var entity = context.SomeEntities.Add(new SomeEntity()
                    {
                        Int32 = rnd.Next(0, 1024),
                        Int64 = rnd.Next(0, 1024),
                        Guid = Guid.NewGuid(),
                        ShortString = new string(Enumerable.Range(0, 128).Select(e => (char)rnd.Next('A', 'Z')).ToArray()),
                        MaxString = new string(Enumerable.Range(0, 128).Select(e => (char)rnd.Next('A', 'Z')).ToArray())
                    });
                    context.SaveChanges();
                    Console.WriteLine($"Add entity with id {entity.Id}");
                    tracker.NotifyEntityChanged<SomeEntity>(entity.Id);
                    Console.WriteLine($"Notified for entity with id {entity.Id}");
                    Thread.Sleep(1000);
                }
            }
        }

        private static readonly object LOCK = new object();
        private static void WriteMessageToConsole(BasicDeliverEventArgs args)
        {
            lock (LOCK)
            {
                var msg = Encoding.Unicode.GetString(args.Body);
                Console.WriteLine(JObject.Parse(msg).ToString(Formatting.Indented));
                Console.WriteLine("---------------");
            }
        }

        private static void RegisterRabbit(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register(e => new ConnectionFactory() { Uri = RABBIT_URI })
                .As<IConnectionFactory>()
                .SingleInstance();

            containerBuilder.Register(e => e.Resolve<IConnectionFactory>().CreateConnection())
                .As<IConnection>()
                .SingleInstance();

            foreach (var name in RABBIT_NAMES)
            {
                containerBuilder.Register(e => e.Resolve<IConnection>().CreateModel())
                    .Named<IModel>(name)
                    .InstancePerLifetimeScope();
            }

            containerBuilder.Register(e => e.Resolve<IConnection>().CreateModel())
                    .Named<IModel>(SETUP_MODEL)
                    .SingleInstance();

            containerBuilder.Register(e =>
            {
                var consumer = new EventingBasicConsumer(e.ResolveNamed<IModel>(CT_LOOPBACK_QUEUE));
                consumer.Received += (sender, obj) => { };
                return consumer;
            })
            .As<EventingBasicConsumer>()
            .SingleInstance();
        }

        private static void RegisterChangeTracker(ContainerBuilder containerBuilder)
        {
            var changeTrackerBuilder = new ChangeTrackerBuilder(containerBuilder);
            var entity = changeTrackerBuilder.Entity<SomeEntity>().FromContext<SourceContext>();
            var entityMapping1 = entity.Map(e => new
            {
                Id = e.Id,
                Guid = e.Guid,
                Int32 = e.Int32,
                Int64 = e.Int64,
                YYY = e.MaxString,
                XXX = e.ShortString
            });
            var entityMapping2 = entity.Map(e => new
            {
                Id = e.Id,
                Guid = e.Guid,
                YYY = e.MaxString,
                Complex = new { e.Int32, e.Int64 }
            });

            changeTrackerBuilder.RegisterEntityDestination(entityMapping1, CT_EXCHANGE_1, false);
            changeTrackerBuilder.RegisterEntityDestination(entityMapping1, CT_EXCHANGE_2, true);

            changeTrackerBuilder.RegisterEntityDestination(entityMapping2, CT_EXCHANGE_2, true);
        }

        private static void RegisterDB(ContainerBuilder containerBuilder)
        {
            var targetDBConnectionString = ConfigurationManager.ConnectionStrings["targetDB"].ConnectionString;
            var sourceDBConnectionString = ConfigurationManager.ConnectionStrings["sourceDB"].ConnectionString;
            containerBuilder.Register(e => new SourceContext(sourceDBConnectionString)).As<SourceContext>();
        }
    }
}
