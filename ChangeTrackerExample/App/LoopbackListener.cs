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

        private string _consumerTag;

        public event EventHandler<EntityChangedEventArgs> EntityChanged;
        public event EventHandler<EventArgs> Cancelled;

        public LoopbackListener(IModel loopbackModel, string loopbackQueue)
        {
            _model = loopbackModel;
            _loopbackQueue = loopbackQueue;
            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += HandleEntityChangedMessage;
            _consumer.ConsumerCancelled += (s, o) => Cancelled?.Invoke(this, o);
        }

        public void Start()
        {
            if (EntityChanged == null)
            {
                throw new InvalidOperationException($"No one listens for {nameof(LoopbackListener)}'s {nameof(EntityChanged)} event");
            }

            if (_consumerTag != null)
            {
                throw new InvalidOperationException("Consumer is already listening");
            }

            _consumerTag = _model.BasicConsume(_loopbackQueue, false, _consumer);
        }

        public void Cancel()
        {
            if (_consumerTag != null)
            {
                throw new InvalidOperationException("Nothing to cancel: no consumer tag found");
            }
            
            _model.BasicCancel(_consumerTag);
        }

        private void HandleEntityChangedMessage(object sender, BasicDeliverEventArgs obj)
        {
            var type = Encoding.UTF8.GetString((byte[])obj.BasicProperties.Headers[Header.TYPE_HEADER]);
            EntityChanged.Invoke(this, new EntityChangedEventArgs(BitConverter.ToInt32(obj.Body, 0), type));
            ((IBasicConsumer)sender).Model.BasicAck(obj.DeliveryTag, false);
        }
    }
}
