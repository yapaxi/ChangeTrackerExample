using Common;
using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Metadata
{
    public class ActivatedSchemaEventArgs : EventArgs
    {
        public ActivatedSchemaEventArgs(string entityName, MappingSchema schema, string queue, StagingTable stagingTable)
        {
            Schema = schema;
            Queue = queue;
            StagingTable = stagingTable;
            EntityName = entityName;
        }

        public MappingSchema Schema { get; }
        public string Queue { get; }
        public StagingTable StagingTable { get; }
        public string EntityName { get; }
    }
}
