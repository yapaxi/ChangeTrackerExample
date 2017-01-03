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

namespace IntegrationService.Host.Metadata
{
    public class DBSchemaService
    {
        private readonly SchemaRepository _repository;
        private readonly object _lock;

        public DBSchemaService(SchemaRepository repository)
        {
            _lock = new object();
            _repository = repository;
        }

        public SchemaActivationResult ActivateSchema(string name, string queueName, MappingSchema schema)
        {
            try
            {
                lock (_lock)
                {
                    using (var tran = _repository.BeginTransaction())
                    {
                        var stagingTable = ActivateSchemaInternal(name, queueName, schema);

                        tran.Commit();

                        return new SchemaActivationResult()
                        {
                            StagingTable = stagingTable,
                            Name = name,
                        };
                    }
                }
            }
            catch (Exception e)
            {
                return new SchemaActivationResult()
                {
                    Exception = e,
                    Name = name,
                };
            }
        }

        public Mapping[] GetActiveMappings()
        {
            return _repository.Mappings.Where(e => e.IsActive).ToArray();
        }

        private StagingTable ActivateSchemaInternal(string name, string queueName, MappingSchema schema)
        {
            var mappings = _repository.Mappings.Where(e => e.Name == name).ToArray();

            var existing = mappings.FirstOrDefault(e => e.Checksum == schema.Checksum);

            if (existing != null)
            {
                if (existing.IsActive)
                {
                    return new StagingTable(existing.StagingTableName);
                }
                else
                {
                    var stagingTable = CreateTable(name, schema.Properties);

                    DeactivateAll(mappings);
                    Activate(existing);

                    _repository.SaveChanges();
                    return stagingTable;
                }
            }
            else
            {
                var stagingTable = CreateTable(name, schema.Properties);

                DeactivateAll(mappings);

                _repository.Add(new Mapping()
                {
                    IsActive = true,
                    Checksum = schema.Checksum,
                    CreatedAt = DateTime.UtcNow,
                    Name = name,
                    QueueName = queueName,
                    SchemaProperties = JsonConvert.SerializeObject(schema.Properties),
                    StagingTableName = stagingTable.Name
                });

                _repository.SaveChanges();
                return stagingTable;
            }
        }

        private StagingTable CreateTable(string name, MappingProperty[] newSchema)
        {
            return _repository.CreateStagingTable(name, newSchema.Select(e => new TableColumnDefinition()
            {
                Name = e.Name,
                IsNullable = IsNullable(e.ClrType),
                SqlType = GetSqlTypeForClrType(e.ClrType, e.Size),
            }).ToArray());
        }

        private string GetSqlTypeForClrType(string clrType, int? size)
        {
            var type = Type.GetType(clrType);
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

            throw new InvalidOperationException($"Unexpected type: {clrType} -> {type}");
        }

        private bool IsNullable(string clrType)
        {
            var t = Type.GetType(clrType);
            return t.IsClass || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static void Activate(Mapping existing)
        {
            existing.IsActive = true;
        }

        private static void DeactivateAll(Domain.Mapping[] mappings)
        {
            foreach (var m in mappings)
            {
                m.DeactivatedAt = DateTime.UtcNow;
                m.IsActive = false;
            }
        }
    }
}
