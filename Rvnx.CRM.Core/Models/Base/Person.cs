using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Base;

public abstract class Person : CRMBaseEntity
{
    [Required]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [MaxLength(100)]
    [Display(Name = "Nickname")]
    public string? Nickname { get; set; }

    [MaxLength(100)]
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    [MaxLength(200)]
    [Display(Name = "Company")]
    public string? Company { get; set; }

    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}".Trim();

    // NotMapped Properties for View Compatibility
    [NotMapped]
    public ICollection<Note> Notes { get; set; } = [];

    [NotMapped]
    public ICollection<Reminder> Reminders { get; set; } = [];

    [NotMapped]
    public ICollection<ImportantDate> ImportantDates { get; set; } = [];

    [NotMapped]
    public ICollection<Relationship> Relationships { get; set; } = [];

    [NotMapped]
    public ICollection<Relationship> RelatedTo { get; set; } = [];

    [NotMapped]
    public ICollection<ContactInfo> ContactInfos { get; set; } = [];

    [NotMapped]
    public ICollection<Fact> Facts { get; set; } = [];
}
