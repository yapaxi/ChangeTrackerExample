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
    public class BulkWriter : IWriter<IEnumerable<FlatMessage>>
    {
        private readonly DataRepository _repository;
        private readonly WriteDestination _destination;

        public BulkWriter(DataRepository repository, WriteDestination destination)
        {
            _repository = repository;
            _destination = destination;
        }

        public void Write(IEnumerable<FlatMessage> rootFlattenRepresentation)
        {

        }
    }
}
