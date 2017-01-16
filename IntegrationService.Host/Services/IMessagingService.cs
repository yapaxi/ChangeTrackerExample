using IntegrationService.Host.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Services
{
    public interface IMessagingService<TMessage>
    {
        void WriteMessage(TMessage rawMessage, MessageInfo info);
    }
}
