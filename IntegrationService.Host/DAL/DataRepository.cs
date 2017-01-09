using IntegrationService.Host.DAL.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL
{
    public class DataRepository
    {
        private readonly DataContext _dataContext;

        public DataRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public void Insert(string tableName, SqlParameter[] parameters)
        {
            var columns = string.Join(",", parameters.Select(e => e.ParameterName).ToArray());
            var prs = string.Join(",", parameters.Select(e => "@" + e.ParameterName).ToArray());
            _dataContext.Database.ExecuteSqlCommand($"insert into {tableName} ({columns}) values ({prs})", parameters);
        }
    }
}
