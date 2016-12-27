using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Domain
{
    public class SomeEntity : IEntity
    {
        public int Id { get; set; }

        [MaxLength(128)]
        public string ShortString { get; set; }

        public string MaxString { get; set; }

        public int Int32 { get; set; }

        public long Int64 { get; set; }
        
        public Guid? Guid { get; set; }
    }
}
