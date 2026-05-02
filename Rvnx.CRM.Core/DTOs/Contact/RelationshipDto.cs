using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class RelationshipDto : BaseDto
{
    public Guid EntityId { get; set; }
    public EntityType EntityType { get; set; } = EntityType.Person;
    public string EntityName { get; set; } = string.Empty;

    public Guid RelatedEntityId { get; set; }
    public string RelatedEntityName { get; set; } = string.Empty;

    public Guid RelationshipTypeId { get; set; }
    public string RelationshipTypeName { get; set; } = string.Empty;
    public string RelationshipTypeOppositeName { get; set; } = string.Empty;
    public string RelationshipTypeCategory { get; set; } = string.Empty;

    public bool IsEntityPartial { get; set; }
    public bool IsRelatedEntityPartial { get; set; }

    public bool IsEntityDeceased { get; set; }
    public bool IsRelatedEntityDeceased { get; set; }

    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
