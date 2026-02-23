using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("Label")]
    public class Label : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)] // e.g. "#FF5733"
        public string? Color { get; set; }

        public virtual ICollection<ContactLabel> ContactLabels { get; set; } = [];
    }
}
