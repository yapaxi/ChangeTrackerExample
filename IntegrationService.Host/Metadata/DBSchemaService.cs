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

        public DBSchemaService(SchemaRepository repository)
        {
            _repository = repository;
        }

        public SchemaActivationResult ActivateSchema(string name, string queueName, MappingSchema schema)
        {
            try
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
                Console.WriteLine("Existing found");
                if (existing.IsActive)
                {
                    Console.WriteLine("Already active, nothing to do");
                    return new StagingTable(existing.StagingTableName);
                }
                else
                {
                    Console.WriteLine("Reactivating");
                    var stagingTable = CreateTable(name, schema.Properties);

                    DeactivateAll(mappings);
                    Activate(existing);

                    _repository.SaveChanges();
                    return stagingTable;
                }
            }
            else
            {
                var max = !mappings.Any() ? default(DateTime) : mappings.Max(e => e.SchemaCreatedAt);

                if (schema.CreatedUTC < max)
                {
                    var times = $"Request UTC: {schema.CreatedUTC:yyyyMMddHHmmss.fff} is less than Known UTC: {max:yyyyMMddHHmmss.fff}";
                    throw new Exception($"Schema change is rejected, because it was issued before last known schema. {times}");
                }

                Console.WriteLine("Exiting not found");

                var stagingTable = CreateTable(name, schema.Properties);

                DeactivateAll(mappings);

                _repository.Add(new Mapping()
                {
                    IsActive = true,
                    Checksum = schema.Checksum,
                    CreatedAt = DateTime.UtcNow,
                    SchemaCreatedAt = schema.CreatedUTC,
                    Name = name,
                    QueueName = queueName,
                    Schema = JsonConvert.SerializeObject(schema),
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
