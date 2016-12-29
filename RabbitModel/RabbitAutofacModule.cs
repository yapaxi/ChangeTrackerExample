using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using EasyNetQ;

namespace RabbitModel
{
    public class RabbitAutofacModule : Module
    {
        private readonly string _connectionString;

        private readonly IReadOnlyDictionary<string, string> _parsedConnectionString;

        public RabbitAutofacModule(string connectionString)
        {
            _connectionString = connectionString;
            _parsedConnectionString = 
                _connectionString.Split(';')
                .Select(e => e.Split('='))
                .ToDictionary(e => e[0].Trim(), e => e[1].Trim());
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register(e => RabbitHutch.CreateBus(_connectionString))
                .As<IBus>()
                .InstancePerLifetimeScope();

            base.Load(builder);
        }

        private string GetOrNull(string key)
        {
            string val;
            return _parsedConnectionString.TryGetValue(key, out val) ? val : null;
        }
    }
}
