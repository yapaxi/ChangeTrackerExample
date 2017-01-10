using Common;
using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Metadata
{
    public class ActivatedSchemaEventArgs : SchemaEventArgs
    {
        public ActivatedSchemaEventArgs(string entityName, RuntimeMappingSchema schema, string queue, IStagingTable stagingTable)
            : base(entityName)
        {
            Schema = schema;
            Queue = queue;
            StagingTable = stagingTable;
        }

        public RuntimeMappingSchema Schema { get; }
        public string Queue { get; }
        public IStagingTable StagingTable { get; }
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
