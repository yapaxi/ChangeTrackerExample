using Autofac;
using ChangeTrackerExample.App;
using ChangeTrackerExample.Configuration;
using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitModel;
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
            RegisterEntities(containerBuilder);
            RegisterHandlers(trackerLoopbackExchange, trackerLoopbackQueue, containerBuilder);

            using (var container = containerBuilder.Build())
            {
                var rabcom = new RabbitCommunicationModelBuilder(container.Resolve<IModel>());

                rabcom.BuildTrackerLoopback(trackerLoopbackExchange, trackerLoopbackQueue);
                rabcom.BuildTrackerToISContract(IS_EXCHANGE_1, IS_QUEUE_1);
                rabcom.BuildTrackerToISContract(IS_EXCHANGE_2, IS_QUEUE_2);

                var listenerTask = RunLoopbackListener(container);

                RunBlockingDebugGenerator(container);

                listenerTask.Wait(5000);
            }
        }

        private static void RunBlockingDebugGenerator(IContainer container)
        {
            const int CNT_PER_BATCH = 1;
            Console.WriteLine("RunDebugGenerator done");

            using (var scope = container.BeginLifetimeScope())
            {
                var context = scope.Resolve<SourceContext>();
                var notifier = scope.Resolve<LoopbackNotifier>();

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
                        while (true)
                        {
                            try
                            {
                                notifier.NotifyChanged<SomeEntity>(entity.Id);
                                break;
                            }
                            catch (Exception e)
                            {
                                var connection = container.Resolve<IConnection>();
                                if (!connection.IsOpen)
                                {
                                    Console.WriteLine(e.Message);
                                    return;
                                }
                                else
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }
                        }
                        Console.WriteLine($"Notified for entity with id {entity.Id}");
                    }

                    Thread.Sleep(200);
                }

            }
        }

        private static Task RunLoopbackListener(IContainer container)
        {
            var cmplSource = new TaskCompletionSource<object>();
            var listener = container.Resolve<LoopbackListener>();
            listener.EntityChanged += (s, o) =>
            {
                using (var scope = container.BeginLifetimeScope())
                {
                    var handler = scope.Resolve<ChangeHandler>();
                    handler.HandleEntityChanged(o.Type, o.Id);
                }
            };
            listener.Cancelled += (s, o) => cmplSource.SetCanceled();
            listener.Start();
            return cmplSource.Task;
        }

        private static void RegisterDB(ContainerBuilder containerBuilder)
        {
            var sourceDBConnectionString = ConfigurationManager.ConnectionStrings["sourceDB"].ConnectionString;
            containerBuilder.Register(e => new SourceContext(sourceDBConnectionString)).As<SourceContext>();
        }
        
        private static void RegisterHandlers(string loopbackExchange, string loopbackQueue, ContainerBuilder containerBuilder)
        {
            containerBuilder.Register(e => new ChangeHandler(
                config: e.Resolve<EntityGroupedConfig>(),
                context: e.Resolve<SourceContext>(),
                outputModel: e.Resolve<IModel>()
            )).InstancePerLifetimeScope();

            containerBuilder.Register(e => new LoopbackNotifier(
                model: e.Resolve<IModel>(),
                loopbackExchange: loopbackExchange
            )).InstancePerLifetimeScope();

            containerBuilder.Register(e => new LoopbackListener(
                loopbackModel: e.Resolve<IModel>(),
                loopbackQueue: loopbackQueue
            )).SingleInstance();
        }

        private static void RegisterEntities(ContainerBuilder containerBuilder)
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

            entityBuilder.RegisterDestination(entityMapping1, IS_EXCHANGE_1, false);
            entityBuilder.RegisterDestination(entityMapping1, IS_EXCHANGE_2, true);

            entityBuilder.RegisterDestination(entityMapping2, IS_EXCHANGE_2, true);

            entityBuilder.Build();
        }
    }
}
