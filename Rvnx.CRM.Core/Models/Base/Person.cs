using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Base;

public abstract class Person : BaseEntity
{
    [Required]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [MaxLength(100)]
    [Display(Name = "Maiden Name")]
    public string? MaidenName { get; set; }

    [MaxLength(100)]
    [Display(Name = "Nickname")]
    public string? Nickname { get; set; }

    [MaxLength(100)]
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    [MaxLength(200)]
    [Display(Name = "Company")]
    public string? Company { get; set; }

    [MaxLength(1000)]
    [Display(Name = "How We Met")]
    public string? HowWeMet { get; set; }

    [Display(Name = "First Met On")]
    public DateOnly? FirstMetOn { get; set; }

    [Display(Name = "Introduced By")]
    public Guid? IntroducedByContactId { get; set; }

    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [Display(Name = "Is Hidden")]
    public bool IsHidden { get; set; } = false;

    [Display(Name = "Deceased")]
    public bool IsDeceased { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of Death")]
    public DateOnly? DateOfDeath { get; set; }

    [NotMapped]
    public Guid? ProfileImageId { get; set; }

    // NotMapped Properties for View Compatibility
    public virtual ICollection<Note> Notes { get; set; } = [];

    public virtual ICollection<SignificantDate> SignificantDates { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of relationships WHERE THIS PERSON IS THE SOURCE.
    /// This property is [NotMapped] and MUST be populated manually by a service (e.g., ContactReadService).
    /// </summary>
    [NotMapped]
    public ICollection<Relationship> Relationships { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of relationships WHERE THIS PERSON IS THE TARGET.
    /// This property is [NotMapped] and MUST be populated manually by a service (e.g., ContactReadService).
    /// </summary>
    [NotMapped]
    public ICollection<Relationship> RelatedTo { get; set; } = [];

    public virtual ICollection<ContactMethod> ContactMethods { get; set; } = [];

    public virtual ICollection<Fact> Facts { get; set; } = [];

    public virtual ICollection<Address> Addresses { get; set; } = [];

    public virtual ICollection<Attachment> Attachments { get; set; } = [];

    public virtual ICollection<ContactTask> ContactTasks { get; set; } = [];
}
