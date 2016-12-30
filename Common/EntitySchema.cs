using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class EntitySchema
    {
        public IReadOnlyCollection<EntityProperty> Properties { get; set; }
        public long Checksum { get; set; }
        public DateTime CreatedUTC { get; set; }
    }
}
