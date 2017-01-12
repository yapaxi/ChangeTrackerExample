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
                    var result = ActivateSchemaInternal(name, queueName, schema);
                    tran.Commit();
                    return result;
                }
            }
            catch (Exception e)
            {
                return new SchemaActivationResult(name, e);
            }
        }

        public Mapping[] GetActiveMappings()
        {
            return _repository.Mappings.Where(e => e.IsActive).ToArray();
        }

        private SchemaActivationResult ActivateSchemaInternal(string name, string queueName, MappingSchema schema)
        {
            var mappings = _repository.Mappings.Where(e => e.Name == name).ToArray();
            var existing = mappings.FirstOrDefault(e => e.Checksum == schema.Checksum);

            if (existing != null)
            {
                Console.WriteLine("Existing table found");
                if (existing.IsActive)
                {
                    Console.WriteLine("Already active, nothing to do");
                    var stagingTable = JsonConvert.DeserializeObject<StagingTable>(existing.StagingTables);
                    return new SchemaActivationResult(name, stagingTable);
                }
                else
                {
                    Console.WriteLine("Reactivating");

                    var stagingTable = CreateTables(name, schema.Properties);

                    DeactivateAll(mappings);

                    Activate(existing);

                    _repository.SaveChanges();

                    return new SchemaActivationResult(name, stagingTable, fullRebuildRequired: true);
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

                Console.WriteLine("Existing table not found");

                var stagingTable = CreateTables(name, schema.Properties);

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
                    StagingTables = JsonConvert.SerializeObject(stagingTable)
                });

                _repository.SaveChanges();

                return new SchemaActivationResult(name, stagingTable, fullRebuildRequired: true);
            }
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

        private static void Activate(Mapping existing)
        {
            existing.IsActive = true;
        }

        private static void DeactivateAll(Mapping[] mappings)
        {
            foreach (var m in mappings)
            {
                m.DeactivatedAt = DateTime.UtcNow;
                m.IsActive = false;
            }
        }
    }
}
