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
        public static readonly string SCHEMA = "schema";
        public static readonly string SCHEMA_FORMAT_VERSION = "schema-format-version";
        public static readonly string SCHEMA_GEN_UTC = "schema-gen-utc";
        public static readonly string SCHEMA_CHECKSUM = "schema-checksum";
        public static readonly string SCHEMA_ENTITY_ID = "entity-id";
    }
}
