using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using IntegrationService.Contracts.v1;

namespace IntegrationService.Client
{
    public class ISClient
    {
        private readonly IBus _bus;

        public ISClient(IBus bus)
        {
            _bus = bus;
        }

        public SyncMetadataResponse SyncMetadata(SyncMetadataRequest request)
        {
            return _bus.Request<SyncMetadataRequest, SyncMetadataResponse>(request);
        }
    }
}
