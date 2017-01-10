using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class ParentChildConfiguration
    {
        internal ParentChildConfiguration(string foreignKeyProperty)
        {
            ForeignKeyProperty = foreignKeyProperty;
        }
        public string ForeignKeyProperty { get; }
    }
}
