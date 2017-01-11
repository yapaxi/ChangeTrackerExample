using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class RuntimeMappingSchema
    {
        public RuntimeMappingSchema(MappingSchema schema)
        {
            Schema = schema;

            var buffer = new Dictionary<string, MappingProperty[]>();
            FindObjects(MappingSchema.RootName, this.Schema.Properties, buffer);
            Objects = buffer;
            FlatProperties = ConverToFlatProperties(buffer);
            TypeCache = FlatProperties.Select(e => e.Value.ClrType).Where(e => e != null).Distinct().ToDictionary(e => e, e => Type.GetType(e));
        }

        public IReadOnlyDictionary<string, Type> TypeCache { get; }
        public MappingSchema Schema { get; }
        public IReadOnlyDictionary<string, MappingProperty[]> Objects { get; }
        public IReadOnlyDictionary<string, MappingProperty> FlatProperties { get;}

        private static void FindObjects(string name, MappingProperty[] properties, Dictionary<string, MappingProperty[]> buffer)
        {
            buffer.Add(name, properties.Where(e => !e.Children.Any()).ToArray());
            foreach (var v in properties.Where(e => e.Children.Any()))
            {
                FindObjects(v.PathName, v.Children, buffer);
            }
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

    public class MappingSchema
    {
        public static readonly string RootName = "root";

        public MappingSchema(
            MappingProperty[] properties,
            long checksum,
            DateTime createdUTC)
        {
            this.Properties = properties;
            this.Checksum = checksum;
            this.CreatedUTC = createdUTC;
        }

        public MappingProperty[] Properties { get; }
        public long Checksum { get; }
        public DateTime CreatedUTC { get; }

    }
}
