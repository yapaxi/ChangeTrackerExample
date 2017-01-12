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
        public SchemaActivationResult(string name, IStagingTable table, bool fullRebuildRequired = false)
        {
            this.Name = name;
            this.StagingTable = table;
            this.FullRebuildRequired = fullRebuildRequired;
        }

        public SchemaActivationResult(string name, Exception exception)
        {
            this.Name = name;
            this.Exception = exception;
        }

        public string Name { get; }
        public bool FullRebuildRequired { get; }
        public Exception Exception { get;  }
        public IStagingTable StagingTable { get; }

        public bool IsFailed => Exception != null;
    }
}
