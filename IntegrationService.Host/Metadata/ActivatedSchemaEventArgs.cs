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
        public ActivatedSchemaEventArgs(string entityName, MappingSchema schema, string queue, StagingTable stagingTable)
            : base(entityName)
        {
            Schema = schema;
            Queue = queue;
            StagingTable = stagingTable;
        }

        public MappingSchema Schema { get; }
        public string Queue { get; }
        public StagingTable StagingTable { get; }
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
