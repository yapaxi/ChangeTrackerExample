using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using EasyNetQ;
using IntegrationService.Client;

namespace RabbitModel
{
    public class RabbitAutofacModule : Autofac.Module
    {
        private readonly string _host = "192.168.11.169";
        private readonly string _scope;
        private readonly string _loopbackVHost;

        public RabbitAutofacModule(string busResolveScope, string loopbackVHost = null)
        {
            _scope = busResolveScope;
            _loopbackVHost = loopbackVHost;
        }

        protected override void Load(ContainerBuilder builder)
        {

            builder
                .Register(e => RabbitHutch.CreateBus($@"host={_host};timeout=120;virtualHost=ISSync;username=test;password=test"))
                .Named<IBus>(Buses.ISSync)
                .InstancePerMatchingLifetimeScope(_scope);

            builder
                .Register(e => RabbitHutch.CreateBus($@"host={_host};timeout=120;virtualHost=/;username=test;password=test"))
                .Named<IBus>(Buses.Messaging)
                .InstancePerMatchingLifetimeScope(_scope);

            if (!string.IsNullOrWhiteSpace(_loopbackVHost))
            {
                builder
                    .Register(e => RabbitHutch.CreateBus($@"host={_host};timeout=120;virtualHost={_loopbackVHost};username=test;password=test"))
                    .Named<IBus>(Buses.Loopback)
                    .InstancePerMatchingLifetimeScope(_scope);
            }

            builder
                .Register(e => new ISClient(e.ResolveNamed<IBus>(Buses.ISSync)))
                .InstancePerMatchingLifetimeScope(_scope);

            base.Load(builder);
        }
    }
}
