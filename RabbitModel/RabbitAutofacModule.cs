using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using RabbitMQ.Client;

namespace RabbitModel
{
    public class RabbitAutofacModule : Autofac.Module
    {
        private readonly string _rabbitUri;

        public RabbitAutofacModule(string rabbitUri)
        {
            _rabbitUri = rabbitUri;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                  .Register(e => new ConnectionFactory() { Uri = _rabbitUri })
                  .As<IConnectionFactory>()
                  .SingleInstance();

            builder
                .Register(e => e.Resolve<IConnectionFactory>().CreateConnection())
                .As<IConnection>()
                .SingleInstance();

            builder
                .Register(e => e.Resolve<IConnection>().CreateModel())
                .As<IModel>();

            base.Load(builder);
        }
    }
}
