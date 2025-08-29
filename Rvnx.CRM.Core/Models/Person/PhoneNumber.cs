using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Person;

[Table(nameof(PhoneNumber))]
public class PhoneNumber : CRMBaseEntity
{
    [MaxLength(20)]
    [Display(Name = "Phone Number Type")]
    public string? Type { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string? Number { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Phone Holder")]
    public virtual Person Person { get; set; } = null!;
}