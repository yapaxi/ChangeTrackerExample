using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners
{
    public class MessageReceivedEventArgs<TMessage> : EventArgs
    {
        public MessageReceivedEventArgs(TMessage message)
        {
            this.Message = message;
        }

        public TMessage Message { get; }
    }
}
