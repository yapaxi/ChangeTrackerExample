using Common;
using IntegrationService.Host.Converters;
using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Writers
{
    public class BulkWriter : IWriter<FlatMessage>
    {
        private readonly DataRepository _repository;

        public BulkWriter(DataRepository repository)
        {
            _repository = repository;
        }

        public void Write(IEnumerable<FlatMessage> roots, WriteDestination destination)
        {
            foreach (var k in roots.SelectMany(e => e.Payload).GroupBy(e => e.Key))
            {
                var agg = k.Select(e => e.Value).Aggregate(new List<Dictionary<string, object>>(), (a, b) => { a.AddRange(b); return a; });
                var table = destination.FlattenTables[k.Key];
                _repository.BulkInsert(table, agg);
            }
        }
    }
}
