using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Metadata
{
    public class SchemaActivationResult
    {
        public string Name { get; set; }
        public bool FullRebuildRequired { get; set; }
        public Exception Exception { get; set; }
        public StagingTable StagingTable { get; set; }

        public bool IsFailed => Exception != null;
    }
}
