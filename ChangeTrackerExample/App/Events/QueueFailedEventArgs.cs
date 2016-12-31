using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App.Events
{
    public class QueueFailedEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public int TryCount { get; set; }
    }
}
