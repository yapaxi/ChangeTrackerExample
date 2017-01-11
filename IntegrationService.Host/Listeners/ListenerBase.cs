using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners
{
    public abstract class ListenerBase
    {
        private readonly ILifetimeScope _scope;

        protected ListenerBase(ILifetimeScope scope)
        {
            _scope = scope;
        }

        protected TResult UsingScope<TResult>(Func<ILifetimeScope, TResult> action)
        {
            using (var scope = _scope.BeginLifetimeScope())
            {
                return action(scope);
            }
        }
        protected void UsingScope(Action<ILifetimeScope> action)
        {
            using (var scope = _scope.BeginLifetimeScope())
            {
                action(scope);
            }
        }
    }
}
