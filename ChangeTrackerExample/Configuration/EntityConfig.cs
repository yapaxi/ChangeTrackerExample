﻿using Common;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class EntityConfig
    {
        internal EntityConfig(IBoundedMappedEntity entity, PrefixedDestinationConfig destinationConfig)
        {
            Entity = entity;
            DestinationConfig = destinationConfig;
            FullName = $"Contracts.IS.Dynamic.{DestinationConfig.RootNamespace}.{DestinationConfig.Prefix}.{Entity.ShortName}";
            DestinationExchange = new Exchange($"{FullName}-A{entity.MappingSchema.Checksum}");
            DestinationQueue = new Queue(DestinationExchange.Name + ".queue", false);
        }

        public string FullName { get; }
        public IBoundedMappedEntity Entity { get; }
        public PrefixedDestinationConfig DestinationConfig { get; }
        public IExchange DestinationExchange { get; }
        public IQueue DestinationQueue { get; }
    }

    public class EntityGroupedConfig
    {
        internal EntityGroupedConfig(IReadOnlyCollection<EntityConfig> entities)
        {
            Entities = entities
                .GroupBy(e => e.Entity.SourceType.FullName)
                .ToDictionary(e => e.Key, e => e.ToArray());
        }

        public IReadOnlyDictionary<string, EntityConfig[]> Entities { get; }
    }
}
