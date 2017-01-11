using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Converters
{
    public interface IConverter<TSource, TResult>
    {
        RuntimeMappingSchema RuntimeSchema { get; }

        TResult Convert(TSource data);
    }
}
