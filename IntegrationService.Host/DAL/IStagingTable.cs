using IntegrationService.Host.DAL.DDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL
{
    public interface IStagingTable
    {
        string FullName { get; }

        string SystemName { get; }

        IReadOnlyCollection<IStagingTable> Children { get; }

        IReadOnlyCollection<TableColumnDefinition> Columns { get; }
    }
}
