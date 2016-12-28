using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitModel
{
    public class RabbitCommunicationModelBuilder
    {
        private readonly IModel _model;

        public RabbitCommunicationModelBuilder(IModel model)
        {
            _model = model;
        }

        public void BuildTrackerLoopback(string exchange, string queue)
        {
            if (string.IsNullOrWhiteSpace(exchange))
            {
                throw new ArgumentNullException(nameof(exchange));
            }

            if (string.IsNullOrWhiteSpace(queue))
            {
                throw new ArgumentNullException(nameof(queue));
            }

            _model.ExchangeDeclare(exchange, "direct", true, false, null);
            _model.QueueDeclare(queue, true, false, false, null);
            _model.QueueBind(queue, exchange, "", null);
        }

        public void BuildTrackerToISContract(string exchangeToPush, string queueToStore)
        {
            if (string.IsNullOrWhiteSpace(exchangeToPush))
            {
                throw new ArgumentNullException(nameof(exchangeToPush));
            }

            if (string.IsNullOrWhiteSpace(queueToStore))
            {
                throw new ArgumentNullException(nameof(queueToStore));
            }

            BuildISExpectationsContract(queueToStore);
            _model.ExchangeDeclare(exchangeToPush, "direct", true, false, null);
            _model.QueueBind(queueToStore, exchangeToPush, "", null);
        }

        public void BuildISExpectationsContract(string queueToReceiveFrom)
        {
            if (string.IsNullOrWhiteSpace(queueToReceiveFrom))
            {
                throw new ArgumentNullException(nameof(queueToReceiveFrom));
            }

            _model.QueueDeclare(queueToReceiveFrom, true, false, false, null);
        }
    }
}
