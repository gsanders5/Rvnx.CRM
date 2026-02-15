using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Rvnx.CRM.Core.Enumerations.CoreEnumerations;

namespace Rvnx.CRM.Core.Models.Contact;

[Table(nameof(PhoneNumber))]
public class PhoneNumber : PolymorphicEntity
{
    [MaxLength(20)]
    [Display(Name = "Phone Number Type")]
    public PhoneNumberType Type { get; set; } = PhoneNumberType.Unknown;

    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string? Value { get; set; } = string.Empty;
}
