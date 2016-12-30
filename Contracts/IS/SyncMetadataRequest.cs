using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.IS
{
    public class SyncMetadataRequest
    {
        public string Name { get; set; }
        public MappingSchema Schema { get; set; }
    }
}
