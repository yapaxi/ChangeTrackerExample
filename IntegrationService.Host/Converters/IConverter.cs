using Common;
using Common.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Converters
{
    public interface IConverter<TSource, TResult>
    {
        TResult Convert(TSource data, IRuntimeMappingSchema runtimeSchema);
    }
}
