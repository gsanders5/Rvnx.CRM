using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class FactFormDto
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Display(Name = "Value")]
    public string Value { get; set; } = string.Empty;

    public Guid ContactId { get; set; }
}
