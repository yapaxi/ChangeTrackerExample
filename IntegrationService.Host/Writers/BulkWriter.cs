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

        public BulkWriter(DataRepository repository)
        {
            _repository = repository;
        }

        public void Write(IEnumerable<FlatMessage> rootFlattenRepresentation, WriteDestination destination)
        {

        }
    }
}
