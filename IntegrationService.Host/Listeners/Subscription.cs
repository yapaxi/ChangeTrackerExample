﻿using Autofac;
using Common;
using EasyNetQ;
using EasyNetQ.Topology;
using IntegrationService.Host.Converters;
using IntegrationService.Host.DAL;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationService.Host.Listeners
{
    public partial class ListenerHost
    {
        private class Subscription : IDisposable
        {
            private int _disposed;

            private readonly IDisposable _consumerSubscription;
            private readonly IConverter _converter;
            private readonly IStagingTable _stagingTable;
            private readonly IReadOnlyDictionary<string, IStagingTable> _flattenTables;
            private readonly ListenerHost _host;
            private readonly string _queue;

            public Subscription(ListenerHost host, string queue, IConverter converter, IStagingTable stagingTable)
            {
                _host = host;
                _stagingTable = stagingTable;
                _converter = converter;
                _disposed = 0;
                _queue = queue;
                var buffer = new Dictionary<string, IStagingTable>();
                FlattenTo(stagingTable, buffer);
                _flattenTables = buffer;
                _consumerSubscription = _host._bus.Advanced.Consume(
                    new Queue(queue, false),
                    (data, properties, info) => ConsumerRoutine(data, properties, info)
                );
            }

            private void FlattenTo(IStagingTable staging, Dictionary<string, IStagingTable> buffer)
            {
                buffer.Add(staging.SystemName, staging);
                foreach (var v in staging.Children)
                {
                    FlattenTo(v, buffer);
                }
            }

            private void ConsumerRoutine(byte[] data, MessageProperties properties, MessageReceivedInfo info)
            {
                if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
                {
                    return;
                }

                try
                {
                    var id = (int)properties.Headers[ISMessageHeader.SCHEMA_ENTITY_ID];
                    Console.WriteLine($"consumed message: id={id},version={_converter.RuntimeSchema.Schema.Checksum}");
                    Console.WriteLine($"\tconverting...");
                    var objects = _converter.Convert(data);
                    using (var dataInsertionScope = _host._scope.BeginLifetimeScope())
                    {
                        Console.WriteLine($"\tinserting...");

                        foreach (var obj in objects)
                        {
                            var table = obj.Key == MappingSchema.RootName ? _stagingTable : _flattenTables[obj.Key];
                            foreach (var line in obj.Value)
                            {
                                dataInsertionScope.Resolve<DataRepository>().Insert(table.FullName, line);
                            }
                        }

                        Console.WriteLine($"\tinserted");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }

            public void Dispose()
            {
                Console.WriteLine($"Closing {this}");
                Interlocked.Exchange(ref _disposed, 1);
                _consumerSubscription.Dispose();
            }

            public override string ToString()
            {
                return $"Subscription for queue {_queue} to table {_stagingTable.FullName} for schema version {_converter.RuntimeSchema.Schema.Checksum}";
            }
        }
    }
}