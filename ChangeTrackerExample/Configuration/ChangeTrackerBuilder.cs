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

        public ChangeTrackerBuilder(ContainerBuilder builder)
        {
            _builder = builder;
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

            _builder.RegisterInstance(new EntityConfig(entitySource, targetExchange));
        }
    }
}
