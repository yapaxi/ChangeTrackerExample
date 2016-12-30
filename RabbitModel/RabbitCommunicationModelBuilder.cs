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

        public void BuildTrackerLoopback(string inputName, string outputName)
        {
            if (string.IsNullOrWhiteSpace(inputName))
            {
                throw new ArgumentNullException(nameof(inputName));
            }

            var e = _advancedBus.ExchangeDeclare(inputName, "direct", durable: true);
            var q = _advancedBus.QueueDeclare(outputName, durable: true);
            _advancedBus.Bind(e, q, "");
        }

        public IExchange BuildTrackerToISContract(string inputName)
        {
            if (string.IsNullOrWhiteSpace(inputName))
            {
                throw new ArgumentNullException(nameof(inputName));
            }

            var q = BuildISExpectationsContract(inputName);
            var e = _advancedBus.ExchangeDeclare(inputName, "direct", durable: true);
            _advancedBus.Bind(e, q, "");
            return e;
        }

        public IQueue BuildISExpectationsContract(string trackerInput)
        {
            if (string.IsNullOrWhiteSpace(trackerInput))
            {
                throw new ArgumentNullException(nameof(trackerInput));
            }

            return _advancedBus.QueueDeclare($"{trackerInput}.queue", durable: true);
        }
    }
}