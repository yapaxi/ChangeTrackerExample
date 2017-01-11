using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Converters
{
    public class FlatMessage
    {
        public FlatMessage(Dictionary<string, List<Dictionary<string, object>>> payload)
        {
            Payload = payload;
        }

        public Dictionary<string, List<Dictionary<string, object>>> Payload { get; }
    }
}
