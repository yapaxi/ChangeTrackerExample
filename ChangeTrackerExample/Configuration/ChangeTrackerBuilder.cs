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

        public RegisteredEntity<TSource> RegisterEntity<TSource>()
            where TSource : class, IEntity
        {
            return new RegisteredEntity<TSource>();
        }

        internal void RegisterEntityDestination<TSourceContext, TSource, TTarget>(
            BoundedMappedEntity<TSourceContext, TSource, TTarget> entitySource
        )
            where TSource : class, IEntity
            where TSourceContext : IEntityContext
        {

        }
    }
}
