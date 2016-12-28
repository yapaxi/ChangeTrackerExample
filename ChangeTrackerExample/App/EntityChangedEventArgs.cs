using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App
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
}
