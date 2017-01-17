using Common;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Services
{
    public interface ISchemaPersistenceService
    {
        ResolvedMapping[] GetActiveMappings();
        SchemaStatus GetSchemaStatus(string entityName, string queueName, MappingSchema schema);
        IWriteDestination UseSchema(string entityName, string queueName, MappingSchema schema);
    }
}
