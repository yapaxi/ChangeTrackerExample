using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App.Events
{
    public class FullRebuildRequiredEventArgs : EventArgs
    {
        public FullRebuildRequiredEventArgs(string sourceTypeClassName)
        {
            SourceTypeClassName = sourceTypeClassName;
        }

        public string SourceTypeClassName { get; }
    }
}
