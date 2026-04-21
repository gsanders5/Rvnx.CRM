using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class FactDto : BaseDto
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public EntityType EntityType { get; set; } = EntityType.Person;
}
