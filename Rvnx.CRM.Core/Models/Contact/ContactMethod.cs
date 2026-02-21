using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("ContactMethod")]
    public class ContactMethod : BaseEntity
    {
        public Guid? ContactId { get; set; }

        [ForeignKey(nameof(ContactId))]
        public virtual Contact? Contact { get; set; }

        [Required]
        [Display(Name = "Type")]
        public ContactMethodType Type { get; set; }

        [Required]
        [MaxLength(256)]
        [Display(Name = "Value")]
        public string Value { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Label")]
        public string? Label { get; set; }
    }
}
