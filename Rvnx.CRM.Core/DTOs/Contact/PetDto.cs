using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public record class PetDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Species { get; set; }
    public string? Breed { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? Notes { get; set; }
    public Guid ContactId { get; set; }
    public List<Guid> ContactIds { get; set; } = [];

    /// <summary>
    /// Per-owner display data (id, name, deceased flag) used by views that list pet owners.
    /// Populated by services that fetch owner contacts; left empty when not needed.
    /// </summary>
    public List<PetOwnerDto> Owners { get; set; } = [];
}

public record class PetOwnerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsDeceased { get; set; }
}
