﻿using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Producer;

namespace IntegrationServiceTests.FakeImpl
{
    class RespondBusFake : IBus
    {
        private Func<object, object> _responder;

        public TResponse FakeSend<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            return (TResponse)_responder(request);
        }
        
        public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
            where TRequest : class
            where TResponse : class
        {
            _responder = new Func<object, object>((q) => responder((TRequest)q));
            return null;
        }

        public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder, Action<IResponderConfiguration> configure)
            where TRequest : class
            where TResponse : class
        {
            return this.Respond<TRequest, TResponse>(responder);
        }

        public IDisposable RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class
        {
            return this.Respond<TRequest, TResponse>(e => responder(e).Result);
        }

        public IDisposable RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, Action<IResponderConfiguration> configure)
            where TRequest : class
            where TResponse : class
        {
            return this.Respond<TRequest, TResponse>(e => responder(e).Result);
        }


        #region NOT USED

        public IAdvancedBus Advanced
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(T message) where T : class
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(T message, string topic) where T : class
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(T message, Action<IPublishConfiguration> configure) where T : class
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(T message) where T : class
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(T message, string topic) where T : class
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(T message, Action<IPublishConfiguration> configure) where T : class
        {
            throw new NotImplementedException();
        }

        public IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers)
        {
            throw new NotImplementedException();
        }

        public IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers, Action<IConsumerConfiguration> configure)
        {
            throw new NotImplementedException();
        }

        public IDisposable Receive<T>(string queue, Func<T, Task> onMessage) where T : class
        {
            throw new NotImplementedException();
        }

        public IDisposable Receive<T>(string queue, Action<T> onMessage) where T : class
        {
            throw new NotImplementedException();
        }

        public IDisposable Receive<T>(string queue, Func<T, Task> onMessage, Action<IConsumerConfiguration> configure) where T : class
        {
            throw new NotImplementedException();
        }

        public IDisposable Receive<T>(string queue, Action<T> onMessage, Action<IConsumerConfiguration> configure) where T : class
        {
            throw new NotImplementedException();
        }

        public TResponse Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            throw new NotImplementedException();
        }

        public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            throw new NotImplementedException();
        }


        public void Send<T>(string queue, T message) where T : class
        {
            throw new NotImplementedException();
        }

        public Task SendAsync<T>(string queue, T message) where T : class
        {
            throw new NotImplementedException();
        }

        public ISubscriptionResult Subscribe<T>(string subscriptionId, Action<T> onMessage) where T : class
        {
            throw new NotImplementedException();
        }

        public ISubscriptionResult Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration> configure) where T : class
        {
            throw new NotImplementedException();
        }

        public ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage) where T : class
        {
            throw new NotImplementedException();
        }

        public ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration> configure) where T : class
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
