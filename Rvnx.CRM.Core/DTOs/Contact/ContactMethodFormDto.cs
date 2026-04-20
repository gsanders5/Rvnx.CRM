using Rvnx.CRM.Core.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class ContactMethodFormDto
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

    public Guid EntityId { get; set; }
}