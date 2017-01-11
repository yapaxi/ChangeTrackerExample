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
    public class DataRepository : RepositoryBase<DataContext>
    {

        public DataRepository(DataContext dataContext)
            : base(dataContext)
        {

        }

        public void Merge(string tableName, IReadOnlyDictionary<string, object> keyValues)
        {
            var columns = string.Join(",", keyValues.Select(e => e.Key).ToArray());
            var prs = string.Join(",", keyValues.Select(e => "@" + e.Key).ToArray());

            Context.Database.ExecuteSqlCommand(
                $"insert into {tableName} ({columns}) values ({prs})",
                keyValues.Select(e => new SqlParameter(e.Key, e.Value)).ToArray());
        }
    }
}
