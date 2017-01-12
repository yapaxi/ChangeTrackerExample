﻿using Common;
using IntegrationService.Host.Converters;
using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Writers
{
    public class RowByRowWriter : IWriter<FlatMessage>
    {
        private readonly DataRepository _repository;

        public RowByRowWriter(DataRepository repository)
        {
            _repository = repository;
        }

        public void Write(IEnumerable<FlatMessage> messages, WriteDestination destination)
        {
            Console.WriteLine($"\tinserting...");
            Console.Write("\t");
            using (var tran = _repository.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                foreach (var message in messages)
                {
                    foreach (var messageElement in message.Payload)
                    {
                        var table = destination.FlattenTables[messageElement.Key];
                        Console.Write(table.SystemName + "... ");
                        foreach (var line in messageElement.Value)
                        {
                            _repository.Merge(table.FullName, line);
                        }
                    }
                }

                tran.Commit();
            }
            Console.WriteLine($"\n\tinserted");
        }
    }
}
