using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using IntegrationService.Host.DAL;
using RabbitModel;

namespace IntegrationService.Host.Listeners
{
    public class RawMessageHandler
    {
        private readonly Guid _runtimeId = Guid.NewGuid();
        private readonly MappingProperty[] _schemaProperties;
        private readonly StagingTable _stagingTable;

        public RawMessageHandler(MappingProperty[] schemaProperties, StagingTable stagingTable)
        {
            this._schemaProperties = schemaProperties;
            this._stagingTable = stagingTable;
        }

        public void Handle(byte[] data, MessageProperties properties, MessageReceivedInfo info)
        {
            var id = (int)properties.Headers[ISMessageHeader.SCHEMA_ENTITY_ID];
            Console.WriteLine($"[{_runtimeId}] Handler accepted message: id={id}");
        }
    }
}
