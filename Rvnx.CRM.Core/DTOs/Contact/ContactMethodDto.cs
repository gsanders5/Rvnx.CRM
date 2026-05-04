using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public record class ContactMethodDto : BaseDto
{
    public ContactMethodType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Label { get; set; }
    public Guid ContactId { get; set; }
}
