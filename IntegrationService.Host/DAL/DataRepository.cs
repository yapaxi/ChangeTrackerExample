using IntegrationService.Host.DAL.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityFramework.BulkInsert.Extensions;
using System.Data;
using Common;

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

        public void BulkInsert(IStagingTable table, IEnumerable<IReadOnlyDictionary<string, object>> keyValues)
        {
            var dataTable = new DataTable();

            foreach (var column in table.Columns)
            {
                dataTable.Columns.Add(column.Name, column.UnwrappedType);
            }

            using (var bulk = new SqlBulkCopy(Context.Database.Connection.ConnectionString))
            {
                bulk.DestinationTableName = table.FullName;
                foreach (var kv in keyValues)
                {
                    var row = dataTable.NewRow();
                    foreach (var k in kv)
                    {
                        row[k.Key] = k.Value;
                    }
                    dataTable.Rows.Add(row);
                }
                bulk.WriteToServer(dataTable);
                Console.WriteLine($"BulkInsert {dataTable.Rows.Count} rows to table {table.FullName}");
            }
        }
    }
}
