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
using IntegrationService.Host.Listeners.Data;

namespace IntegrationService.Host.Converters
{
    public class FlatMessageConverter : 
        IConverter<IEnumerable<RawMessage>, FlatMessage>,
        IConverter<RawMessage, FlatMessage>
    {
        public IEnumerable<FlatMessage> Convert(RawMessage data, RuntimeMappingSchema runtimeSchema)
        {
            return Convert(Encoding.Unicode.GetString(data.Body), runtimeSchema).ToArray();
        }

        public IEnumerable<FlatMessage> Convert(IEnumerable<RawMessage> data, RuntimeMappingSchema runtimeSchema)
        {
            foreach (var messageGroup in data)
            {
                foreach (var flatMessage in Convert(messageGroup, runtimeSchema))
                {
                    yield return flatMessage;
                }
            }
        }

        private IEnumerable<FlatMessage> Convert(string json, RuntimeMappingSchema runtimeSchema)
        {
            var properties = CreateBlankProperties(runtimeSchema);

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
                                yield return new FlatMessage(properties);
                                properties = CreateBlankProperties(runtimeSchema);
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

        private static Dictionary<string, List<Dictionary<string, object>>> CreateBlankProperties(RuntimeMappingSchema runtimeSchema)
        {
            return runtimeSchema.Objects.ToDictionary(e => e.Key, e => new List<Dictionary<string, object>>());
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
    }
}
