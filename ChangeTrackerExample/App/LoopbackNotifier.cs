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
        private readonly string _loopbackExchange;
        private readonly IModel _model;

        public LoopbackNotifier(IModel model, string loopbackExchange)
        {
            _model = model;
            _loopbackExchange = loopbackExchange;
        }

        public void NotifyChanged<TSource>(int id)
        {
            var properties = CreateProperties<TSource>();
            _model.BasicPublish(_loopbackExchange, "", properties, BitConverter.GetBytes(id));
        }

        private IBasicProperties CreateProperties<TSource>()
        {
            var properties = _model.CreateBasicProperties();
            properties.ContentType = "application/octet-stream";
            properties.DeliveryMode = 2;
            properties.Headers = new Dictionary<string, object>();
            properties.Headers[Header.TYPE_HEADER] = typeof(TSource).FullName;
            return properties;
        }
    }
}
