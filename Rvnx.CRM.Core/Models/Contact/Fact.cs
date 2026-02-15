using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact
{
    [Table("Fact")]
    public class Fact : CRMGenericEntity
    {
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
