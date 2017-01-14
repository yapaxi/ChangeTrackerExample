using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Services
{
    interface IRequestResponseService<TRequest, TResponse>
    {
        TResponse Response(TRequest request);
    }
}
