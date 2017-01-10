using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Converters
{
    interface IConverter
    {
        RuntimeMappingSchema RuntimeSchema { get; }

        Dictionary<string, List<Dictionary<string, object>>> Convert(byte[] data);
    }
}
