using Autofac;
using ChangeTrackerExample.App;
using ChangeTrackerExample.Configuration;
using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using Common;
using EasyNetQ;
using EasyNetQ.Topology;
using IntegrationService.Client;
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
        public static void Main(string[] args)
        {
            var rootScope = "root";
            var trackerLoopbackExchange = ConfigurationManager.ConnectionStrings["CTLoopbackExchange"].ConnectionString;
            var trackerLoopbackQueue = ConfigurationManager.ConnectionStrings["CTLoopbackQueue"].ConnectionString;
            
            var containerBuilder = new ContainerBuilder();

            var module = new RabbitAutofacModule(
                busResolveScope: rootScope,
                loopbackVHost: "ChangeTrackerExample"
            );

            containerBuilder.RegisterModule(module);

            RegisterDB(containerBuilder);
            RegisterIS(containerBuilder);
            RegisterEntities(containerBuilder);
            RegisterHandlers(trackerLoopbackExchange, trackerLoopbackQueue, containerBuilder);

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope(rootScope))
            {
                scope.Resolve<ISSynchronizer>().Start();

                var rabcom = new RabbitCommunicationModelBuilder(scope.ResolveNamed<IBus>(Buses.Messaging).Advanced);

                BuildTrackerLoopback(
                    scope.ResolveNamed<IBus>(Buses.Loopback).Advanced,
                    trackerLoopbackExchange,
                    trackerLoopbackQueue);

                foreach (var config in scope.Resolve<IEnumerable<EntityConfig>>())
                {
                    rabcom.BuildTrackerToISContract(config.DestinationExchange.Name, config.DestinationQueue.Name);
                }
                
                RunLoopbackListener(scope);
                RunBlockingDebugGenerator(scope);
            }
        }

        private static void BuildTrackerLoopback(IAdvancedBus bus, string trackerLoopbackExchange, string trackerLoopbackQueue)
        {
            var e = bus.ExchangeDeclare(trackerLoopbackExchange, "direct", durable: true);
            var q = bus.QueueDeclare(trackerLoopbackQueue, durable: true);
            bus.Bind(e, q, "");
        }

        private static void RegisterIS(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register(e =>
            {
                var sync = new ISSynchronizer(e.Resolve<ISClient>(), e.Resolve<IEnumerable<EntityConfig>>());
                sync.OnSyncSucceeded += (s, o) => Console.WriteLine("Meta sync success");
                sync.OnSyncFailed += (s, o) => Console.WriteLine("Meta sync failed");
                sync.OnQueueFailed += (s, o) => Console.WriteLine("Meta sync queue failed");
                return sync;
            }).SingleInstance();
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
                    var entity = context.SomeEntities.Add(new SomeEntity()
                    {
                        Int32 = rnd.Next(0, 1024),
                        Int64 = rnd.Next(0, 1024),
                        Guid = Guid.NewGuid(),
                        ShortString = new string(range.Select(e => (char)rnd.Next('A', 'Z')).ToArray()),
                        MaxString = new string(range.Select(e => (char)rnd.Next('A', 'Z')).ToArray()),
                        Lines = new List<Line>(),
                        SuperLines = new List<SuperLine>()
                    });

                    entity.Lines.Add(new Line() { String = new string(range.Select(e => (char)rnd.Next('A', 'Z')).ToArray()) });
                    entity.Lines.Add(new Line() { String = new string(range.Select(e => (char)rnd.Next('A', 'Z')).ToArray()) });
                    entity.SuperLines.Add(new SuperLine() { SuperString = new string(range.Select(e => (char)rnd.Next('A', 'Z')).ToArray()) });
                    entity.SuperLines.Add(new SuperLine() { SuperString = new string(range.Select(e => (char)rnd.Next('A', 'Z')).ToArray()) });

                    context.SaveChanges();
                    Console.WriteLine($"Generated: {entity.Id}");

                    //Thread.Sleep(1000);
                    notifier.NotifyChanged<SomeEntity>(entity.Id);
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
                bus: e.ResolveNamed<IBus>(Buses.Loopback),
                queue: new Queue(loopbackQueue, false)
            )).InstancePerLifetimeScope();
            
            containerBuilder.Register(e => new ChangeHandler(
                config: e.Resolve<EntityGroupedConfig>(),
                context: e.Resolve<SourceContext>(),
                bus: e.ResolveNamed<IBus>(Buses.Messaging)
            )).InstancePerLifetimeScope();

            containerBuilder.Register(e => new LoopbackNotifier(
                bus: e.ResolveNamed<IBus>(Buses.Loopback),
                exchange: new Exchange(loopbackExchange)
            ));
        }
        
        private static void RegisterEntities(ContainerBuilder containerBuilder)
        {
            var entityBuilder = new EntityBuilder(containerBuilder);

            var entity = entityBuilder
                    .Entity<SomeEntity>()
                    .FromContext<SourceContext>();

            var root = entity.SelectRoot(e => new
            {
                Id = e.Id,
                e.Guid,
                Z = e.Int32*1235,
                Lines = e.Lines,
                Object = new
                {
                    Id = e.Id,
                    EntityId = e.Id,
                    X = 1,
                    GGGHH = 34343
                },
                SuperLines = e.SuperLines.Select(z => new
                {
                    Id = z.Id,
                    CoolForeignKey = z.EntityId,
                    MegaString = z.SuperString,
                    hhh = 3453454,
                })
            })
            .WithChild(e => e.Lines, e => e.EntityId)
            .WithChild(e => e.Object, e => e.EntityId)
            .WithChild(e => e.SuperLines, e => e.CoolForeignKey);

            var mappedEntity = root.Named("some-entity");
            
            var reporting = entityBuilder.DestinationRoot("example-ms").Prefixed("reporting");

            entityBuilder.MapEntityToDestination(mappedEntity, reporting);

            entityBuilder.Build();
        }
    }
}
