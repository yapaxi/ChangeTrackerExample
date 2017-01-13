using IntegrationService.Host.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL.Contexts
{
    public class SchemaContext : DbContext
    {
        public SchemaContext(string connectionString)
            : base(connectionString)
        {

        }

        public DbSet<Mapping> Mappings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
