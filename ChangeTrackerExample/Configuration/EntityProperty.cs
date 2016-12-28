using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class EntityProperty
    {
        public const int VERSION = 1;

        public string Name { get; set; }
        public string Type { get; set; }
        public int? Size { get; set; }
        public IReadOnlyCollection<EntityProperty> Children { get; set; }
    }
}
