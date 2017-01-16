using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using RabbitModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using IntegrationService.Host.Subscriptions;

namespace IntegrationService.Host.Converters
{
    public class FlatMessageConverter :
        IConverter<IReadOnlyCollection<RawMessage>, FlatMessage>,
        IConverter<RawMessage, FlatMessage>
    {
        public FlatMessage Convert(RawMessage data, RuntimeMappingSchema runtimeSchema)
        {
            var properties = CreateBlankProperties(runtimeSchema, data.EntityCount);
            ConvertJTR(Encoding.Unicode.GetString(data.Body), runtimeSchema, properties);
            return new FlatMessage(properties);
        }

        public FlatMessage Convert(IReadOnlyCollection<RawMessage> data, RuntimeMappingSchema runtimeSchema)
        {
            var properties = CreateBlankProperties(runtimeSchema, data.Sum(e => e.EntityCount));
            foreach (var messageGroup in data)
            {
                ConvertJTR(Encoding.Unicode.GetString(messageGroup.Body), runtimeSchema, properties);
            }
            return new FlatMessage(properties);
        }

        private void ConvertJTR(string json, RuntimeMappingSchema runtimeSchema, Dictionary<string, List<Dictionary<string, object>>> properties)
        {
            using (var r = new JsonTextReader(new StringReader(json)))
            {
                string currentProperty = null;
                var pathStack = new Stack<string>();
                var lineStack = new Stack<Dictionary<string, object>>();
                while (r.Read())
                {
                    switch (r.TokenType)
                    {
                        case JsonToken.String:
                            {
                                var path = ClearPath(r.Path);
                                MappingProperty mapping;
                                if (!runtimeSchema.FlatProperties.TryGetValue(path, out mapping))
                                {
                                    throw new Exception($"[{nameof(FlatMessageConverter)}] Unexpected property: {path}. Convertion is aborted.");
                                }
                                if (runtimeSchema.TypeCache[mapping.ClrType] == typeof(Guid))
                                {
                                    lineStack.Peek().Add(currentProperty, Guid.Parse((string)r.Value));
                                }
                                else
                                {
                                    lineStack.Peek().Add(currentProperty, r.Value);
                                }
                            }
                            break;
                        case JsonToken.Boolean:
                            lineStack.Peek().Add(currentProperty, r.Value);
                            break;
                        case JsonToken.Date:
                            lineStack.Peek().Add(currentProperty, r.Value);
                            break;
                        case JsonToken.Float:
                        case JsonToken.Integer:
                            {
                                var path = ClearPath(r.Path);
                                MappingProperty mapping;
                                if (!runtimeSchema.FlatProperties.TryGetValue(path, out mapping))
                                {
                                    throw new Exception($"[{nameof(FlatMessageConverter)}] Unexpected property: {path}. Convertion is aborted.");
                                }
                                lineStack.Peek().Add(currentProperty, System.Convert.ChangeType(r.Value, runtimeSchema.TypeCache[mapping.ClrType]));
                            }
                            break;
                        case JsonToken.PropertyName:
                            currentProperty = (string)r.Value;
                            break;
                        case JsonToken.StartObject:
                            pathStack.Push(r.Depth == 1 ? MappingSchema.RootName : ClearPath(r.Path));
                            lineStack.Push(new Dictionary<string, object>());
                            break;
                        case JsonToken.EndObject:
                            var objectName = pathStack.Peek();
                            properties[objectName].Add(lineStack.Pop());
                            pathStack.Pop();
                            if (r.Depth == 1)
                            {
                                pathStack.Clear();
                                lineStack.Clear();
                                currentProperty = null;
                            }
                            break;
                        case JsonToken.StartArray:
                            break;
                        case JsonToken.EndArray:
                            break;
                        case JsonToken.Null:
                            break;
                        default:
                            throw new Exception($"[{nameof(FlatMessageConverter)}] Unexpected token: {r.TokenType}. Convertion is aborted.");
                    }
                }
            }
        }

        private static Dictionary<string, List<Dictionary<string, object>>> CreateBlankProperties(RuntimeMappingSchema runtimeSchema, int estimatedEntitiesCount)
        {
            return runtimeSchema.Objects.ToDictionary(e => e.Key, e => new List<Dictionary<string, object>>(estimatedEntitiesCount));
        }

        private static string ClearPath(string path)
        {
            var builder = new StringBuilder();
            var inBracket = false;
            for (int i = 0; i < path.Length; i++)
            {
                var c = path[i];

                switch (c)
                {
                    case '[':
                        inBracket = true;
                        break;
                    case ']':
                        inBracket = false;
                        break;
                    default:
                        if (!inBracket)
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }

            return builder.ToString().TrimStart('.');
        }

        #region SLOW PARSER VERSION 

        private void ConvertJO(string json, RuntimeMappingSchema runtimeSchema, Dictionary<string, List<Dictionary<string, object>>> buffer)
        {
            EnterArray(JArray.Parse(json), runtimeSchema, buffer);
        }

        private static void EnterArray(JToken ja, RuntimeMappingSchema runtimeSchema, Dictionary<string, List<Dictionary<string, object>>> buffer)
        {
            foreach (JToken jae in ja)
            {
                switch (jae.Type)
                {
                    case JTokenType.Object:
                        EnterObject(jae, runtimeSchema, buffer);
                        break;
                    default:
                        throw new InvalidOperationException($"Only array of objects is supported: {ClearPath(ja.Path)}");
                }
            }
        }

        private static void EnterObject(JToken jae, RuntimeMappingSchema runtimeSchema, Dictionary<string, List<Dictionary<string, object>>> buffer)
        {
            var path = ClearPath(jae.Path);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = MappingSchema.RootName;
            }
            var simpleData = new Dictionary<string, object>();
            buffer[path].Add(simpleData);
            foreach (JProperty jo in jae.Children())
            {
                switch (jo.Value.Type)
                {
                    case JTokenType.Object:
                        EnterObject(jo.Value, runtimeSchema, buffer);
                        break;
                    case JTokenType.Array:
                        EnterArray(jo.Value, runtimeSchema, buffer);
                        break;
                    case JTokenType.Null:
                        break;
                    default:
                        var valuePath = ClearPath(jo.Value.Path);
                        MappingProperty mapping;
                        if (runtimeSchema.FlatProperties.TryGetValue(valuePath, out mapping))
                        {
                            simpleData.Add(jo.Name, ConvertValue(valuePath, (JValue)jo.Value, runtimeSchema.TypeCache[mapping.ClrType]));
                        }
                        break;
                }
            }
        }

        private static object ConvertValue(string path, JValue value, Type clrType)
        {
            switch (value.Type)
            {
                case JTokenType.String:
                    {
                        if (clrType == typeof(Guid))
                        {
                            return Guid.Parse(value.Value<string>());
                        }
                        else
                        {
                            return value.Value;
                        }
                    }
                case JTokenType.Float:
                case JTokenType.Integer:
                    {
                        return value.ToObject(clrType);
                    }
                default:
                    return value.Value;
            }
        }

        #endregion

    }
}
