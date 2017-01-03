﻿using IntegrationService.Host.DAL.Contexts;
using IntegrationService.Host.DAL.DDL;
using IntegrationService.Host.Domain;
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
    public class SchemaRepository
    {
        private const string DEACTIVATED_SCHEMA_NAME = "deactivated";
        private const string STAGING_SCHEMA_NAME = "staging";

        private readonly SchemaContext _context;

        public SchemaRepository(SchemaContext context)
        {
            _context = context;
        }

        public IQueryable<Mapping> Mappings => _context.Mappings;

        public T Add<T>(T entity) where T : class => _context.Set<T>().Add(entity);

        public DbContextTransaction BeginTransaction()
        {
            return _context.Database.BeginTransaction();
        }

        public DbContextTransaction BeginTransaction(IsolationLevel level)
        {
            return _context.Database.BeginTransaction(level);
        }

        public StagingTable CreateStagingTable(string name, TableColumnDefinition[] columns)
        {
            MoveOldTableIfExists(name);
            var tableName = CreateStagingTableInternal(name, columns);
            return new StagingTable(tableName);
        }

        private string CreateStagingTableInternal(string name, TableColumnDefinition[] columns)
        {
            string tableName;
            var sql = GetStagingTableCreateSql(name, columns, out tableName);
            _context.Database.ExecuteSqlCommand(sql);
            return tableName;
        }

        private void MoveOldTableIfExists(string name)
        {
            var exists = _context.Database.SqlQuery<bool>(
                @"select cast(count(*) as bit) as [exists]
                  from INFORMATION_SCHEMA.TABLES t 
                  where t.TABLE_NAME = @p0 and t.TABLE_SCHEMA = @p1",
                new SqlParameter("p0", name),
                new SqlParameter("p1", STAGING_SCHEMA_NAME)).FirstOrDefault();

            if (exists)
            {
                var timestmap = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                _context.Database.ExecuteSqlCommand($"alter schema [{DEACTIVATED_SCHEMA_NAME}] transfer [{STAGING_SCHEMA_NAME}].[{name}]");
                _context.Database.ExecuteSqlCommand($"sp_rename '[{DEACTIVATED_SCHEMA_NAME}].[{name}]', '{name}_{timestmap}'");
            }
        }

        private static string GetStagingTableCreateSql(string name, TableColumnDefinition[] columns, out string tableName)
        {
            const string indent = "    ";
            var builder = new StringBuilder();

            tableName = $"[{STAGING_SCHEMA_NAME}].[{name}]";
            builder.AppendLine($"create table {tableName}");
            builder.AppendLine("(");
            foreach (var column in columns)
            {
                builder.Append($"{indent}[{column.Name}] {column.SqlType}");
                if (column.IsNullable)
                {
                    builder.Append(" null");
                }
                else
                {
                    builder.Append(" not null");
                }
                builder.AppendLine(",");
            }
            builder.Length -= 1;
            builder.AppendLine(")");
            return builder.ToString();
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}