using Rvnx.CRM.Core.Models.Contact;
﻿using Rvnx.CRM.Core.Models.Dates;
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

    [MaxLength(256)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    [MaxLength(200)]
    [Display(Name = "Company")]
    public string? Company { get; set; }

    [Display(Name = "Birthday")]
    [DataType(DataType.Date)]
    public DateTime? Birthday { get; set; }

    [Display(Name = "Important Dates")]
    [InverseProperty(nameof(ImportantDate.Person))]
    public virtual ICollection<ImportantDate> ImportantDates { get; set; } = [];

    [Display(Name = "Relationships")]
    [InverseProperty(nameof(Relationship.Person))]
    public virtual ICollection<Relationship> Relationships { get; set; } = [];

    [Display(Name = "Related To")]
    [InverseProperty(nameof(Relationship.RelatedPerson))]
    public virtual ICollection<Relationship> RelatedTo { get; set; } = [];

    [Display(Name = "Reminders")]
    [InverseProperty(nameof(Reminder.Person))]
    public virtual ICollection<Reminder> Reminders { get; set; } = [];

    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}".Trim();
}