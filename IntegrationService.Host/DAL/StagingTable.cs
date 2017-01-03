using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL
{
    public class StagingTable
    {
        public StagingTable(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
