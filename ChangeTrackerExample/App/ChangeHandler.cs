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

                var properties = CreateProperties(config, id, deliveryMode: 2);
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
                var globalRange = config.Entity.GetEntityRanges(_context);
                var ranges = Split(globalRange, 1000);

                foreach (var range in ranges)
                {
                    var mappedEntities = config.Entity.GetAndMapByRange(_context, range.MinId, range.MaxId);

                    if (!mappedEntities.Any())
                    {
                        return;
                    }

                    var properties = CreateProperties(config, -1, deliveryMode: 1);
                    var bytes = GetEntityBytes(mappedEntities);

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

        private IEnumerable<EntityRange> Split(EntityRange range, int count)
        {
            if (count >= range.Length)
            {
                yield return range;
                yield break;
            }

            var min = range.MinId;
            while (min <= range.MaxId)
            {
                yield return new EntityRange(min, min + count);
                min += count + 1;
            }
        }

        private static MessageProperties CreateProperties(EntityConfig config, int id, byte deliveryMode)
        {
            var properties = new MessageProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = deliveryMode;
            properties.Headers = new Dictionary<string, object>();
            properties.Headers[ISMessageHeader.SCHEMA_CHECKSUM] = config.Entity.MappingSchema.Checksum;
            properties.Headers[ISMessageHeader.SCHEMA_ENTITY_ID] = id;
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
