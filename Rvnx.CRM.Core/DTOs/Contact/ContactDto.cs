using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class ContactDto : BaseDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MaidenName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public bool IsHidden { get; set; }
    public Guid? ProfileImageId { get; set; }

    public string? Pronouns { get; set; }
    public string? Gender { get; set; }
    public string? Religion { get; set; }

    public DateTime? Birthday { get; set; }

    public bool IsPartial { get; set; }

    public IEnumerable<LabelDto> Labels { get; set; } = [];
}