using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.DAL.Contexts
{
    public interface IEntityContext : IDisposable
    {
        IQueryable<TEntity> ReadonlyGet<TEntity>() where TEntity : class;
    }
}
