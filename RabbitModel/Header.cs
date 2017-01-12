using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitModel
{
    public static class LoopbackMessageHeader
    {
        public static readonly string MESSAGE_TYPE = "type";
    }

    public static class ISMessageHeader
    {
        public static readonly string SCHEMA_CHECKSUM = "tr-schema-checksum";
        public static readonly string ENTITY_COUNT = "tr-entity-count";
        public static readonly string BATCH_IS_LAST = "tr-butch-is-last";
        public static readonly string BATCH_ORDINAL = "tr-butch-ordinal";
        public static readonly string BATCH_COUNT = "tr-butch-count";
    }
}
