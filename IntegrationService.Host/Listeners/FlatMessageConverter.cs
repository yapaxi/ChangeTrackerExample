using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using IntegrationService.Host.DAL;
using RabbitModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using ResolvedMapping = System.Tuple<Common.MappingProperty, System.Type>;
using System.Data.SqlClient;

namespace IntegrationService.Host.Listeners
{
    public class FlatMessageConverter
    {
        private readonly Guid _runtimeId = Guid.NewGuid();
        private readonly Dictionary<string, ResolvedMapping> _schemaProperties;

        public FlatMessageConverter(MappingProperty[] schemaProperties)
        {
            this._schemaProperties = schemaProperties.ToDictionary(e => e.Name, e => new ResolvedMapping(e, Type.GetType(e.ClrType)));
        }

        public SqlParameter[] Convert(byte[] data, MessageProperties properties, MessageReceivedInfo info)
        {
            var id = (int)properties.Headers[ISMessageHeader.SCHEMA_ENTITY_ID];
            var lst = new List<SqlParameter>(_schemaProperties.Count);

            Console.WriteLine($"[{_runtimeId}] Executing converter for message: id={id}");

            using (var r = new JsonTextReader(new StringReader(Encoding.Unicode.GetString(data))))
            {
                int level = 0;
                string propertyName = null;
                ResolvedMapping mapping = null;
                while (r.Read())
                {
                    switch (r.TokenType)
                    {
                        case JsonToken.String:
                            lst.Add(new SqlParameter(propertyName, r.Value));
                            break;
                        case JsonToken.Boolean:
                            lst.Add(new SqlParameter(propertyName, r.Value));
                            break;
                        case JsonToken.Date:
                            lst.Add(new SqlParameter(propertyName, r.Value));
                            break;
                        case JsonToken.Float:
                        case JsonToken.Integer:
                            lst.Add(new SqlParameter(propertyName, System.Convert.ChangeType(r.Value, mapping.Item2)));
                            break;
                        case JsonToken.PropertyName:
                            propertyName = (string)r.Value;
                            if (!_schemaProperties.TryGetValue(propertyName, out mapping))
                            {
                                Console.WriteLine($"[{_runtimeId}] Unexpected property name: {propertyName}. Property is ignored.");
                                r.Skip();
                            }
                            break;
                        case JsonToken.StartObject:
                            if (level != 0)
                            {
                                Console.WriteLine($"[{_runtimeId}] Unexpected nested level: {level}. Insertion is aborted.");
                            }
                            else
                            {
                                ++level;
                            }
                            break;
                        case JsonToken.EndObject:
                            --level;
                            break;
                        default:
                            Console.WriteLine($"[{_runtimeId}] Unexpected token: {r.TokenType}. Insertion is aborted.");
                            return null;
                    }
                }
            }

            return lst.ToArray();
        }
    }
}
