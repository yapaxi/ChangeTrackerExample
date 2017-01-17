using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Runtime
{
    public interface IRuntimeMappingSchema
    {
        IReadOnlyDictionary<string, Type> TypeCache { get; }
        IReadOnlyDictionary<string, MappingProperty[]> Objects { get; }
        IReadOnlyDictionary<string, MappingProperty> FlatProperties { get; }
    }
}
