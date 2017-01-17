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
        private readonly DataMode _mode;
        private readonly IAdvancedBus _advancedBus;

        public RabbitCommunicationModelBuilder(DataMode mode, IAdvancedBus model)
        {
            _mode = mode;
            _advancedBus = model;
        }

        public void BuildISMetadataHandshakeContract()
        {
            const string name = "IS.metadata-handshake";
            var e = _advancedBus.ExchangeDeclare(name, "direct", durable: true);
            var q = _advancedBus.QueueDeclare($"{name}.queue", durable: true);
            _advancedBus.Bind(e, q, "");
        }

        public IExchange BuildTrackerToISContract(string inputName, string outputName)
        {
            if (string.IsNullOrWhiteSpace(inputName))
            {
                throw new ArgumentNullException(nameof(inputName));
            }

            var q = BuildISExpectationsContract(outputName);
            var e = _advancedBus.ExchangeDeclare(inputName, "direct", durable: true);
            _advancedBus.Bind(e, q, "");
            return e;
        }

        public IQueue BuildISExpectationsContract(string outputName)
        {
            if (string.IsNullOrWhiteSpace(outputName))
            {
                throw new ArgumentNullException(nameof(outputName));
            }

            switch (_mode)
            {
                case DataMode.RowByRow:
                    return _advancedBus.QueueDeclare(outputName, durable: true);
                case DataMode.Bulk:
                    return _advancedBus.QueueDeclare(outputName, durable: false, autoDelete: true);
                default:
                    throw new InvalidOperationException($"Unexpected mode {_mode}");
            }

        }
    }
}