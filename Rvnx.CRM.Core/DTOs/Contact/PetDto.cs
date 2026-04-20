using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class PetDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Species { get; set; }
    public string? Breed { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Notes { get; set; }
    public Guid EntityId { get; set; }
    public EntityType EntityType { get; set; } = EntityType.Person;
    public List<Guid> ContactIds { get; set; } = [];
}