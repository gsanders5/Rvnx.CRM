using Rvnx.CRM.Core.Validation;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class ContactFormDto
{
    public Guid? Id { get; set; }

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
    public string? Nickname { get; set; }

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(50)]
    [PhoneNumber]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? JobTitle { get; set; }

    [MaxLength(200)]
    public string? Company { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Birthday { get; set; }

    [Display(Name = "Remind on Birthday")]
    public bool RemindOnBirthday { get; set; } = true;

    public bool IsHidden { get; set; }

    [MaxLength(100)]
    public string? Pronouns { get; set; }

    [MaxLength(100)]
    public string? Gender { get; set; }

    [MaxLength(100)]
    public string? Religion { get; set; }

    public Guid? ProfileImageId { get; set; }

    public Guid? ImmichPersonId { get; set; }

    [MaxLength(256)]
    public string? ImmichPersonName { get; set; }

    public Guid? ImmichTagId { get; set; }

    [MaxLength(256)]
    public string? ImmichTagValue { get; set; }

    [MaxLength(1000)]
    [Display(Name = "How We Met")]
    [DataType(DataType.MultilineText)]
    public string? HowWeMet { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "First Met On")]
    public DateOnly? FirstMetOn { get; set; }

    [Display(Name = "Introduced By")]
    public Guid? IntroducedByContactId { get; set; }

    public List<LabelDto> AllLabels { get; set; } = [];
    public List<Guid> AssignedLabelIds { get; set; } = [];

    /// <summary>
    /// Candidates available to be selected as the "introduced by" contact.
    /// Excludes the current contact to prevent self-reference.
    /// </summary>
    public List<ContactSelectItemDto> IntroducerCandidates { get; set; } = [];
}
