using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("Pet")]
    public class Pet : BaseEntity
    {
        [Required]
        public Guid ContactId { get; set; }

        [ForeignKey(nameof(ContactId))]
        public virtual Contact Contact { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        [Display(Name = "Species")]
        public string? Species { get; set; }

        [MaxLength(100)]
        [Display(Name = "Breed")]
        public string? Breed { get; set; }

        [Display(Name = "Birthday")]
        public DateTime? Birthday { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}
