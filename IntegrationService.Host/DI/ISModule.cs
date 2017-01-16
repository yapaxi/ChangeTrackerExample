using Autofac;
using EasyNetQ;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.Converters;
using IntegrationService.Host.DAL;
using IntegrationService.Host.DAL.Contexts;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Middleware;
using IntegrationService.Host.Services;
using IntegrationService.Host.Subscriptions;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Core;
using NLog;

namespace IntegrationService.Host.DI
{
    internal class ISModule<TLogger> : LoggerModule<TLogger>
        where TLogger : class
    {
        private readonly string _dataDBConnectionString;
        private readonly string _rootScopeName;
        private readonly string _schemaDBConnectionString;
        
        public ISModule(string schemaDBConnectionString, string dataDBConnectionString, string rootScopeName, ILoggerFactory<TLogger> loggerFactory)
            : base(loggerFactory)
        {
            _rootScopeName = rootScopeName;
            _schemaDBConnectionString = schemaDBConnectionString;
            _dataDBConnectionString = dataDBConnectionString;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule(new RabbitAutofacModule(_rootScopeName));

            builder.Register(e => new SchemaContext(_schemaDBConnectionString));
            builder.Register(e => new DataContext(_dataDBConnectionString));

            builder.RegisterType<SchemaRepository>().InstancePerLifetimeScope();
            builder.RegisterType<DataRepository>().InstancePerLifetimeScope();
            builder.RegisterType<SchemaPersistenceService>().InstancePerLifetimeScope();

            builder.RegisterType<RequestLifetimeHandler>().As<IRequestLifetimeHandler>().SingleInstance();

            builder.Register(e => new SubscriptionManager(
                handler: e.Resolve<IRequestLifetimeHandler>(),
                isBus: e.ResolveNamed<IBus>(Buses.ISSync),
                simpleBus: e.ResolveNamed<IBus>(Buses.SimpleMessaging),
                bulkBus: e.ResolveNamed<IBus>(Buses.BulkMessaging),
                logger: e.Resolve<ILoggerFactory<ILogger>>().CreateForType(typeof(SubscriptionManager)),
                loggerFactory: e.Resolve<ILoggerFactory<ILogger>>()
            )).SingleInstance();

            builder.RegisterType<FlatMessageConverter>().SingleInstance();

            builder.RegisterType<MetadataSyncService>()
                .As<IRequestResponseService<SyncMetadataRequest, SyncMetadataResponse>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<MessagingService>()
                .As<IMessagingService<RawMessage>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<MessagingService>()
                .As<IMessagingService<IReadOnlyCollection<RawMessage>>>()
                .InstancePerLifetimeScope();

            base.Load(builder);
        }
    }
}
