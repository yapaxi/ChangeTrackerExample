using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Services.Policy
{
    public class SerialExecutionAttribute : Attribute
    {
        public object Lock { get; } = new object();
    }
}
