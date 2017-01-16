using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.DAL
{
    public interface IWriteDestination
    {
        IReadOnlyDictionary<string, IStagingTable> FlattenTables { get; }
        IStagingTable StagingTable { get; }
    }

    public class WriteDestination : IWriteDestination
    {
        private readonly IStagingTable _stagingTable;
        private readonly Dictionary<string, IStagingTable> _flattenTables;

        public WriteDestination(IStagingTable stagingTable)
        {
            _flattenTables = new Dictionary<string, IStagingTable>();
            _stagingTable = stagingTable;

            Flatten(stagingTable, MappingSchema.RootName);
        }

        public IReadOnlyDictionary<string, IStagingTable> FlattenTables => _flattenTables;
        public IStagingTable StagingTable => _stagingTable;

        private void Flatten(IStagingTable staging, string overrideName = null)
        {
            _flattenTables.Add(overrideName ?? staging.SystemName, staging);

            foreach (var child in staging.Children)
            {
                Flatten(child);
            }
        }
    }
}
