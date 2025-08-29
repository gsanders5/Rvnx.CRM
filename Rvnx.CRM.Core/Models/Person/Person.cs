using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rvnx.CRM.Core.Models.Person;

[Table("People")]
[Index(nameof(FirstName), nameof(LastName), Name = "IX_People_FirstName_LastName")]
public class Person : CRMBaseEntity
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

    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}".Trim();

    public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; } = [];
}