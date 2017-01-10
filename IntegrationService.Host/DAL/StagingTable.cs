using Newtonsoft.Json;
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
    }

    public class StagingTable : IStagingTable
    {
        public string FullName { get; set; }
        public string SystemName { get; set; }

        public List<StagingTable> Children { get; set; }


        [JsonIgnore]
        IReadOnlyCollection<IStagingTable> IStagingTable.Children => this.Children;
    }
}
