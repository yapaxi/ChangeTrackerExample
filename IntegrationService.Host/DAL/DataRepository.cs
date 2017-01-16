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
using NLog;

namespace IntegrationService.Host.DAL
{
    public class DataRepository : RepositoryBase<DataContext>
    {
        private readonly ILogger _logger;

        public DataRepository(DataContext dataContext, ILogger logger)
            : base(dataContext)
        {
            _logger = logger;
        }

        public void Merge(string tableName, IReadOnlyDictionary<string, object> keyValues)
        {
            var columns = string.Join(",", keyValues.Select(e => e.Key).ToArray());
            var parameters = string.Join(",", keyValues.Select(e => "@" + e.Key).ToArray());
            var update = string.Join(",", keyValues.Where(e => !e.Key.Equals("Id", StringComparison.OrdinalIgnoreCase))
                                                   .Select(e => $"[target].[{e.Key}] = @{e.Key}").ToArray());

            Context.Database.ExecuteSqlCommand(
                $@"merge {tableName} as [target]
                   using (select @id as id) as [source]
                   on [target].[id] = [source].[id]
                   when not matched then insert ({columns})
                                         values ({parameters})
                   when matched then update 
                                     set {update};",
                keyValues.Select(e => new SqlParameter(e.Key, e.Value)).ToArray());
        }

        public void BulkInsert(IStagingTable table, IEnumerable<IReadOnlyDictionary<string, object>> keyValues)
        {
            var dataTable = new DataTable();

            foreach (var column in table.Columns)
            {
                dataTable.Columns.Add(column.Name, column.UnwrappedType);
            }

            using (var bulk = new SqlBulkCopy(
                Context.Database.Connection.ConnectionString,
                SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction | SqlBulkCopyOptions.CheckConstraints
            ))
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

                _logger.Info($"BulkInsert {dataTable.Rows.Count} rows to table {table.FullName}");
            }
        }
    }
}
