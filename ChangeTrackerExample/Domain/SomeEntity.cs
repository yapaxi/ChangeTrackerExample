using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Domain
{
    public class SomeEntity : IEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(128)]
        [Column(TypeName = "varchar")]
        public string ShortString { get; set; }

        [Column(TypeName = "nvarchar")]
        public string MaxString { get; set; }

        public int Int32 { get; set; }

        public long Int64 { get; set; }
        
        public Guid? Guid { get; set; }

        public ICollection<Line> Lines { get; set; }

        public ICollection<SuperLine> SuperLines { get; set; }
    }
}
