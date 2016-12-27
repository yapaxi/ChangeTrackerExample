using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class EntityConfig
    {
        internal EntityConfig(IBoundedMappedEntity entity, string targetExchange)
        {
            Entity = entity;
            TargetExchangeFQN = targetExchange;
        }

        public IBoundedMappedEntity Entity { get; }
        public string TargetExchangeFQN { get; }
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
