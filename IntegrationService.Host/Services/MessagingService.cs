using Autofac;
using Common;
using EasyNetQ.Topology;
using IntegrationService.Host.Converters;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Subscriptions;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        IMessagingService<IReadOnlyCollection<RawMessage>>
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

            foreach (var message in messages.Payload)
            {
                var table = info.Destination.FlattenTables[message.Key];
                Console.Write(table.SystemName + "... ");
                foreach (var line in message.Value)
                {
                    _repository.Merge(table.FullName, line);
                }
            }
        }

        public void WriteMessage(IReadOnlyCollection<RawMessage> rawMessage, MessageInfo info)
        {
            var sw = new Stopwatch();
            sw.Start();
            var converter = new FlatMessageConverter();
            var roots = converter.Convert(rawMessage, info.Schema);
            WriteElapsed(sw);
            GroupedWrite(info, roots);
            WriteElapsed(sw);
            sw.Stop();
        }

        private static void WriteElapsed(Stopwatch sw)
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(sw.Elapsed);
            Console.ForegroundColor = c;
            sw.Restart();
        }

        private void GroupedWrite(MessageInfo info, FlatMessage roots)
        {
            foreach (var k in roots.Payload)
            {
                var table = info.Destination.FlattenTables[k.Key];
                _repository.BulkInsert(table, k.Value);
            }
        }
    }
}
