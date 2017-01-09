using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL.Contexts
{
    public class DataContext : DbContext
    {
        public DataContext(string connectionString)
            : base (connectionString)
        {

        }
    }
}
