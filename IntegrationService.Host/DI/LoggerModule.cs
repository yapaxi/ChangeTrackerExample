using Autofac;
using Autofac.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DI
{
    abstract class LoggerModule<TLogger> : Module
        where TLogger : class
    {
        private readonly ILoggerFactory<TLogger> _loggerFactory;

        protected LoggerModule(ILoggerFactory<TLogger> loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            var type = registration.Activator.LimitType;
            if (HasCtorLoggerDependency(type))
            {
                registration.Preparing += (s, e) =>
                {
                    e.Parameters = e.Parameters.Concat(new[] { new TypedParameter(
                        typeof(TLogger),
                        _loggerFactory.CreateForType(e.Component.Activator.LimitType))
                    });
                };
            }

            base.AttachToComponentRegistration(componentRegistry, registration);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_loggerFactory).As<ILoggerFactory<TLogger>>();
            base.Load(builder);
        }

        private static bool HasCtorLoggerDependency(Type type)
        {
            return type.GetConstructors().Any(e => e.GetParameters().Any(q => q.ParameterType == typeof(TLogger)));
        }
    }
}
