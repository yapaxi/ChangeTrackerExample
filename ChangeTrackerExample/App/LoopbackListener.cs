using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App
{
    public class LoopbackListener
    {
        private readonly IModel _model;
        private readonly string _loopbackQueue;
        private readonly EventingBasicConsumer _consumer;
        
        public LoopbackListener(IModel loopbackModel, string loopbackQueue)
        {
            _model = loopbackModel;
            _loopbackQueue = loopbackQueue;
            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += (sender, obj) => HandleEntityChangedMessage(obj);
        }

        public void Start()
        {
            _model.BasicConsume(_loopbackQueue, false, _consumer);
        }

        private void HandleEntityChangedMessage(BasicDeliverEventArgs obj)
        {
            var type = Encoding.UTF8.GetString((byte[])obj.BasicProperties.Headers[Header.TYPE_HEADER]);
            _handler.HandleEntityChanged(type, BitConverter.ToInt32(obj.Body, 0));
            _model.BasicAck(obj.DeliveryTag, false);
        }
    }
}
