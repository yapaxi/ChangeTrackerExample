using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;
using RabbitMQ.Client.Events;
using System.IO;

namespace IntegrationServiceTests.FakeImpl
{
    class ConsumerBusFake : IAdvancedBus
    {
        private Action<byte[], MessageProperties, MessageReceivedInfo> _consumer;

        public ConsumerBusFake()
        {

        }

        public void Send(byte[] data, MessageProperties props, MessageReceivedInfo info)
        {
            _consumer(data, props, info);
        }

        public IDisposable Consume(IQueue queue, Action<byte[], MessageProperties, MessageReceivedInfo> onMessage)
        {
            _consumer = onMessage;
            return new MemoryStream();
        }

        public IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {
            throw new NotImplementedException();
        }

        public IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers)
        {
            throw new NotImplementedException();
        }

        public IDisposable Consume(IQueue queue, Action<byte[], MessageProperties, MessageReceivedInfo> onMessage, Action<IConsumerConfiguration> configure)
        {
            throw new NotImplementedException();
        }

        public IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure)
        {
            throw new NotImplementedException();
        }

        public IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers, Action<IConsumerConfiguration> configure)
        {
            throw new NotImplementedException();
        }

        public IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage) where T : class
        {
            throw new NotImplementedException();
        }

        public IDisposable Consume<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage) where T : class
        {
            throw new NotImplementedException();
        }

        public IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure) where T : class
        {
            throw new NotImplementedException();
        }

        public IDisposable Consume<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage, Action<IConsumerConfiguration> configure) where T : class
        {
            throw new NotImplementedException();
        }

        #region REST 
        public IContainer Container
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IConventions Conventions
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

        public event EventHandler<ConnectionBlockedEventArgs> Blocked;
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<MessageReturnedEventArgs> MessageReturned;
        public event EventHandler Unblocked;

        public IBinding Bind(IExchange source, IExchange destination, string routingKey)
        {
            throw new NotImplementedException();
        }

        public IBinding Bind(IExchange exchange, IQueue queue, string routingKey)
        {
            throw new NotImplementedException();
        }

        public Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey)
        {
            throw new NotImplementedException();
        }

        public Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey)
        {
            throw new NotImplementedException();
        }

        public void BindingDelete(IBinding binding)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IExchange ExchangeDeclare(string name, string type, bool passive = false, bool durable = true, bool autoDelete = false, bool @internal = false, string alternateExchange = null, bool delayed = false)
        {
            throw new NotImplementedException();
        }

        public Task<IExchange> ExchangeDeclareAsync(string name, string type, bool passive = false, bool durable = true, bool autoDelete = false, bool @internal = false, string alternateExchange = null, bool delayed = false)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDelete(IExchange exchange, bool ifUnused = false)
        {
            throw new NotImplementedException();
        }

        public IBasicGetResult Get(IQueue queue)
        {
            throw new NotImplementedException();
        }

        public IBasicGetResult<T> Get<T>(IQueue queue) where T : class
        {
            throw new NotImplementedException();
        }

        public uint MessageCount(IQueue queue)
        {
            throw new NotImplementedException();
        }

        public void Publish(IExchange exchange, string routingKey, bool mandatory, MessageProperties messageProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(IExchange exchange, string routingKey, bool mandatory, IMessage<T> message) where T : class
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync(IExchange exchange, string routingKey, bool mandatory, IMessage message)
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync(IExchange exchange, string routingKey, bool mandatory, MessageProperties messageProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(IExchange exchange, string routingKey, bool mandatory, IMessage<T> message) where T : class
        {
            throw new NotImplementedException();
        }

        public IQueue QueueDeclare()
        {
            throw new NotImplementedException();
        }

        public IQueue QueueDeclare(string name, bool passive = false, bool durable = true, bool exclusive = false, bool autoDelete = false, int? perQueueMessageTtl = default(int?), int? expires = default(int?), int? maxPriority = default(int?), string deadLetterExchange = null, string deadLetterRoutingKey = null, int? maxLength = default(int?), int? maxLengthBytes = default(int?))
        {
            throw new NotImplementedException();
        }

        public Task<IQueue> QueueDeclareAsync(string name, bool passive = false, bool durable = true, bool exclusive = false, bool autoDelete = false, int? perQueueMessageTtl = default(int?), int? expires = default(int?), int? maxPriority = default(int?), string deadLetterExchange = null, string deadLetterRoutingKey = null, int? maxLength = default(int?), int? maxLengthBytes = default(int?))
        {
            throw new NotImplementedException();
        }

        public void QueueDelete(IQueue queue, bool ifUnused = false, bool ifEmpty = false)
        {
            throw new NotImplementedException();
        }

        public void QueuePurge(IQueue queue)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
