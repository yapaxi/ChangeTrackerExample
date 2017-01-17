using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Runtime
{
    public class RuntimeMappingSchema : IRuntimeMappingSchema
    {
        public RuntimeMappingSchema(MappingSchema schema)
        {
            var buffer = new Dictionary<string, MappingProperty[]>();
            FindObjects(MappingSchema.RootName, schema.Properties, buffer);
            Objects = buffer;
            FlatProperties = ConverToFlatProperties(buffer);
            TypeCache = FlatProperties.Select(e => e.Value.ClrType).Where(e => e != null).Distinct().ToDictionary(e => e, e => Unwrap(Type.GetType(e)));
        }

        public IReadOnlyDictionary<string, Type> TypeCache { get; }
        public IReadOnlyDictionary<string, MappingProperty[]> Objects { get; }
        public IReadOnlyDictionary<string, MappingProperty> FlatProperties { get; }

        private static void FindObjects(string name, MappingProperty[] properties, Dictionary<string, MappingProperty[]> buffer)
        {
            buffer.Add(name, properties.Where(e => !e.Children.Any()).ToArray());
            foreach (var v in properties.Where(e => e.Children.Any()))
            {
                FindObjects(v.PathName, v.Children, buffer);
            }
        }

        private static Type Unwrap(Type t)
        {
            if (t.IsGenericType)
            {
                return t.GetGenericArguments()[0];
            }

            return t;
        }

        private static Dictionary<string, MappingProperty> ConverToFlatProperties(Dictionary<string, MappingProperty[]> buffer)
        {
            var flatProperties = new Dictionary<string, MappingProperty>();
            foreach (var v in buffer)
            {
                foreach (var p in v.Value)
                {
                    flatProperties.Add(p.PathName, p);
                }
            }

            return flatProperties;
        }
    }

}
