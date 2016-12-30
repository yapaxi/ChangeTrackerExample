using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class MappingProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int? Size { get; set; }
        public IReadOnlyCollection<MappingProperty> Children { get; set; }
    }
}
