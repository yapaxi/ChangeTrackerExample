using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App.Events
{
    public class EntityChangedEventArgs : EventArgs
    {
        public EntityChangedEventArgs(int id, string type)
        {
            this.Id = id;
            this.Type = type;
        }

        public int Id { get; }
        public string Type { get; }
    }

    public class EntityRangeEventArgs : EventArgs
    {
        public EntityRangeEventArgs(int fromId, int toId, string type)
        {
            this.FromId = fromId;
            this.ToId = toId;
            this.Type = type;
        }

        public int FromId { get; }
        public int ToId { get; }
        public string Type { get; }
    }
}
