using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners.Data
{
    public class RawMessage
    {
        public RawMessage(int entityId, byte[] body)
        {
            EntityId = entityId;
            Body = body;
        }

        public int EntityId { get; }
        public byte[] Body { get; }
    }
}
