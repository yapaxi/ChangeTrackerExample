using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Domain;
using Newtonsoft.Json;
using IntegrationService.Host.DAL.DDL;
using IntegrationService.Host.Services;
using IntegrationService.Host.Metadata;

namespace IntegrationService.Host.Services
{
    public class SchemaPersistenceService : ISchemaPersistenceService
    {
        private readonly SchemaRepository _repository;

        public SchemaPersistenceService(SchemaRepository repository)
        {
            _repository = repository;
        }

        public ResolvedMapping[] GetActiveMappings()
        {
            return _repository.Mappings.Where(e => e.IsActive).ToArray().Select(e => new ResolvedMapping(e)).ToArray();
        }

        public SchemaStatus GetSchemaStatus(string entityName, string queueName, MappingSchema schema)
        {
            try
            {
                var mappings = _repository.Mappings.Where(e => e.Name == entityName).ToArray();
                var existing = mappings.FirstOrDefault(e => e.Checksum == schema.Checksum);
                return CheckSchemaInternal(entityName, schema, mappings, existing, failHard: false);
            }
            catch (Exception e)
            {
                return new SchemaStatus(entityName, e);
            }
        }

        public IWriteDestination UseSchema(string entityName, string queueName, MappingSchema schema)
        {
            using (var tran = _repository.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                var table = GetOrCreateTableForSchema(entityName, queueName, schema);
                tran.Commit();
                return table;
            }
        }

        private IWriteDestination GetOrCreateTableForSchema(string entityName, string queueName, MappingSchema schema)
        {
            var mappings = _repository.Mappings.Where(e => e.Name == entityName).ToArray();
            var existing = mappings.FirstOrDefault(e => e.Checksum == schema.Checksum);
            var result = CheckSchemaInternal(entityName, schema, mappings, existing, failHard: true);

            if (result.FullRebuildRequired)
            {
                var table = CreateTables(entityName, schema.Properties);

                DeactivateAll(mappings);

                var mapping = new Mapping()
                {
                    CreatedAt = DateTime.UtcNow,
                    Name = entityName,
                    QueueName = queueName
                };

                Activate(mapping, schema, table);

                _repository.Add(mapping);
                _repository.SaveChanges();

                return new WriteDestination(table);
            }
            else if (result.IsActive)
            {
                return ResolvedMapping.GetWriteDestination(existing);
            }
            else if (!result.IsActive)
            {
                var table = CreateTables(entityName, schema.Properties);

                DeactivateAll(mappings);

                Activate(existing, schema, table);

                _repository.SaveChanges();
                
                return new WriteDestination(table);
            }

            throw new InvalidOperationException("Unexpected result");
        }

        private static SchemaStatus CheckSchemaInternal(string entityName, MappingSchema schema, Mapping[] mappings, Mapping existing, bool failHard)
        {
            if (existing != null)
            {
                return new SchemaStatus(entityName, false, existing.IsActive);
            }
            else
            {
                var max = !mappings.Any() ? default(DateTime) : mappings.Max(e => e.SchemaCreatedAt);

                if (schema.CreatedUTC < max)
                {
                    var times = $"Request UTC: {schema.CreatedUTC:yyyyMMddHHmmss.fff} is less than Known UTC: {max:yyyyMMddHHmmss.fff}";
                    var exception = new Exception($"Schema change is rejected, because it was issued before last known schema. {times}");
                    if (failHard)
                    {
                        throw exception;
                    }
                    return new SchemaStatus(entityName, exception);
                }
                else
                {
                    return new SchemaStatus(entityName, true, false);
                }
            }
        }

        private void DeactivateAll(Mapping[] mappings)
        {
            foreach (var mapping in mappings)
            {
                mapping.DeactivatedAt = DateTime.UtcNow;
                mapping.IsActive = false;
            }
        }

        private void Activate(Mapping mapping, MappingSchema schema, IStagingTable table)
        {
            mapping.IsActive = true;
            mapping.StagingTables = JsonConvert.SerializeObject(table);
            mapping.Schema = JsonConvert.SerializeObject(schema);
            mapping.Checksum = schema.Checksum;
            mapping.SchemaCreatedAt = schema.CreatedUTC;
        }

        private StagingTable CreateTables(string name, MappingProperty[] newSchema)
        {
            var simpleProperties = newSchema
                .Where(e => !e.Children.Any())
                .Select(e => new TableColumnDefinition(
                    e.ShortName,
                    e.ClrType,
                    GetSqlTypeForClrType(e.ClrType, e.Size),
                    IsNullable(e.ClrType))
                ).ToArray();

            var table = _repository.CreateStagingTable(name, simpleProperties);

            table.Children = new List<StagingTable>();

            foreach (var v in newSchema.Where(e => e.Children.Any()))
            {
                table.Children.Add(CreateTables(v.PathName, v.Children));
            }

            return table;
        }

        private string GetSqlTypeForClrType(string clrType, int? size)
        {
            var type = Type.GetType(clrType);

            if (type.IsGenericType)
            {
                return GetSqlTypeForClrTypeInternal(type.GetGenericArguments()[0], size);
            }

            return GetSqlTypeForClrTypeInternal(type, size);
        }

        private static string GetSqlTypeForClrTypeInternal(Type type, int? size)
        {
            if (type == typeof(string))
            {
                return size == null ? "nvarchar(max)" : $"nvarchar({size})";
            }

            if (type == typeof(Guid))
            {
                return "uniqueidentifier";
            }

            if (type == typeof(int))
            {
                return "int";
            }

            if (type == typeof(long))
            {
                return "bigint";
            }

            if (type == typeof(bool))
            {
                return "bit";
            }

            if (type == typeof(double))
            {
                return "float";
            }

            throw new InvalidOperationException($"Unexpected type: {type}");
        }

        private bool IsNullable(string clrType)
        {
            var t = Type.GetType(clrType);
            return t.IsClass || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }
    }
}
