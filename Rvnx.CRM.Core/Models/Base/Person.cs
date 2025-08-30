using Rvnx.CRM.Core.Models.Dates;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Base;

public abstract class Person : CRMBaseEntity
{
    [Required]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Birthday")]
    [DataType(DataType.Date)]
    public DateTime? Birthday { get; set; }

    [Display(Name = "Important Dates")]
    [InverseProperty(nameof(ImportantDate.Person))]
    public virtual ICollection<ImportantDate> ImportantDates { get; set; } = [];

    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}".Trim();
}