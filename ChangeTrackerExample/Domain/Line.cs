using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Domain
{
    public class Line : IEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(128)]
        [Column(TypeName = "varchar")]
        public string String { get; set; }

        public int EntityId { get; set; }

        [ForeignKey(nameof(EntityId))]
        public SomeEntity Entity { get; set; }
    }

    public class SuperLine : IEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(256)]
        [Column(TypeName = "nvarchar")]
        public string SuperString { get; set; }

        public int EntityId { get; set; }

        [ForeignKey(nameof(EntityId))]
        public SomeEntity Entity { get; set; }
    }
}
