using Autofac;
using ChangeTrackerExample.Configuration;
using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App
{
    public class ChangeTracker
    {
        private const string TYPE_HEADER = "type";
        private readonly string _ctLoopbackExchange;
        private readonly IContainer _container;
        private readonly Dictionary<string, EntityConfig[]> _entities;
        private readonly EventingBasicConsumer _loopbackConsumer;

        public ChangeTracker(
            string ctLoopbackExchange,
            string ctLoopbackQueue,
            IContainer container)
        {
            _ctLoopbackExchange = ctLoopbackExchange;
            _container = container;
            _entities = _container
                .Resolve<IEnumerable<EntityConfig>>()
                .GroupBy(e => e.Entity.SourceType.FullName)
                .ToDictionary(e => e.Key, e => e.ToArray());

            var loopbackModel = _container.ResolveNamed<IModel>(ctLoopbackQueue);
            _loopbackConsumer = new EventingBasicConsumer(loopbackModel);
            _loopbackConsumer.Received += (sender, obj) => HandleEntityChangedMessage(obj, loopbackModel);
            loopbackModel.BasicConsume(ctLoopbackQueue, false, _loopbackConsumer);

            Console.WriteLine($"Changed tracked run with loopback {ctLoopbackExchange}->{ctLoopbackQueue} and {_entities.Count} entities to {_entities.Sum(e => e.Value.Count())} destinations");
            foreach (var e in _entities)
            {
                Console.WriteLine("Entity: " + e.Key);
                foreach (var x in e.Value)
                {
                    Console.WriteLine($"\tto {x.TargetExchangeFQN}");
                }
            }
        }

        public void NotifyEntityChanged<TSource>(int id)
            where TSource : IEntity
        {
            using (var lifetimeScope = _container.BeginLifetimeScope())
            {
                var loopback = lifetimeScope.ResolveNamed<IModel>(_ctLoopbackExchange);
                var properties = loopback.CreateBasicProperties();
                properties.ContentType = "application/octet-stream";
                properties.DeliveryMode = 2;
                properties.Headers = new Dictionary<string, object>();
                properties.Headers[TYPE_HEADER] = typeof(TSource).FullName;
                loopback.BasicPublish(_ctLoopbackExchange, "", properties, BitConverter.GetBytes(id));
            }
        }

        private void HandleEntityChangedMessage(BasicDeliverEventArgs obj, IModel loopbackModel)
        {
            var type = Encoding.UTF8.GetString((byte[])obj.BasicProperties.Headers[TYPE_HEADER]);
            HandleEntityChanged(type, BitConverter.ToInt32(obj.Body, 0));
            loopbackModel.BasicAck(obj.DeliveryTag, false);
        }

        private void HandleEntityChanged(string entityTypeFullName, int id)
        {
            Console.WriteLine($"Received changed entity notification {id}");
            using (var lifetimeScope = _container.BeginLifetimeScope())
            {
                var configs = GetConfigurationOrFail(entityTypeFullName);
                foreach (var config in configs)
                {
                    var context = (IEntityContext)lifetimeScope.Resolve(config.Entity.ContextType);
                    var mappedEntity = config.Entity.GetAndMapById(context, id);

                    if (mappedEntity == null)
                    {
                        throw new Exception($"Entity \"{config.Entity.SourceType}\" with id {id} received from {config.TargetExchangeFQN} not found in context {config.Entity.ContextType}");
                    }

                    var json = JsonConvert.SerializeObject(mappedEntity);

                    var queueModel = lifetimeScope.ResolveNamed<IModel>(config.TargetExchangeFQN);
                    var properties = queueModel.CreateBasicProperties();
                    properties.ContentType = "application/json";
                    properties.DeliveryMode = 2;
                    properties.Headers = new Dictionary<string, object>();
                    properties.Headers["schema"] = JsonConvert.SerializeObject(config.Entity.TargetTypeSchema); 

                    var bytes = GetBytes(json);
                    queueModel.BasicPublish(config.TargetExchangeFQN, "", properties, bytes);
                }
            }
        }


        private EntityConfig[] GetConfigurationOrFail(string entityTypeFullName)
        {
            EntityConfig[] entities;
            if (!_entities.TryGetValue(entityTypeFullName, out entities))
            {
                throw new Exception($"Entity for type \"{entityTypeFullName}\" is not expected: destination not found");
            }
            return entities;
        }

        private byte[] GetBytes(string str)
        {
            var arr = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, arr, 0, arr.Length);
            return arr;
        }
    }
}
