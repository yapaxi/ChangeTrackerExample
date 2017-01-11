using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL
{
    public abstract class RepositoryBase<TContext>
        where TContext : DbContext
    {
        protected readonly TContext Context;

        public RepositoryBase(TContext context)
        {
            Context = context;
        }

        public DbContextTransaction BeginTransaction()
        {
            return Context.Database.BeginTransaction();
        }

        public DbContextTransaction BeginTransaction(IsolationLevel level)
        {
            return Context.Database.BeginTransaction(level);
        }

        public void SaveChanges()
        {
            Context.SaveChanges();
        }
    }
}
