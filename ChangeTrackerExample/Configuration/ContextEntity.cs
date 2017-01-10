using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class ContextEntity<TSourceContext, TSource>
        where TSourceContext : IEntityContext
        where TSource : class, IEntity
    {
        public Type Parent { get; }

        internal ContextEntity()
        {

        }
        internal ContextEntity(Type source)
        {
            Parent = source;
        }

        internal EntityRoot<TSourceContext, TSource, TTarget> SelectRoot<TTarget>(Expression<Func<TSource, TTarget>> mapper)
            where TTarget : class
        {
            return new EntityRoot<TSourceContext, TSource, TTarget>(mapper);
        }
    }
}
