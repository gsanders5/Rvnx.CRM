using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class ContactMethodFormDto : IValidatableObject
{
    public Guid? Id { get; set; }

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

    public Guid ContactId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == ContactMethodType.Phone
            && !PhoneNumberNormalizer.TryNormalize(Value, out _, out string? error))
        {
            yield return new ValidationResult(error, [nameof(Value)]);
        }
    }
}
