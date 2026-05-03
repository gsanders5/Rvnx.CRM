using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class PetDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Species { get; set; }
    public string? Breed { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? Notes { get; set; }
    public Guid EntityId { get; set; }
    public EntityType EntityType { get; set; } = EntityType.Person;
    public List<Guid> ContactIds { get; set; } = [];

    /// <summary>
    /// Per-owner display data (id, name, deceased flag) used by views that list pet owners.
    /// Populated by services that fetch owner contacts; left empty when not needed.
    /// </summary>
    public List<PetOwnerDto> Owners { get; set; } = [];
}

public class PetOwnerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsDeceased { get; set; }
}
