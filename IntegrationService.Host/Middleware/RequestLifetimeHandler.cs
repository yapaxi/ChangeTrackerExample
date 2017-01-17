using Autofac;
using Common;
using EasyNetQ;
using EasyNetQ.Topology;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Services;
using IntegrationService.Host.Services.Policy;
using IntegrationService.Host.Subscriptions;
using Newtonsoft.Json;
using NLog;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Middleware
{
    public class RequestLifetimeHandler : IRequestLifetimeHandler
    {
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        
        public RequestLifetimeHandler(ILifetimeScope scope, ILogger logger)
        {
            _scope = scope;
            _logger = logger;
        }
        
        public TResponse Response<TRequest, TResponse>(TRequest request)
        {
            return Execute<IRequestResponseService<TRequest, TResponse>, TResponse>(e => e.Response(request));
        }

        public void HandleDataMessage<TMessage>(TMessage message, MessageInfo info)
        {
            Execute<IMessagingService<TMessage>>(e => e.WriteMessage(message, info));
        }

        private void Execute<TService>(Action<TService> action)
        {
            using (var scope = _scope.BeginLifetimeScope())
            {
                var service = scope.Resolve<TService>();
                var serviceType = service.GetType();
                var syncObject = GetSyncObjectForService(serviceType);

                if (syncObject == null)
                {
                    _logger.Debug($"Executing {serviceType.FullName} without synchronization");
                    action(service);
                }
                else
                {
                    _logger.Debug($"Executing {serviceType.FullName} with synchronization");
                    lock (syncObject)
                    {
                        action(service);
                    }
                }
            }
        }
        private TResult Execute<TService, TResult>(Func<TService, TResult> action)
        {
            using (var scope = _scope.BeginLifetimeScope())
            {
                var service = scope.Resolve<TService>();
                var syncObject = GetSyncObjectForService(service.GetType());

                if (syncObject == null)
                {
                    return action(service);
                }
                else
                {
                    lock (syncObject)
                    {
                        return action(service);
                    }
                }
            }
        }

        private object GetSyncObjectForService(Type type)
        {
            return (type.GetCustomAttributes(typeof(SerialExecutionAttribute), true).FirstOrDefault() as SerialExecutionAttribute)?.Lock;
        }
    }
}
