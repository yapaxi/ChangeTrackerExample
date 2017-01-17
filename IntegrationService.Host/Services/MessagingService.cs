using Autofac;
using Common;
using EasyNetQ.Topology;
using IntegrationService.Host.Converters;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Subscriptions;
using NLog;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Services
{
    public class MessagingService : 
        IMessagingService<RawMessage>,
        IMessagingService<IReadOnlyCollection<RawMessage>>
    {
        private readonly DataRepository _repository;
        private readonly FlatMessageConverter _converter;
        private readonly ILogger _logger;

        public MessagingService(FlatMessageConverter converter, DataRepository repository, ILogger logger)
        {
            _repository = repository;
            _converter = converter;
            _logger = logger;
        }

        public void WriteMessage(RawMessage rawMessage, MessageInfo info)
        {
            using (var tran = _repository.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                WriteSingleMessage(rawMessage, info);
                tran.Commit();
            }
        }

        private void WriteSingleMessage(RawMessage rawMessage, MessageInfo info)
        {
            var messages = _converter.Convert(rawMessage, info.Schema);

            foreach (var message in messages.TablesWithData)
            {
                var table = info.Destination.FlattenTables[message.Key];
                _logger.Debug($"inserting line into {table.SystemName}");
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
            WriteElapsed("convert duration", sw);
            foreach (var tableData in roots.TablesWithData)
            {
                var table = info.Destination.FlattenTables[tableData.Key];
                _repository.BulkInsert(table, tableData.Value);
            }
            WriteElapsed("insert duration", sw);
            sw.Stop();
        }

        private void WriteElapsed(string name, Stopwatch sw)
        {
            _logger.Debug($"{name}: {sw.Elapsed}");
            sw.Restart();
        }
    }
}
