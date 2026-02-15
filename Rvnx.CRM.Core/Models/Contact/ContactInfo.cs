using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("ContactInfo")]
    public class ContactInfo : CRMGenericEntity
    {
        [Required]
        [Display(Name = "Type")]
        public ContactInfoType Type { get; set; }

        [Required]
        [MaxLength(256)]
        [Display(Name = "Value")]
        public string Value { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Label")]
        public string? Label { get; set; }
    }
}
