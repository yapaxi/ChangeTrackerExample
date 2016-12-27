using Autofac;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeTrackerExample.DAL.Contexts;

namespace ChangeTrackerExample.Configuration
{
    public class ChangeTrackerBuilder
    {
        private readonly ContainerBuilder _builder;
        private readonly List<EntityConfig> _configs;

        public ChangeTrackerBuilder(ContainerBuilder builder)
        {
            _builder = builder;
            _configs = new List<EntityConfig>();
        }

        public RegisteredEntity<TSource> Entity<TSource>()
            where TSource : class, IEntity
        {
            return new RegisteredEntity<TSource>();
        }

        public void RegisterEntityDestination(
            IBoundedMappedEntity entitySource,
            string targetExchange,
            bool allowComplexObjects
        )
        {
            if (!allowComplexObjects && entitySource.TargetTypeSchema.Any(e => e.Children.Any()))
            {
                throw new Exception($"Complex objects are not allowed for echange {targetExchange}");
            }

            _configs.Add(new EntityConfig(entitySource, targetExchange));
        }

        public void Build()
        {
            _builder.RegisterInstance(new EntityGroupedConfig(_configs));
        }
    }
}
