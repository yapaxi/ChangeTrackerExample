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
        public ActivatedSchemaEventArgs(MappingProperty[] schemaProperties, string queue, StagingTable stagingTable)
        {
            SchemaProperties = schemaProperties;
            Queue = queue;
            StagingTable = stagingTable;
        }

        public MappingProperty[] SchemaProperties { get; }
        public string Queue { get; }
        public StagingTable StagingTable { get; }
    }
}
