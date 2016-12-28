using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
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
        private readonly ManagementClient _model;

        public RabbitCommunicationModelBuilder(ManagementClient model)
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

            var vhost = new Vhost() { Name = "/" };
            var e = _model.CreateExchange(new ExchangeInfo(exchange, "direct", false, true, false, new Arguments()), vhost);
            var q = _model.CreateQueue(new QueueInfo(queue, false, true, new InputArguments()), vhost);
            _model.CreateBinding(e, q, new BindingInfo(""));
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

            var vhost = new Vhost() { Name = "/" };
            var q = BuildISExpectationsContract(queueToStore);
            var e = _model.CreateExchange(new ExchangeInfo(exchangeToPush, "direct", false, true, false, new Arguments()), vhost);
            _model.CreateBinding(e, q, new BindingInfo(""));
        }

        public Queue BuildISExpectationsContract(string queueToReceiveFrom)
        {
            if (string.IsNullOrWhiteSpace(queueToReceiveFrom))
            {
                throw new ArgumentNullException(nameof(queueToReceiveFrom));
            }

            var vhost = new Vhost() { Name = "/" };
            var q = _model.CreateQueue(new QueueInfo(queueToReceiveFrom, false, true, new InputArguments()), vhost);
            return q;
        }
    }
}
