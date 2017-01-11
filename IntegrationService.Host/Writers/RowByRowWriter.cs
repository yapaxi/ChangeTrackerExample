using Common;
using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Writers
{
    public class RowByRowWriter
    {
        private readonly DataRepository _repository;
        private readonly WriteDestination _destination;

        public RowByRowWriter(DataRepository repository, WriteDestination destination)
        {
            _repository = repository;
            _destination = destination;
        }

        public void Write(Dictionary<string, List<Dictionary<string, object>>> rootFlattenRepresentation)
        {
            Console.WriteLine($"\tinserting...");
            Console.Write("\t");
            using (var tran = _repository.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                foreach (var rootElement in rootFlattenRepresentation)
                {
                    var table = _destination.FlattenTables[rootElement.Key];
                    Console.Write(table.SystemName + "... ");
                    foreach (var line in rootElement.Value)
                    {
                        _repository.Merge(table.FullName, line);
                    }
                }

                tran.Commit();
            }
            Console.WriteLine($"\n\tinserted");
        }
    }
}
