using Common;
using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Writers
{
    public interface IWriter<TData>
    {
        void Write(IEnumerable<TData> rootFlattenRepresentation, WriteDestination destination);
    }
}
