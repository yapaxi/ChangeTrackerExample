using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace IntegrationService.Host.DI
{
    internal class NLogFactory : ILoggerFactory<ILogger>
    {
        public ILogger CreateForType(Type type)
        {
            return LogManager.GetLogger(type.FullName);
        }
    }
}
