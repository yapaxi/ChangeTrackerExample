using Common;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Subscriptions;

namespace IntegrationService.Host.Middleware
{
    public interface IRequestLifetimeHandler
    {
        TResponse Response<TRequest, TResponse>(TRequest request);

        void HandleDataMessage<TSource>(TSource message, MessageInfo messagInfo);
    }
}
