using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class BoundedMappedEntity<TSourceContext, TSource, TTarget>
        where TSource : class, IEntity
        where TSourceContext : IEntityContext
    {

    }
}
