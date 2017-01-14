using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Subscriptions
{
    public class RawMessage
    {
        public RawMessage(byte[] body, int entityCount)
        {
            EntityCount = entityCount;
            Body = body;
        }

        public int EntityCount { get; }
        public byte[] Body { get; }
    }
}
