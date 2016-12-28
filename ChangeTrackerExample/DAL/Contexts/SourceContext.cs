using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.DAL.Contexts
{
    public class SourceContext : DbContext, IEntityContext
    {
        public SourceContext(string connectionString)
            : base(connectionString)
        {

        }

        public IDbSet<SomeEntity> SomeEntities { get; set; }

        public IQueryable<TEntity> Get<TEntity>() where TEntity : class => Set<TEntity>();
    }
}
