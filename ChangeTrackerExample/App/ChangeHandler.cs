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
            var configs = GetConfigurationOrFail(entityTypeFullName);
            foreach (var config in configs)
            {
                var mappedEntity = config.Entity.GetAndMapById(_context, id);

                if (mappedEntity == null)
                {
                    throw new Exception($"Entity \"{config.Entity.SourceType}\" with id {id} received from {config.DestinationConfig} not found in context {config.Entity.ContextType}");
                }

                var properties = CreateDefaultProperties(config, deliveryMode: 2);
                properties.Headers[ISMessageHeader.ENTITY_COUNT] = 1;

                var bytes = GetEntityBytes(new[] { mappedEntity });
                _bus.Advanced.Publish(config.DestinationExchange, "", false, properties, bytes);
                Console.WriteLine($"Sent to destination: {id}");
            }
        }

        public void HandleEntityFullRebuild(string entityTypeFullName)
        {
            var configs = GetConfigurationOrFail(entityTypeFullName);
            foreach (var config in configs)
            {
                var items = 2000;
                var ranges = config.Entity.GetEntityRanges(_context, items);

                Console.WriteLine($"Found {ranges.Length} ranges, {items} items each");

                for (var i = 0; i < ranges.Length; i++)
                {
                    var range = ranges[i];
                    var mappedEntities = config.Entity.GetAndMapByRange(_context, range.MinId, range.MaxId);

                    if (!mappedEntities.Any())
                    {
                        return;
                    }

                    var properties = CreateDefaultProperties(config, deliveryMode: 1);
                    properties.Headers[ISMessageHeader.ENTITY_COUNT] = mappedEntities.Count;
                    properties.Headers[ISMessageHeader.BATCH_IS_LAST] = i == ranges.Length - 1;
                    properties.Headers[ISMessageHeader.BATCH_ORDINAL] = i;
                    properties.Headers[ISMessageHeader.BATCH_COUNT] = ranges.Length;

                    var bytes = GetEntityBytes(mappedEntities);

                    Console.WriteLine($"Sending range {i}: {range.MinId} -> {range.MaxId}");

                    _bus.Advanced.Publish(config.DestinationExchange, "", false, properties, bytes);

                    Console.WriteLine($"Sent {mappedEntities.Count} entities to destination:");
                }
            }
        }

        private byte[] GetEntityBytes(object mappedEntity)
        {
            var json = JsonConvert.SerializeObject(mappedEntity, Formatting.None);
            var bytes = GetBytes(json);
            return bytes;
        }
        
        private static MessageProperties CreateDefaultProperties(EntityConfig config, byte deliveryMode)
        {
            var properties = new MessageProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = deliveryMode;
            properties.Headers = new Dictionary<string, object>();
            properties.Headers[ISMessageHeader.SCHEMA_CHECKSUM] = config.Entity.MappingSchema.Checksum;
            return properties;
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
