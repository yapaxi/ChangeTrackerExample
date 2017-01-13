using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Contracts.v3
{
    public class SyncMetadataResponse
    {
        public SyncMetadataResponseItem[] Items { get; set; }
    }

    public class SyncMetadataResponseItem
    {
        public string Name { get; set; }
        public SyncMetadataResult Result { get; set; }
        public bool FullRebuildRequired { get; set; }
        public bool FullRebuildInProgress { get; set; }
        public string Message { get; set; }
    }

    public enum SyncMetadataResult
    {
        Invalid = 0,
        Success = 1,
        BusinessConstraintViolation = 1001,
        TemporaryError = 2001,
        UnhandledError = 2002,
    }
}
