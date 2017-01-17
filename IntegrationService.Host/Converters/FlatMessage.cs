using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Converters
{
    public class FlatMessage
    {
        public FlatMessage(IReadOnlyDictionary<string, List<Dictionary<string, object>>> tablesWithData)
        {
            TablesWithData = tablesWithData;
        }

        public IReadOnlyDictionary<string, List<Dictionary<string, object>>> TablesWithData { get; }
    }
}
