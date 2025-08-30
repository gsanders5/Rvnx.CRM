using Rvnx.CRM.Core.Models.Dates;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models.Base;

public abstract class Company : CRMBaseEntity
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Name")]
    public string CompanyName { get; set; } = string.Empty;

    [Display(Name = "Website")]
    public string? Website { get; set; } = string.Empty;

    [Display(Name = "Contacts")]
    public virtual ICollection<Person> Contacts { get; set; } = [];

    [Display(Name = "Important Dates")]
    public virtual ICollection<ImportantDate> ImportantDates { get; set; } = [];
}