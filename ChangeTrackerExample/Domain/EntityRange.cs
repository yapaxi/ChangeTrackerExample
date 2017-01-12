using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Domain
{
    public class EntityRange
    {
        public int MinId { get; set; }
        public int MaxId { get; set; }

        public int Length => MaxId - MinId + 1;
    }
}
