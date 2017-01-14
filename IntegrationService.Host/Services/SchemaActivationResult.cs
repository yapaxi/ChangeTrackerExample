using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Metadata
{
    public class SchemaStatus
    {
        public SchemaStatus(string name, bool fullRebuildRequired, bool isActive)
        {
            this.Name = name;
            this.IsActive = isActive;
            this.FullRebuildRequired = fullRebuildRequired;
        }

        public SchemaStatus(string name, Exception exception)
        {
            this.Name = name;
            this.Exception = exception;
        }

        public string Name { get; }

        public bool FullRebuildRequired { get; }

        public bool IsActive { get; }

        public Exception Exception { get; }

        public bool IsFailed => Exception != null;
    }

    public class SchemaActivationResult
    {
        public SchemaActivationResult(string name, IStagingTable table)
        {
            this.StagingTable = table;
        }

        public string Name { get; }

        public IStagingTable StagingTable { get; }
    }
}
