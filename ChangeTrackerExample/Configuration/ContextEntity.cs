using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
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
        internal MappedContextEntity<TSourceContext, TSource, TTarget> Map<TTarget>(string name, Expression<Func<TSource, TTarget>> mapper)
            where TTarget : class
        {
            return new MappedContextEntity<TSourceContext, TSource, TTarget>(name, mapper);
        }
    }
}
