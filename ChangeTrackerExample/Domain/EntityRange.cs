using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Domain
{
    public class EntityRange
    {
        public EntityRange(int minId, int maxId)
        {
            this.MinId = minId;
            this.MaxId = maxId;
        }

        public int MinId { get; }
        public int MaxId { get; }

        public int Length => MaxId - MinId + 1;
    }
}
