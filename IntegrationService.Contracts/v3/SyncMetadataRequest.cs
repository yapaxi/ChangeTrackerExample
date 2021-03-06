﻿using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Contracts.v3
{
    public class SyncMetadataRequest
    {
        public SyncMetadataRequestItem[] Items { get; set; }
    }

    public class SyncMetadataRequestItem
    {
        public string EntityName { get; set; }
        public MappingSchema Schema { get; set; }
        public string QueueName { get; set; }
        public string SourceTypeFullName { get; set; }
    }
}
