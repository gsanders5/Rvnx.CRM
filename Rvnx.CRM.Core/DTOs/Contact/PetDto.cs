using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class PetDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Species { get; set; }
    public string? Breed { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Notes { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public List<Guid> ContactIds { get; set; } = [];
}