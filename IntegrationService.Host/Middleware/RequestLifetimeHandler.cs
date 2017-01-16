﻿using Autofac;
using Common;
using EasyNetQ;
using EasyNetQ.Topology;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Services;
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
            try
            {
                using (var scope = _scope.BeginLifetimeScope())
                {
                    return scope.Resolve<IRequestResponseService<TRequest, TResponse>>().Response(request);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }

        public void HandleDataMessage<TMessage>(TMessage message, MessageInfo info)
        {
            try
            {
                using (var scope = _scope.BeginLifetimeScope())
                {
                    scope.Resolve<IMessagingService<TMessage>>().WriteMessage(message, info);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }
    }
}