using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class TrackerLoopbackExchange
    {
        public TrackerLoopbackExchange(string exchangeFQN)
        {
            ExchangeFQN = exchangeFQN;
        }

        public string ExchangeFQN { get; }
    }
}
