using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DI
{
    public interface ILoggerFactory<TLogger>
        where TLogger : class
    {
        TLogger CreateForType(Type type);
    }
}
