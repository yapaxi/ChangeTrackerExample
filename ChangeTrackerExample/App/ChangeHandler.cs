using Autofac;
using ChangeTrackerExample.Configuration;
using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using EasyNetQ;
using EasyNetQ.NonGeneric;
using EasyNetQ.Topology;
using Newtonsoft.Json;
using RabbitModel;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App
{
    public class ChangeHandler
    {
        private readonly EntityGroupedConfig _config;
        private readonly IEntityContext _context;
        private readonly IBus _bus;

        public ChangeHandler(EntityGroupedConfig config, IEntityContext context, IBus bus)
        {
            _config = config;
            _context = context;
            _bus = bus;
        }
        
        public void HandleEntityChanged(string entityTypeFullName, int id)
        {
            Console.WriteLine($"Received changed entity notification {id}");

            var configs = GetConfigurationOrFail(entityTypeFullName);
            foreach (var config in configs)
            {
                var mappedEntity = config.Entity.GetAndMapById(_context, id);

                if (mappedEntity == null)
                {
                    throw new Exception($"Entity \"{config.Entity.SourceType}\" with id {id} received from {config.DestinationConfig} not found in context {config.Entity.ContextType}");
                }

                var properties = new MessageProperties();
                properties.ContentType = "application/json";
                properties.DeliveryMode = 2;
                properties.Headers = new Dictionary<string, object>();
                properties.Headers[ISMessageHeader.SCHEMA_CHECKSUM] = config.Entity.MappingSchema.Checksum;
                properties.Headers[ISMessageHeader.SCHEMA_ENTITY_ID] = id;

                var json = JsonConvert.SerializeObject(mappedEntity);
                var bytes = GetBytes(json);

                _bus.Advanced.Publish(config.DestinationExchange, "", false, properties, bytes);
            }
        }


        private EntityConfig[] GetConfigurationOrFail(string entityTypeFullName)
        {
            EntityConfig[] entities;
            if (!_config.Entities.TryGetValue(entityTypeFullName, out entities))
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
