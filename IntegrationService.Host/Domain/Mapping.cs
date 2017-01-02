using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Domain
{
    public class Mapping
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id { get; set; }

        [Required]
        [MaxLength(512)]
        public virtual string Name { get; set; }

        public virtual long Checksum { get; set; }

        [Required]
        [MaxLength(512)]
        public virtual string QueueName { get; set; }

        [Required]
        public virtual string Schema { get; set; }

        public virtual DateTime CreatedAt { get; set; }

        public virtual bool IsActive { get; set; }

        public virtual DateTime? DeactivatedAt { get; set; }
    }
}
