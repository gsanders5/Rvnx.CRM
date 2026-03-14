using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models.Base;

public abstract class Company : BaseEntity
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Name")]
    public string CompanyName { get; set; } = string.Empty;

    [Display(Name = "Website")]
    public string? Website { get; set; } = string.Empty;
}