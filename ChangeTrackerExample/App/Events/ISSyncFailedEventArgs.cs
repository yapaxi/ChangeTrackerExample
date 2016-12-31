using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App.Events
{
    public class ISSyncFailedEventArgs : EventArgs
    {
        public int TotalEntities { get; set; }

        public int SucceededEntities { get; set; }

        public int FailedByBusinessReasons { get; set; }

        public int FailedByTemporaryReasons { get; set; }

        public int FailedByUnexpectedReasons { get; set; }

        public string[] Messages { get; set; }

        public int TryCount { get; set; }
    }
}
