using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class MappedEntity<TSource, TTarget>
        where TSource : class, IEntity
    {
        public Expression<Func<TSource, TTarget>> Mapper { get; }

        internal MappedEntity(Expression<Func<TSource, TTarget>> mapper)
        {
            Mapper = mapper;
        }

        internal Task<TTarget> GetById(IQueryable<TSource> collection, int id)
        {
            return collection.Where(e => e.Id == id).Select(Mapper).FirstOrDefaultAsync();
        }

        internal Task<TTarget[]> GetByRange(IQueryable<TSource> collection, int fromId, int toId)
        {
            return collection.Where(e => e.Id >= fromId && e.Id <= toId).Select(Mapper).ToArrayAsync();
        }

        public BoundedMappedEntity<TSourceContext, TSource, TTarget> FromContext<TSourceContext>()
            where TSourceContext : IEntityContext
        {
            return new BoundedMappedEntity<TSourceContext, TSource, TTarget>();
        }
    }
}
