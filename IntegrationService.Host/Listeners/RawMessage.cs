using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners
{
    public class RawMessage
    {
        public int EntityId { get; set; }
        public byte[] Body { get; set; }
    }
}
