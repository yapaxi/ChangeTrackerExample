using Common;
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
            Destination = new Exchange($"Contracts.IS.Dynamic.{DestinationConfig.RootNamespace}.{DestinationConfig.Prefix}.{Entity.Name}");
        }

        public IBoundedMappedEntity Entity { get; }
        public PrefixedDestinationConfig DestinationConfig { get; }
        public Exchange Destination { get; }
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
