using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL.DDL
{
    public class TableColumnDefinition
    {
        [JsonIgnore]
        private Type _type;

        [JsonIgnore]
        private Type _unwrappedType;

        public TableColumnDefinition(string name, string clrType, string sqlType, bool isNullable)
        {
            this.Name = name;
            this.SqlType = sqlType;
            this.IsNullable = isNullable;
            this.ClrType = clrType;
        }

        public string Name { get; }
        public string SqlType { get; }
        public string ClrType { get; }
        public bool IsNullable { get; }

        [JsonIgnore]
        public Type Type => _type ?? (_type = Type.GetType(ClrType));

        [JsonIgnore]
        public Type UnwrappedType => _unwrappedType ?? (_unwrappedType = Unwrap(Type.GetType(ClrType)));

        private static Type Unwrap(Type t)
        {
            if (t.IsGenericType)
            {
                return t.GetGenericArguments()[0];
            }

            return t;
        }
    }
}
