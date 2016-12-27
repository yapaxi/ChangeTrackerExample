using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class RegisteredEntity<TSource>
        where TSource : class, IEntity
    {
        internal RegisteredEntity()
        {

        }

        public MappedEntity<TSource, TTarget> Map<TTarget>(Expression<Func<TSource, TTarget>> mapper)
        {
            return new MappedEntity<TSource, TTarget>(mapper);
        }
    }
}
