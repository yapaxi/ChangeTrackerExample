using Autofac;
using Common;
using EasyNetQ.Topology;
using IntegrationService.Host.Converters;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Subscriptions;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Services
{
    public interface IMessagingService<TMessage>
    {
        void WriteMessage(TMessage rawMessage, MessageInfo info);
    }

    public class MessagingService : 
        IMessagingService<RawMessage>,
        IMessagingService<IEnumerable<RawMessage>>
    {
        private readonly DataRepository _repository;
        private readonly FlatMessageConverter _converter;

        public MessagingService(FlatMessageConverter converter, DataRepository repository)
        {
            _repository = repository;
            _converter = converter;
        }

        public void WriteMessage(RawMessage rawMessage, MessageInfo info)
        {
            Console.WriteLine($"\tinserting...");
            Console.Write("\t");

            using (var tran = _repository.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                WriteSingleMessage(rawMessage, info);
                tran.Commit();
            }
            Console.WriteLine($"\n\tinserted");
        }

        private void WriteSingleMessage(RawMessage rawMessage, MessageInfo info)
        {
            var messages = _converter.Convert(rawMessage, info.Schema);

            foreach (var message in messages)
            {
                foreach (var messageElement in message.Payload)
                {
                    var table = info.Destination.FlattenTables[messageElement.Key];
                    Console.Write(table.SystemName + "... ");
                    foreach (var line in messageElement.Value)
                    {
                        _repository.Merge(table.FullName, line);
                    }
                }
            }
        }

        public void WriteMessage(IEnumerable<RawMessage> rawMessage, MessageInfo info)
        {
            var converter = new FlatMessageConverter();

            var roots = converter.Convert(rawMessage, info.Schema);

            foreach (var k in roots.SelectMany(e => e.Payload).GroupBy(e => e.Key))
            {
                var agg = k.Select(e => e.Value).Aggregate(new List<Dictionary<string, object>>(), (a, b) => { a.AddRange(b); return a; });
                var table = info.Destination.FlattenTables[k.Key];
                _repository.BulkInsert(table, agg);
            }
        }
    }
}
