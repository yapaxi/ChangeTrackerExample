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

        public void Insert(string tableName, KeyValuePair<string, object>[] keyValues)
        {
            var columns = string.Join(",", keyValues.Select(e => e.Key).ToArray());
            var prs = string.Join(",", keyValues.Select(e => "@" + e.Key).ToArray());
            _dataContext.Database.ExecuteSqlCommand(
                $"insert into {tableName} ({columns}) values ({prs})",
                keyValues.Select(e => new SqlParameter(e.Key, e.Value)).ToArray());
        }
    }
}
