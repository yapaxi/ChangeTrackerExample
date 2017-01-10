﻿using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using RabbitModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using ResolvedMapping = System.Tuple<Common.MappingProperty, System.Type>;

namespace IntegrationService.Host.Converters
{
    public class FlatMessageConverter : IConverter
    {
        private readonly Dictionary<string, Type> _typeCache;
        private readonly Guid _runtimeId = Guid.NewGuid();
        public RuntimeMappingSchema RuntimeSchema { get; }

        public FlatMessageConverter(RuntimeMappingSchema runtimeSchema)
        {
            RuntimeSchema = runtimeSchema;
            _typeCache = RuntimeSchema.FlatProperties.Select(e => e.Value.ClrType).Where(e => e != null).Distinct().ToDictionary(e => e, e => Type.GetType(e));
        }

        public Dictionary<string, List<Dictionary<string, object>>> Convert(byte[] data)
        {
            var properties = RuntimeSchema.Objects.ToDictionary(e => e.Key, e => new List<Dictionary<string, object>>());

            using (var r = new JsonTextReader(new StringReader(Encoding.Unicode.GetString(data))))
            {
                string currentProperty = null;
                var pathStack = new Stack<string>();
                var lineStack = new Stack<Dictionary<string, object>>();
                while (r.Read())
                {
                    switch (r.TokenType)
                    {
                        case JsonToken.String:
                            lineStack.Peek().Add(currentProperty, r.Value);
                            break;
                        case JsonToken.Boolean:
                            lineStack.Peek().Add(currentProperty, r.Value);
                            break;
                        case JsonToken.Date:
                            lineStack.Peek().Add(currentProperty, r.Value);
                            break;
                        case JsonToken.Float:
                        case JsonToken.Integer:
                            var path = RemoveArrayElement(r.Path);
                            MappingProperty mapping;
                            if (!RuntimeSchema.FlatProperties.TryGetValue(path, out mapping))
                            {
                                Console.WriteLine($"[{_runtimeId}] Unexpected property: {path}. Convertion is aborted.");
                                return null;
                            }
                            lineStack.Peek().Add(currentProperty, System.Convert.ChangeType(r.Value, _typeCache[mapping.ClrType]));
                            break;
                        case JsonToken.PropertyName:
                            currentProperty = (string)r.Value;
                            break;
                        case JsonToken.StartObject:
                            pathStack.Push(string.IsNullOrWhiteSpace(r.Path) ? MappingSchema.RootName : RemoveArrayElement(r.Path));
                            lineStack.Push(new Dictionary<string, object>());
                            break;
                        case JsonToken.EndObject:
                            var objectName = pathStack.Peek();
                            properties[objectName].Add(lineStack.Pop());
                            pathStack.Pop();
                            break;
                        case JsonToken.StartArray:
                        case JsonToken.EndArray:
                            break;
                        case JsonToken.Null:
                            break;
                        default:
                            Console.WriteLine($"[{_runtimeId}] Unexpected token: {r.TokenType}. Convertion is aborted.");
                            return null;
                    }
                }
            }

            return properties;
        }

        private static string RemoveArrayElement(string path)
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

            return builder.ToString();
        }
    }
}