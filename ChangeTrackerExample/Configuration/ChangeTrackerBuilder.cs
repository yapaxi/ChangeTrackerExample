using Autofac;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeTrackerExample.DAL.Contexts;
using EasyNetQ.Topology;

namespace ChangeTrackerExample.Configuration
{
    public class EntityBuilder
    {
        private readonly ContainerBuilder _builder;
        private readonly List<EntityConfig> _configs;

        public EntityBuilder(ContainerBuilder builder)
        {
            _builder = builder;
            _configs = new List<EntityConfig>();
        }

        public RegisteredEntity<TSource> Entity<TSource>()
            where TSource : class, IEntity
        {
            return new RegisteredEntity<TSource>();
        }

        public void RegisterDestination(
            IBoundedMappedEntity entitySource,
            IExchange exchange,
            bool allowComplexObjects
        )
        {
            if (!allowComplexObjects && entitySource.TargetTypeSchema.Any(e => e.Children.Any()))
            {
                throw new Exception($"Complex objects are not allowed for echange {exchange.Name}");
            }

            _configs.Add(new EntityConfig(entitySource, exchange));
        }

        public void Build()
        {
            _builder.RegisterInstance(new EntityGroupedConfig(_configs));
        }
    }
}
