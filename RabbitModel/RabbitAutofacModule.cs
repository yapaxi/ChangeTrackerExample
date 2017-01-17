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
        private readonly string _password;
        private readonly string _user;
        private readonly string _host;
        private readonly string _scope;
        private readonly string _loopbackVHost;

        public RabbitAutofacModule(
            string user,
            string password,
            string host,
            string busResolveScope,
            string loopbackVHost = null)
        {
            _password = password;
            _user = user;
            _host = host;
            _scope = busResolveScope;
            _loopbackVHost = loopbackVHost;
        }

        protected override void Load(ContainerBuilder builder)
        {

            builder
                .Register(e => RabbitHutch.CreateBus($@"host={_host};timeout=120;virtualHost={Buses.ISSync};username={_user};password={_password}"))
                .Named<IBus>(Buses.ISSync)
                .InstancePerMatchingLifetimeScope(_scope);

            builder
                .Register(e => RabbitHutch.CreateBus($@"host={_host};timeout=120;virtualHost={Buses.SimpleMessaging};username={_user};password={_password}"))
                .Named<IBus>(Buses.SimpleMessaging)
                .InstancePerMatchingLifetimeScope(_scope);

            builder
                .Register(e => RabbitHutch.CreateBus($@"host={_host};timeout=120;virtualHost={Buses.BulkMessaging};username={_user};password={_password}"))
                .Named<IBus>(Buses.BulkMessaging)
                .InstancePerMatchingLifetimeScope(_scope);

            if (!string.IsNullOrWhiteSpace(_loopbackVHost))
            {
                builder
                    .Register(e => RabbitHutch.CreateBus($@"host={_host};timeout=120;virtualHost={_loopbackVHost};username={_user};password={_password}"))
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
