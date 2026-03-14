using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("Fact")]
    public class Fact : BaseEntity
    {
        public Guid? ContactId { get; set; }

        [ForeignKey(nameof(ContactId))]
        public virtual Contact? Contact { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Display(Name = "Value")]
        public string Value { get; set; } = string.Empty;
    }
}