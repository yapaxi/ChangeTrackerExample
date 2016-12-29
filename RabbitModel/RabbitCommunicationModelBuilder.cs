using EasyNetQ;
using EasyNetQ.Topology;
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
        private readonly IAdvancedBus _advancedBus;

        public RabbitCommunicationModelBuilder(IAdvancedBus model)
        {
            _advancedBus = model;
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

            var e = _advancedBus.ExchangeDeclare(exchange, "direct", durable: true);
            var q = _advancedBus.QueueDeclare(queue, durable: true);
            _advancedBus.Bind(e, q, "");
        }

        public IExchange BuildTrackerToISContract(string exchangeToPush, string queueToStore)
        {
            if (string.IsNullOrWhiteSpace(exchangeToPush))
            {
                throw new ArgumentNullException(nameof(exchangeToPush));
            }

            if (string.IsNullOrWhiteSpace(queueToStore))
            {
                throw new ArgumentNullException(nameof(queueToStore));
            }

            var q = BuildISExpectationsContract(queueToStore);
            var e = _advancedBus.ExchangeDeclare(exchangeToPush, "direct", durable: true);
            _advancedBus.Bind(e, q, "");
            return e;
        }

        public IQueue BuildISExpectationsContract(string queueToReceiveFrom)
        {
            if (string.IsNullOrWhiteSpace(queueToReceiveFrom))
            {
                throw new ArgumentNullException(nameof(queueToReceiveFrom));
            }

            return _advancedBus.QueueDeclare(queueToReceiveFrom, durable: true);
        }
    }
}