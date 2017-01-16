using IntegrationService.Host.DAL.Contexts;
using IntegrationService.Host.DAL.DDL;
using IntegrationService.Host.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL
{
    public class SchemaRepository : RepositoryBase<SchemaContext>
    {
        private const string DEACTIVATED_SCHEMA_NAME = "deactivated";
        private const string STAGING_SCHEMA_NAME = "staging";
        private readonly ILogger _logger;

        public SchemaRepository(SchemaContext context, ILogger logger)
            : base(context)
        {
            _logger = logger;
        }
        
        public T Add<T>(T entity) where T : class => Context.Set<T>().Add(entity);

        public IQueryable<Mapping> Mappings => Context.Mappings;

        public StagingTable CreateStagingTable(string schemalessTableName, TableColumnDefinition[] columns)
        {
            CreateSchemaIfNotExists(STAGING_SCHEMA_NAME);

            var tableName = FormatTableName(STAGING_SCHEMA_NAME, schemalessTableName);
            MoveOldStagingTableIfExists(schemalessTableName);
            CreateTableInternal(tableName, columns);
            return new StagingTable()
            {
                FullName = tableName,
                SystemName = schemalessTableName,
                Columns = columns
            };
        }

        private string CreateTableInternal(string fqTableName, TableColumnDefinition[] columns)
        {
            _logger.Info($"Creating table {fqTableName}");

            var sql = GetTableCreateSql(fqTableName, columns);

            _logger.Debug(sql);

            Context.Database.ExecuteSqlCommand(sql);

            _logger.Info($"Table {fqTableName} created");
            return fqTableName;
        }

        private void MoveOldStagingTableIfExists(string name)
        {
            var existingTableName = FormatTableName(STAGING_SCHEMA_NAME, name);

            var exists = Context.Database.SqlQuery<bool>(
                @"select cast(iif(object_id(@p0) is null, 0, 1) as bit) as [exists]",
                new SqlParameter("p0", existingTableName)
            ).FirstOrDefault();

            var schemalessNewTableName = $"{name}_{DateTime.UtcNow:yyyyMMddHHmmss.fff}";

            var fqMovedTableName = FormatTableName(DEACTIVATED_SCHEMA_NAME, name);
            var fqNewTableName = FormatTableName(DEACTIVATED_SCHEMA_NAME, schemalessNewTableName);

            if (exists)
            {
                _logger.Info($"Table {existingTableName} found, moving to {fqNewTableName}");
                CreateSchemaIfNotExists(DEACTIVATED_SCHEMA_NAME);
                Context.Database.ExecuteSqlCommand($"alter schema [{DEACTIVATED_SCHEMA_NAME}] transfer {existingTableName}");
                Context.Database.ExecuteSqlCommand("sp_rename {0}, {1}", fqMovedTableName, schemalessNewTableName);
            }
            else
            {
                _logger.Info($"Table {existingTableName} not found");
            }
        }

        private void CreateSchemaIfNotExists(string name)
        {
            Context.Database.ExecuteSqlCommand($"if (schema_id('{name}') is null) begin exec sp_executesql N'create schema [{name}]'; end ");
        }

        private static string FormatTableName(string schema, string tableName)
        {
            return $"[{schema}].[{tableName}]";
        }

        private static string GetTableCreateSql(string tableName, TableColumnDefinition[] columns)
        {
            const string indent = "    ";
            var builder = new StringBuilder();
            
            builder.AppendLine($"create table {tableName}");
            builder.AppendLine("(");
            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                builder.Append($"{indent}[{column.Name}] {column.SqlType}");
                if (column.IsNullable)
                {
                    builder.Append(" null");
                }
                else
                {
                    builder.Append(" not null");
                }

                if (i != columns.Length - 1)
                {
                    builder.AppendLine(",");
                }
                else
                {
                    builder.AppendLine();
                }
            }
            builder.AppendLine(")");
            return builder.ToString();
        }
    }
}
