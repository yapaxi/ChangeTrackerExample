using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class MappingSchema
    {
        public MappingSchema(IReadOnlyCollection<MappingProperty> properties, long checksum, DateTime createdUTC)
        {
            this.Properties = properties;
            this.Checksum = checksum;
            this.CreatedUTC = createdUTC;
        }

        public IReadOnlyCollection<MappingProperty> Properties { get; }
        public long Checksum { get; }
        public DateTime CreatedUTC { get; }
    }
}
