using Common;
using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Subscriptions
{
    public class MessageInfo
    {
        public MessageInfo(RuntimeMappingSchema schema, IWriteDestination destination)
        {
            Schema = schema;
            Destination = destination;
        }

        public RuntimeMappingSchema Schema { get; }

        public IWriteDestination Destination { get; }
    }
}
