using IntegrationService.Host.DAL.DDL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL
{
   
    public class StagingTable : IStagingTable
    {
        public string FullName { get; set; }
        public string SystemName { get; set; }

        public List<StagingTable> Children { get; set; }

        public TableColumnDefinition[] Columns { get; set; }

        [JsonIgnore]
        IReadOnlyCollection<IStagingTable> IStagingTable.Children => this.Children;

        [JsonIgnore]
        IReadOnlyCollection<TableColumnDefinition> IStagingTable.Columns => this.Columns;
    }
}
