using Common;
using Common.Runtime;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Services
{
    public class ResolvedMapping
    {
        public ResolvedMapping(Mapping mapping)
        {
            this.EntityName = mapping.Name;
            this.QueueName = mapping.QueueName;
            this.Schema = GetRuntimeSchema(mapping);
            this.Destination = GetWriteDestination(mapping);
        }

        public string EntityName { get; }

        public string QueueName { get; }

        public IRuntimeMappingSchema Schema { get; }

        public IWriteDestination Destination { get; }

        public static IRuntimeMappingSchema GetRuntimeSchema(Mapping mapping) 
            => new RuntimeMappingSchema(JsonConvert.DeserializeObject<MappingSchema>(mapping.Schema));

        public static IWriteDestination GetWriteDestination(Mapping mapping) 
            => new WriteDestination(JsonConvert.DeserializeObject<StagingTable>(mapping.StagingTables));
    }
}
