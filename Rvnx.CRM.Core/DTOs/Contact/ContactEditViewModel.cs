using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class ContactEditViewModel : ContactFormDto
{
    public IEnumerable<string> PronounOptions { get; set; } = [];
    public IEnumerable<string> GenderOptions { get; set; } = [];
    public bool HasRelationships { get; set; }
    public bool ImmichEnabled { get; set; }
    public IReadOnlyList<ImmichOptionDto> AllImmichPeople { get; set; } = [];
    public IReadOnlyList<ImmichOptionDto> AllImmichTags { get; set; } = [];

    /// <summary>
    /// True when the contact being edited is the current user's self-contact.
    /// Used to suppress destructive actions (such as marking the user deceased)
    /// that would silently disable their own reminders / dashboard / calendar.
    /// </summary>
    public bool IsSelf { get; set; }
}
