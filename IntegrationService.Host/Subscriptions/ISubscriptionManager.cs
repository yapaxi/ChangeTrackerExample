using Common;
using Common.Runtime;
using IntegrationService.Host.DAL;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Subscriptions
{
    public interface ISubscriptionManager
    {
        bool SubscriptionExists(string name, DataMode mode);
        void SubscribeOnMetadataSync();
        void SubscribeOnDataFlow(DataMode mode, string entityName, string queue, IRuntimeMappingSchema schema, IWriteDestination destination);
        void CloseAllEntitySubscriptions(string entityName);
    }
}
