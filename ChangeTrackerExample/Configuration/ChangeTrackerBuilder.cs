using Autofac;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeTrackerExample.DAL.Contexts;
using EasyNetQ.Topology;
using Common;

namespace ChangeTrackerExample.Configuration
{
    public class EntityBuilder
    {
        private readonly ContainerBuilder _builder;
        private readonly List<EntityConfig> _entities;

        public EntityBuilder(ContainerBuilder containerBuilder)
        {
            _builder = containerBuilder;
            _entities = new List<EntityConfig>();
        }

        public RegisteredEntity<TSource> Entity<TSource>()
            where TSource : class, IEntity
        {
            return new RegisteredEntity<TSource>();
        }

        public DestinationConfig DestinationRoot(string rootNamespace)
        {
            return new DestinationConfig(rootNamespace);
        }

        public void MapEntityToDestination(IBoundedMappedEntity entitySource, PrefixedDestinationConfig destination)
        {
            RecursiveEnsureForeignKeysExists(entitySource.MappingSchema.Properties, entitySource);
            _entities.Add(new EntityConfig(entitySource, destination));
        }

        private static void RecursiveEnsureForeignKeysExists(MappingProperty[] properties, IBoundedMappedEntity entitySource)
        {
            foreach (var v in properties.Where(e => e.Children.Any()))
            {
                if (!entitySource.Children.ContainsKey(v.PathName))
                {
                    throw new Exception($"Complex objects \"{v.ShortName}\" on entity \"{entitySource.ShortName}\" is not configured (use \"WithChild\" method)");
                }

                RecursiveEnsureForeignKeysExists(v.Children, entitySource);
            }
        }

        public void Build()
        {
            var dst = _entities.Select(e => e.DestinationConfig);

            var duplicates = string.Join(", ", dst.GroupBy(e => e).Where(e => e.Count() > 1).Select(e => e.Key.ToString()));

            if (!string.IsNullOrWhiteSpace(duplicates))
            {
                throw new Exception($"Destinations \"{duplicates}\" violate following rules: one destination = one mapping");
            }

            _entities.ForEach(e => _builder.RegisterInstance(e));
            _builder.RegisterInstance(new EntityGroupedConfig(_entities));
        }
    }
}
