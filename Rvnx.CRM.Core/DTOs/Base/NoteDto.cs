using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Base;

public class NoteDto : BaseDto
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public EntityType EntityType { get; set; } = EntityType.Person;
}
