using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    internal class EntityConfig
    {
        public EntityConfig(IBoundedMappedEntity entity, string targetExchange)
        {
            Entity = entity;
            TargetExchangeFQN = targetExchange;
        }

        public IBoundedMappedEntity Entity { get; }
        public string TargetExchangeFQN { get; }
    }
}
