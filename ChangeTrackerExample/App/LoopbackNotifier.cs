using EasyNetQ;
using EasyNetQ.Topology;
using RabbitModel;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App
{
    public class LoopbackNotifier
    {
        private readonly IExchange _exchange;
        private readonly IBus _bus;

        public LoopbackNotifier(IBus bus, IExchange exchange)
        {
            _bus = bus;
            _exchange = exchange;
        }

        public void NotifyChanged<TSource>(int id)
            where TSource : class
        {
            var properties = CreateProperties<TSource>();
            _bus.Advanced.Publish(_exchange, "", false, properties, BitConverter.GetBytes(id));
        }

        private MessageProperties CreateProperties<TSource>()
        {
            var properties = new MessageProperties();
            properties.ContentType = "application/octet-stream";
            properties.DeliveryMode = 2;
            properties.Headers = new Dictionary<string, object>();
            properties.Headers[LoopbackMessageHeader.MESSAGE_TYPE] = typeof(TSource).FullName;
            return properties;
        }
    }
}
