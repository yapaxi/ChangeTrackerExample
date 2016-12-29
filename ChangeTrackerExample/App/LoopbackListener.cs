using EasyNetQ;
using EasyNetQ.Topology;
using RabbitModel;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App
{
    public class LoopbackListener : IDisposable
    {
        private readonly IBus _bus;
        private readonly IQueue _queue;

        private IDisposable _subscription;

        public event EventHandler<EntityChangedEventArgs> EntityChanged;

        public LoopbackListener(IBus bus, IQueue queue)
        {
            _bus = bus;
            _queue = queue;
        }

        public void Start()
        {
            if (EntityChanged == null)
            {
                throw new InvalidOperationException($"No one listens for {nameof(LoopbackListener)}'s {nameof(EntityChanged)} event");
            }

            if (_subscription != null)
            {
                throw new InvalidOperationException("Listener is already started");
            }

            _subscription = _bus.Advanced.Consume(_queue, (b, m, i) => HandleEntityChangedMessage(m, b));
        }

        public void Cancel()
        {
            if (_subscription != null)
            {
                throw new InvalidOperationException("Nothing to cancel: no subscription found");
            }

            Dispose();
        }

        private void HandleEntityChangedMessage(MessageProperties properties, byte[] body)
        {
            var type = Encoding.UTF8.GetString((byte[])properties.Headers[LoopbackMessageHeader.MESSAGE_TYPE]);
            EntityChanged.Invoke(this, new EntityChangedEventArgs(BitConverter.ToInt32(body, 0), type));
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
