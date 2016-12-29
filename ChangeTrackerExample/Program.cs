using Autofac;
using ChangeTrackerExample.App;
using ChangeTrackerExample.Configuration;
using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using EasyNetQ;
using EasyNetQ.Topology;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitModel;
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

        private static readonly string IS_EXCHANGE_1 = ConfigurationManager.ConnectionStrings["ISExchange1"].ConnectionString;
        private static readonly string IS_EXCHANGE_2 = ConfigurationManager.ConnectionStrings["ISExchange2"].ConnectionString;
        private static readonly string IS_QUEUE_1 = ConfigurationManager.ConnectionStrings["ISQueue1"].ConnectionString;
        private static readonly string IS_QUEUE_2 = ConfigurationManager.ConnectionStrings["ISQueue2"].ConnectionString;

        public static void Main(string[] args)
        {
            var trackerLoopbackExchange = ConfigurationManager.ConnectionStrings["CTLoopbackExchange"].ConnectionString;
            var trackerLoopbackQueue = ConfigurationManager.ConnectionStrings["CTLoopbackQueue"].ConnectionString;

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule(new RabbitAutofacModule(RABBIT_URI));

            RegisterDB(containerBuilder);
            RegisterEntities(new Exchange(IS_EXCHANGE_1), new Exchange(IS_EXCHANGE_2), containerBuilder);
            RegisterHandlers(trackerLoopbackExchange, trackerLoopbackQueue, containerBuilder);

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var rabcom = new RabbitCommunicationModelBuilder(scope.Resolve<IBus>().Advanced);

                rabcom.BuildTrackerLoopback(trackerLoopbackExchange, trackerLoopbackQueue);
                rabcom.BuildTrackerToISContract(IS_EXCHANGE_1, IS_QUEUE_1);
                rabcom.BuildTrackerToISContract(IS_EXCHANGE_2, IS_QUEUE_2);

                RunLoopbackListener(scope);
                RunBlockingDebugGenerator(scope);
            }
        }

        private static void RunBlockingDebugGenerator(ILifetimeScope outerScope)
        {
            const int CNT_PER_BATCH = 1;
            Console.WriteLine("RunDebugGenerator done");

            var notifier = outerScope.Resolve<LoopbackNotifier>();

            using (var innerScope = outerScope.BeginLifetimeScope())
            {
                var context = innerScope.Resolve<SourceContext>();

                var rnd = new Random((int)DateTime.UtcNow.Ticks);
                var range = Enumerable.Range(0, 128).ToArray();
                while (true)
                {
                    var lst = new List<SomeEntity>(CNT_PER_BATCH);
                    for (int i = 0; i < CNT_PER_BATCH; i++)
                    {
                        var entity = context.SomeEntities.Add(new SomeEntity()
                        {
                            Int32 = rnd.Next(0, 1024),
                            Int64 = rnd.Next(0, 1024),
                            Guid = Guid.NewGuid(),
                            ShortString = new string(range.Select(e => (char)rnd.Next('A', 'Z')).ToArray()),
                            MaxString = new string(range.Select(e => (char)rnd.Next('A', 'Z')).ToArray())
                        });
                        lst.Add(entity);
                    }

                    context.SaveChanges();

                    Console.WriteLine($"Sync {lst.Count}");

                    foreach (var entity in lst)
                    {
                        notifier.NotifyChanged<SomeEntity>(entity.Id);
                        Console.WriteLine($"Notified for entity with id {entity.Id}");
                    }

          //          Thread.Sleep(200);
                }

            }
        }

        private static void RunLoopbackListener(ILifetimeScope outerScope)
        {
            var listener = outerScope.Resolve<LoopbackListener>();
            listener.EntityChanged += (s, o) =>
            {
                using (var innerScope = outerScope.BeginLifetimeScope())
                {
                    var handler = innerScope.Resolve<ChangeHandler>();
                    handler.HandleEntityChanged(o.Type, o.Id);
                }
            };
            listener.Start();
        }

        private static void RegisterDB(ContainerBuilder containerBuilder)
        {
            var sourceDBConnectionString = ConfigurationManager.ConnectionStrings["sourceDB"].ConnectionString;
            containerBuilder
                .Register(e => new SourceContext(sourceDBConnectionString))
                .As<SourceContext>()
                .InstancePerLifetimeScope();
        }
        
        private static void RegisterHandlers(string loopbackExchange, string loopbackQueue, ContainerBuilder containerBuilder)
        {
            containerBuilder.Register(e => new LoopbackListener(
                bus: e.Resolve<IBus>(),
                queue: new Queue(loopbackQueue, false)
            )).InstancePerLifetimeScope();
            
            containerBuilder.Register(e => new ChangeHandler(
                config: e.Resolve<EntityGroupedConfig>(),
                context: e.Resolve<SourceContext>(),
                bus: e.Resolve<IBus>()
            )).InstancePerLifetimeScope();

            containerBuilder.Register(e => new LoopbackNotifier(
                bus: e.Resolve<IBus>(),
                exchange: new Exchange(loopbackExchange)
            ));
        }

        private static void RegisterEntities(
            IExchange exchange1,
            IExchange exchange2,
            ContainerBuilder containerBuilder
        )
        {
            var entityBuilder = new EntityBuilder(containerBuilder);
            var entity = entityBuilder.Entity<SomeEntity>().FromContext<SourceContext>();
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

            entityBuilder.RegisterDestination(entityMapping1, exchange1, false);
            entityBuilder.RegisterDestination(entityMapping1, exchange2, true);

            entityBuilder.RegisterDestination(entityMapping2, exchange2, true);

            entityBuilder.Build();
        }
    }
}
