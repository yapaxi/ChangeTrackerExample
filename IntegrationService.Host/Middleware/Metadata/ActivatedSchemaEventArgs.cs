using Common;
using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners.Metadata
{
    public class ActivatedSchemaEventArgs : SchemaEventArgs
    {
        public ActivatedSchemaEventArgs(string queue, string entityName, RuntimeMappingSchema schema, WriteDestination destination)
            : base(entityName)
        {
            Schema = schema;
            Queue = queue;
            Destination = destination;
        }
        public RuntimeMappingSchema Schema { get; }
        public string Queue { get; }
        public WriteDestination Destination { get; }
    }


    public class SchemaEventArgs : EventArgs
    {
        public SchemaEventArgs(string entityName)
        {
            EntityName = entityName;
        }

        public string EntityName { get; }
    }
}
