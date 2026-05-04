using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public record class FactDto : BaseDto
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Guid ContactId { get; set; }
}
