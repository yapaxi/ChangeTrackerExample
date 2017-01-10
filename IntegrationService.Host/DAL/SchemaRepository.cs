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

        public StagingTable CreateStagingTable(string schemalessTableName, TableColumnDefinition[] columns)
        {
            var tableName = FormatTableName(STAGING_SCHEMA_NAME, schemalessTableName);
            MoveOldStagingTableIfExists(schemalessTableName);
            CreateTableInternal(tableName, columns);
            return new StagingTable() { FullName = tableName, SystemName = schemalessTableName };
        }

        private string CreateTableInternal(string fqTableName, TableColumnDefinition[] columns)
        {
            Console.WriteLine($"Creating table {fqTableName}");
            var sql = GetTableCreateSql(fqTableName, columns);
            Console.WriteLine(sql);
            _context.Database.ExecuteSqlCommand(sql);
            Console.WriteLine($"Table {fqTableName} created");
            return fqTableName;
        }

        private void MoveOldStagingTableIfExists(string name)
        {
            var existingTableName = FormatTableName(STAGING_SCHEMA_NAME, name);

            var exists = _context.Database.SqlQuery<bool>(
                @"select cast(iif(object_id(@p0) is null, 0, 1) as bit) as [exists]",
                new SqlParameter("p0", existingTableName)
            ).FirstOrDefault();

            var schemalessNewTableName = $"{name}_{DateTime.UtcNow:yyyyMMddHHmmss.fff}";

            var fqMovedTableName = FormatTableName(DEACTIVATED_SCHEMA_NAME, name);
            var fqNewTableName = FormatTableName(DEACTIVATED_SCHEMA_NAME, schemalessNewTableName);

            if (exists)
            {
                Console.WriteLine($"Table {existingTableName} found, moving to {fqNewTableName}");
                _context.Database.ExecuteSqlCommand($"alter schema [{DEACTIVATED_SCHEMA_NAME}] transfer {existingTableName}");
                _context.Database.ExecuteSqlCommand($"sp_rename '{fqMovedTableName}', '{schemalessNewTableName}'");
            }
            else
            {
                Console.WriteLine($"Table {existingTableName} not found");
            }
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

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}
